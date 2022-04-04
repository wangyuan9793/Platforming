using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.OrTools.LinearSolver;
using System.IO;



namespace TOL
{
    class SolverMgr
    {
        Solver solver;
        internal Objective objective;

        ModelMgr modelMgr;

        int consCnt = 0;
        int varCnt = 0;

        static int M = 9999;

        Dictionary<Decision, Variable> X = new Dictionary<Decision, Variable>();

        

        internal SolverMgr(ModelMgr modelMgr)
        {

            this.modelMgr = modelMgr;
            CreateSolver();
            PrepareVariables();
            SetObjective();
            OneDecisionConstrain();
            ConflictFreeConstraint();
            SetConflictCost();
            AddThroatLeisureConstraints();

            if(modelMgr.solverConfig.isResourceUseViolationAllowed)
            {
                ResourcePenaltyUse();
            }
            else
            {
                ResourceOneUse();
            }

            
            LockedResourcesIsNotAllowedToBeSeize();

            if (modelMgr.solverConfig.isAddCut)
            {
                AddCut();
            }

            if(modelMgr.solverConfig.isWarmStart)
            {
                SetWarmStart();
            }

            //solver.Solve();

        }

        Variable z;

        List<LinearExpr> obj_le;

        internal void CreateSolver()
        {

            var test = Solver.SupportsProblemType(modelMgr.solverConfig.solverType);

            try
            {
                solver = new Solver("solver", modelMgr.solverConfig.solverType);
            }
            catch
            {
                ;
            }
            finally
            {
                ;
            }
                

            //new solver

            
            Console.WriteLine("create " + modelMgr.solverConfig.solverType + " solver at " + DateTime.Now.ToString("yyyy-MM-dd hh:mm"));
            //solver.EnableOutput();
            if(modelMgr.solverConfig.isPrint)
            {
                solver.EnableOutput();
            }

            //get obj
            objective = solver.Objective();
            objective.SetMinimization();

            //solver.Add(objective >= 10);
            //var status = solver.SetNumThreads(8);
            obj_le = new List<LinearExpr>();
        }
        
        internal void PrepareVariables()
        {
            foreach (WPC wPC in modelMgr.wPCs)
            {
                foreach (Decision decision in wPC.decisions)
                {
                    Variable x = solver.MakeBoolVar("");
                    X.Add(decision, x);
                    //X0.Add(decision, 0);
                }
            }

            //Console.WriteLine(solver.NumVariables()-this.VarNum + " variables added according to decisions" + solver.NumVariables() + "in total");
            //this.VarNum = solver.NumVariables();
            modelMgr.ShowConsAndVarsNum("initializing decisions", ref this.varCnt, ref this.consCnt,0, this.solver);
        }

        internal void SetObjective()
        {
            foreach (Decision decision in X.Keys)
            {
                objective.SetCoefficient(X[decision], decision.panelty);
                obj_le.Add(X[decision] * decision.panelty);
            }

        }

        internal void OneDecisionConstrain()
        {
            foreach (WPC wpc in modelMgr.wPCs)
            {
                //LinearExpr linearExpr = new LinearExpr();
                List<LinearExpr> les = new List<LinearExpr>();

                foreach (Decision decision in wpc.decisions)
                {
                    //linearExpr = linearExpr + X[decision];
                    les.Add(X[decision]);
                }
                RangeConstraint rangeConstraint = (LinearExprArrayHelper.Sum(les.ToArray()) == 1);
                solver.Add(rangeConstraint);

                //TPTGroupLEXP.Add(tptg, linearExpr);
            }

            modelMgr.ShowConsAndVarsNum("ond decision constraint", ref this.varCnt, ref this.consCnt, 1, this.solver);
        }

