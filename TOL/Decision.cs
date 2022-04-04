using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace TOL
{
    public class Decision
    {
        public WPC wpc;
        public List<Decision> conflictDecisions = new List<Decision>();
        public int panelty;
        public Dictionary<Decision, int> conflictDegree = new Dictionary<Decision, int>();

        public List<ResourceUse> resourceUses = new List<ResourceUse>();
        public List<object> resourceLocks = new List<object>();
        public List<object> resourceSeizes = new List<object>();
        public List<ResourceUse> throatUses = new List<ResourceUse>();  // for throat leisure constrains


        public Dictionary<int,Dictionary<Decision, int>> conflictCut = new Dictionary<int, Dictionary<Decision, int>> ();

        public object actualStartTime;
        public object actualEndTime;

        public object inTrack;
        public object outTrack;
        public object Platform;

        public string decisionInfo;


        private void RemoveDuplicatedResourceUse()
        {



        }

    }
    
    public class ResourceUse
    {
        public object resource;
        public int StartTime;
        public int EndTime;
    }





}
