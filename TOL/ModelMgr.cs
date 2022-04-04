using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.LinearSolver;


namespace TOL
{
    public class ModelMgr
    {
        internal SolverConfig solverConfig;
        SolverMgr solverMgr;
        public IEnumerable<WPC> wPCs;
        
        public double objective;

        //public List<List<Tuple<Decision,Decision,>>> cuts;


        public ModelMgr(SolverConfig solverConfig, IEnumerable<WPC> wPCs)
        {
            //Solver solver = new Solver("", Solver.OptimizationProblemType.GUROBI_MIXED_INTEGER_PROGRAMMING);
            this.solverConfig = solverConfig;
            this.wPCs = wPCs;
            solverMgr = new SolverMgr(this);
            //MinConflictSolverMgr minConflictSolverMgr = new MinConflictSolverMgr(this);            
        }


        public void Solve()
        {
            solverMgr.Solve();
            this.objective = solverMgr.objective.Value();
        }

        internal void ShowConsAndVarsNum(string phase, ref int numVar, ref int numCon, int type, Solver solver)
        {
            if(solverConfig.isPrint)
            {
                if (type == 0)
                {

                    Console.WriteLine("{0} variables added at {1} ({2} variables in total)", solver.NumVariables() - numVar, phase, solver.NumVariables());
                    numVar = solver.NumVariables();
                }
                else if (type == 1)
                {
                    Console.WriteLine("{0} constraints added at {1} ({2} constraints in total)", solver.NumConstraints() - numCon, phase, solver.NumConstraints());
                    numCon = solver.NumConstraints();
                }

            }

        }

        
    }
}
