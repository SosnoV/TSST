using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpMPLS
{
    class LRM
    {
        private int AvB;
        private Dictionary<string, int> neighboursAvB;

        public LRM(int avb) 
        {
            AvB = avb;
            neighboursAvB = new Dictionary<string, int>();
        }

        internal bool Reserve(bool reserve, string nodeId, int bandwith) 
        {
            if (reserve)
            {
                int avb;
                neighboursAvB.TryGetValue(nodeId, out avb);
                if (avb < bandwith)
                    return false;
                avb -= bandwith;
                neighboursAvB.Remove(nodeId);
                neighboursAvB.Add(nodeId, avb);
                return true;
            }
            else 
            {
                int avb;
                neighboursAvB.TryGetValue(nodeId, out avb);
                avb += bandwith;
                if (avb > AvB)
                    avb = AvB;
                neighboursAvB.Remove(nodeId);
                neighboursAvB.Add(nodeId, avb);
                return true; 
            }
        }
        
        internal void AddNeighbourAVB(string nodeId) 
        {
            neighboursAvB.Add(nodeId, AvB);
            return;
        }

        internal void RemoveNeighbourAVB(string nodeId)
        {
            neighboursAvB.Remove(nodeId);
            return;
        }
    }
}