        internal void Solve()
        {
            //string mps = solver.ExportModelAsMpsFormat(true, true);

            //File.WriteAllText("test93.mps", mps);


            //modelMgr.solverConfig.MPSolverParameters.SetDoubleParam(MPSolverParameters.DoubleParam.RELATIVE_MIP_GAP)

            //solver.SetSolverSpecificParametersAsString("ImproveStartTime  200");
            //solver.SetTimeLimit(10000);

            solver.Add(LinearExprArrayHelper.Sum(obj_le.ToArray()) >= modelMgr.solverConfig.LB);
            if (modelMgr.solverConfig.timeLimitSecond >0)
            solver.SetTimeLimit(modelMgr.solverConfig.timeLimitSecond * 1000);



            //modelMgr.solverConfig.MPSolverParameters.SetDoubleParam(MPSolverParameters.DoubleParam.RELATIVE_MIP_GAP, 0.0005);

            solver.Solve(modelMgr.solverConfig.MPSolverParameters);

            //solver.Solve();
            
            foreach (Decision decision in X.Keys)
            {
                if(X[decision].SolutionValue()==1.0)
                {
                    decision.wpc.selectedDecision = decision;
                }

            }
        }

        internal void ConflictFreeConstraint()
        {
            foreach (Decision decision in X.Keys)
            {
                foreach (Decision decision_conflict in decision.conflictDecisions)
                {

                    if(X.Keys.Contains(decision_conflict))
                    {
                        solver.Add(X[decision] + X[decision_conflict] <= 1);
                    }
                    
                }

            }
            //Console.WriteLine(solver.NumConstraints() + " constraints in total at conflict free");
            modelMgr.ShowConsAndVarsNum("conflict free", ref this.varCnt, ref this.consCnt, 1, this.solver);
        }

        internal void SetConflictCost()
        {
            foreach (Decision decision in X.Keys)
            {
                foreach (Decision decision_conflict in decision.conflictDegree.Keys)
                {
                    Variable p = solver.MakeBoolVar("");
                    //Variable p = solver.MakeVar(1.0, 2.0, false, "");
                    solver.Add(X[decision] + X[decision_conflict] -1<= p);
                    objective.SetCoefficient(p, decision.conflictDegree[decision_conflict]);
                    obj_le.Add(decision.conflictDegree[decision_conflict] * p);
                    
                }

            }
            //Console.WriteLine(solver.NumVariables() + " variables in total at conflict cost ");

            modelMgr.ShowConsAndVarsNum("conflict cost", ref this.varCnt, ref this.consCnt, 0, this.solver);
            //Console.WriteLine(solver.NumConstraints() + " constraints in total at conflict cost");
            modelMgr.ShowConsAndVarsNum("conflict cost", ref this.varCnt, ref this.consCnt, 1, this.solver);

        }

        internal void ResourceOneUse()
        {
            Dictionary<object, Dictionary<int, List<LinearExpr>>> LE = new Dictionary<object, Dictionary<int, List<LinearExpr>>>();

            foreach (Decision decision in X.Keys)
            {
                foreach (ResourceUse rsUse in decision.resourceUses)
                {
                    if (LE.Keys.Contains(rsUse.resource) == false)
                        LE[rsUse.resource] = new Dictionary<int, List<LinearExpr>>();
                    for (int i = rsUse.StartTime; i < rsUse.EndTime; i++)
                    {
                        if (LE[rsUse.resource].Keys.Contains(i) == false)
                        {
                            LE[rsUse.resource][i] = new List<LinearExpr>();

                        }
                        LE[rsUse.resource][i].Add(X[decision]);
                    } 
                }
            }

            foreach (object resource in LE.Keys)
            {
                foreach (var t in LE[resource].Keys)
                {
                    solver.Add(LinearExprArrayHelper.Sum(LE[resource][t].ToArray()) <= 1);
                }
            }

            //Console.WriteLine(solver.NumConstraints() + " constraints in total at resource-one-use");
            modelMgr.ShowConsAndVarsNum("resource one use", ref this.varCnt, ref this.consCnt, 1, this.solver);
        }


