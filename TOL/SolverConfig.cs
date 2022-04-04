using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.LinearSolver;

namespace TOL
{
    public class SolverConfig
    {
        public Solver.OptimizationProblemType solverType = Solver.OptimizationProblemType.GUROBI_MIXED_INTEGER_PROGRAMMING;
        public MPSolverParameters MPSolverParameters = new MPSolverParameters();

        public int timeLimitSecond = 0;

        public bool isWarmStart = false;

        public double LB = 0;

        public bool isAddCut = false;

        public bool isResourceUseViolationAllowed = false;

        public bool isPrint = false;

        public Dictionary<object, double> resourcesPenaly = new Dictionary<object, double>();

        /// <summary>
        /// k: parent, v: children
        /// <para>throatTree[*leaf*].Count = 0</para>
        /// </summary>
        public Dictionary<object, List<object>> throatTree = new Dictionary<object, List<object>>();

        /// <summary>
        /// throat resource in durations will not be occupancied to the best of solver's abilities, [Item1, Item2)
        /// </summary>
        public List<Tuple<int, int>> throatLeisureDurations = new List<Tuple<int, int>>();

        public double throatLeisureViolationPenalty;

    }

    
}
