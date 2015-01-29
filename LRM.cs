using LabelSwitchingRouter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpMPLS
{
    class LRM
    {
        private Dictionary<int, double> neighboursAvB = null;
        private Dictionary<int, double> currentAvB = null;
        private LSR lsr;

        public LRM(int nodeId, LSR lsr) 
        {
            this.lsr = lsr;
            neighboursAvB = Parser.portsAvB(nodeId);
            currentAvB = neighboursAvB;
            Console.WriteLine("LRM created");
        }

        internal bool Reserve(bool reserve, string nodeId, double bandwith) 
        {
            int port = lsr.Translate(nodeId);
            if (reserve)
            {
                double avb;
                currentAvB.TryGetValue(port, out avb);
                if (avb < bandwith)
                    return false;
                avb -= bandwith;
                currentAvB.Remove(port);
                currentAvB.Add(port, avb);
                return true;
            }
            else 
            {
                double avb;
                double AvB; //maksymalne AvB
                currentAvB.TryGetValue(port, out avb);
                neighboursAvB.TryGetValue(port, out AvB);
                avb += bandwith;
                if (avb > AvB)
                    avb = AvB;
                currentAvB.Remove(port);
                currentAvB.Add(port, avb);
                return true; 
            }
        }
        
        //internal void AddNeighbourAVB(string nodeId) 
        //{
        //    neighboursAvB.Add(nodeId, AvB);
        //    return;
        //}

        //internal void RemoveNeighbourAVB(string nodeId)
        //{
        //    neighboursAvB.Remove(nodeId);
        //    return;
        //}
    }
}