        internal void ResourcePenaltyUse()
        {
            Dictionary<object, Dictionary<int, List<LinearExpr>>> LE = new Dictionary<object, Dictionary<int, List<LinearExpr>>>();

            foreach (Decision decision in X.Keys)
            {
                foreach (ResourceUse rsUse in decision.resourceUses)
                {
                    if (LE.Keys.Contains(rsUse.resource) == false)
                        LE[rsUse.resource] = new Dictionary<int, List<LinearExpr>>();
                    for (int i = rsUse.StartTime; i < rsUse.EndTime; i++)
                    {
                        if (LE[rsUse.resource].Keys.Contains(i) == false)
                        {
                            LE[rsUse.resource][i] = new List<LinearExpr>();

                        }
                        if (!LE[rsUse.resource][i].Contains(X[decision]))
                        {
                            LE[rsUse.resource][i].Add(X[decision]);
                        }
                    }
                }
            }

            foreach (object resource in LE.Keys)
            {
                foreach (var t in LE[resource].Keys)
                {
                    Variable r = solver.MakeVar(0.0, 3, false, "");
                    objective.SetCoefficient(r, modelMgr.solverConfig.resourcesPenaly[resource]);
                    solver.Add(LinearExprArrayHelper.Sum(LE[resource][t].ToArray()) <= 1 + r);
                }
            }

            //Console.WriteLine(solver.NumConstraints() + " constraints in total at resource-one-use");
            modelMgr.ShowConsAndVarsNum("resource-violation-with-panelty", ref this.varCnt, ref this.consCnt, 1, this.solver);

        }

        internal void LockedResourcesIsNotAllowedToBeSeize()
        {

            Dictionary<object, List<LinearExpr>> LE = new Dictionary<object, List<LinearExpr>>();

            foreach (Decision decision in X.Keys)
            {
                foreach (var rsLock in decision.resourceLocks)
                {

                    if (LE.Keys.Contains(rsLock) == false)
                    {
                        LE[rsLock] = new List<LinearExpr>();
                    }
                    LE[rsLock].Add(X[decision]);
                }

                foreach (var rsSeize in decision.resourceSeizes)
                {
                    if (LE.Keys.Contains(rsSeize) == false)
                    {
                        LE[rsSeize] = new List<LinearExpr>();
                    }
                    LE[rsSeize].Add(X[decision] * M);
                }
            }




            foreach (var resource in LE.Keys)
            {
                solver.Add(LinearExprArrayHelper.Sum(LE[resource].ToArray()) <= M+1);
            }


            //Console.WriteLine(solver.NumConstraints() + " constraints in total at resource-lock-seize-constrain");
            modelMgr.ShowConsAndVarsNum("resource-lock-seize-constraint", ref this.varCnt, ref this.consCnt, 1, this.solver);

        }

        private void AddCut()
        {
            z = solver.MakeVar(double.NegativeInfinity, double.PositiveInfinity, false, "");

            objective.SetCoefficient(z, 1.0);
            obj_le.Add(1.0 * z);

            Dictionary<int, List<LinearExpr>> cuts = new Dictionary<int, List<LinearExpr>>(); 


            //List<LinearExpr> le = new List<LinearExpr>();
            foreach (Decision decisioni in X.Keys)
            {
                foreach (int cutID in decisioni.conflictCut.Keys)
                {

                    if(cuts.Keys.Contains(cutID) == false)
                    {
                        cuts[cutID] = new List<LinearExpr>();
                    }

                    foreach (Decision decisionj in decisioni.conflictCut[cutID].Keys)
                    {
                        cuts[cutID].Add(decisioni.conflictCut[cutID][decisionj] * (X[decisioni] + X[decisionj] - 1));
                    }

                    //le.Add(decisioni.conflictCut[decisionj] * (X[decisioni] + X[decisionj] - 1));
                }
            }

            foreach (var le in cuts.Values)
            {
                solver.Add(LinearExprArrayHelper.Sum(le.ToArray()) <= z);
            }
            
            modelMgr.ShowConsAndVarsNum("adding cuts", ref this.varCnt, ref this.consCnt, 1, this.solver);
        }

