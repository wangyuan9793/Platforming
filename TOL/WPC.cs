using System;
using System.Collections.Generic;
using System.Text;

namespace TOL
{
    public class WPC
    {
        public string ID;
               
        public List<Decision> decisions = new List<Decision>();
        
        public Decision selectedDecision = null;

        public Decision warmStartDecision = null;


        public object originalTask;

        public string preStation;

        public string postStation;

        public int arrDelta = 0;

        public int depDelta = 0;


        public int minialDwell;

    }
}