        private void AddThroatLeisureConstraints()
        {
            if (modelMgr.solverConfig.throatLeisureDurations.Count == 0)
            {
                Console.WriteLine("no throat leisure constraint added.");
                return;
            }

            var throatLeisureMinutes = new HashSet<int>(
                    modelMgr.solverConfig.throatLeisureDurations
                    .SelectMany(d => Enumerable.Range(d.Item1, d.Item2 - d.Item1))
                );  // [a, b)

            IEnumerable<object> RGetThroatLeaves(object node)
            {
                if (modelMgr.solverConfig.throatTree.ContainsKey(node))
                {
                    var children = modelMgr.solverConfig.throatTree[node];

                    if (children.Count > 0)
                    {
                        foreach (var childNode in children)
                        {
                            foreach (var childLeaf in RGetThroatLeaves(childNode))
                            {
                                yield return childLeaf;
                            }
                        }
                    }
                    else
                    {
                        yield return node;  // leaf
                    }
                }
            }


            var tableMinuteAndThroatLeafResourceWithLinearExpr = new Dictionary<int, Dictionary<object, List<LinearExpr>>>();
            foreach (Decision decision in X.Keys)
            {
                foreach (ResourceUse rsUse in decision.throatUses)
                {
                    foreach(var leaf in RGetThroatLeaves(rsUse.resource))  // leaves only
                    {
                        for (int t = rsUse.StartTime; t < rsUse.EndTime; t++)
                        {
                            if (throatLeisureMinutes.Contains(t))  // leisure time
                            {
                                // -----
                                if(!tableMinuteAndThroatLeafResourceWithLinearExpr.ContainsKey(t))
                                {
                                    tableMinuteAndThroatLeafResourceWithLinearExpr[t] = new Dictionary<object, List<LinearExpr>>();
                                }
                                if(!tableMinuteAndThroatLeafResourceWithLinearExpr[t].ContainsKey(leaf))
                                {
                                    tableMinuteAndThroatLeafResourceWithLinearExpr[t][leaf] = new List<LinearExpr>();
                                }
                                tableMinuteAndThroatLeafResourceWithLinearExpr[t][leaf].Add(X[decision]);
                            }
                        }
                    }
                }
            }

            var leaves = modelMgr.solverConfig.throatTree.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();
            foreach (var kv1 in tableMinuteAndThroatLeafResourceWithLinearExpr)
            {
                var t = kv1.Key;
                var dictLeafAndAssistVar = new Dictionary<object, Variable>();  // creat assistant variables
                foreach (var leaf in leaves)
                {
                    dictLeafAndAssistVar[leaf] = solver.MakeBoolVar($"throat state at time {t}");
                }

                foreach (var kv2 in kv1.Value)
                {
                    var resource = kv2.Key;
                    var linearExprs = kv2.Value;
                    // leaves leisure constraints
                    foreach (var linerExpr in linearExprs)
                    {
                        solver.Add(linerExpr <= dictLeafAndAssistVar[resource]);
                    }
                }
                // at least one leaf available
                var p = solver.MakeBoolVar("throat leisure violation compensation");
                solver.Add(LinearExprArrayHelper.Sum(dictLeafAndAssistVar.Values.ToArray()) <= dictLeafAndAssistVar.Count - 1 + p);
                objective.SetCoefficient(p, modelMgr.solverConfig.throatLeisureViolationPenalty);
            }

            //Console.WriteLine(solver.NumConstraints() + " constraints in total at resource-one-use");
            modelMgr.ShowConsAndVarsNum("throat-leisure-violation-cost", ref this.varCnt, ref this.consCnt, 1, this.solver);
        }

        private Dictionary<Variable, double> X0 = new Dictionary<Variable, double>();
        internal void SetWarmStart()
        {
            X0 = X.Select(a => a.Value).ToDictionary(v => v, a => 0.0);

            foreach (WPC wPC in modelMgr.wPCs)
            {
                if (wPC.warmStartDecision != null && X.Keys.Contains(wPC.warmStartDecision))
                {
                    X0[X[wPC.warmStartDecision]]=1.0;
                }

            }



            MPVariableVector variables = new MPVariableVector(X0.Keys);

            solver.SetHint(variables, X0.Values.ToArray());

        }


    }
}
