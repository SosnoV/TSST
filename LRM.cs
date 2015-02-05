using LabelSwitchingRouter;
using System;
using System.Collections.Generic;

namespace CsharpMPLS
{
    class LRM
    {
        private Dictionary<int, double> neighboursAvB = null; //port, MaximumBandwith
        private Dictionary<int, double> currentAvB = null; ////port, AvailableBandwith
        private Dictionary<string, LC> takenSNPs = null;
        private List<int> labels = null;
        public Dictionary<string, int> nodesToPorts = null;
        
        public struct LC 
        {
            public string neighbour;
            public int port;
            public string label;
            public double takenAvB;
        }

        public LRM(int nodeId) 
        {

            neighboursAvB = Parser.portsAvB(nodeId);
            currentAvB = neighboursAvB;
            Console.WriteLine("LRM created");
            foreach (var i in currentAvB)
            {
                Console.WriteLine(" Key {0} Value {1}", i.Key, i.Value);
            }
            takenSNPs = new Dictionary<string, LC>();
            labels = new List<int>();
            nodesToPorts = new Dictionary<string, int>();
        }

        private string generateLabel()
        {
            Random rand = new Random();
            int label;
            bool exists;
            while (true) 
            {
                exists = false;
                label = rand.Next(100) * 100;
                foreach (var l in labels)
                {
                    if (l == label)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    break;
            }
            labels.Add(label);
            return label.ToString();
        }

        internal string Reserve(string neighbour, int port, double bandwith, string pathID) 
        {
            if (ReserveAvB(port, bandwith))
            {
                LC lc;
                lc.neighbour = neighbour;
                lc.port = port;
                lc.takenAvB = bandwith;
                lc.label = generateLabel();
                takenSNPs.Add(pathID, lc);
                return lc.label;            
            }
            else
                return "0";
        
        }

        internal void Disreserve(string pathID)
        {
            
            double avb;
            double AvB; //maksymalne AvB
            LC lc;
            takenSNPs.TryGetValue(pathID, out lc);
            currentAvB.TryGetValue(lc.port, out avb);
            neighboursAvB.TryGetValue(lc.port, out AvB);
            avb += lc.takenAvB;
            if (avb > AvB)
                avb = AvB;
            currentAvB.Remove(lc.port);
            currentAvB.Add(lc.port, avb);
            labels.Remove(int.Parse(lc.label));
            takenSNPs.Remove(pathID);
            return;
        }

        private bool ReserveAvB(int port, double bandwith)
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

        //internal bool Reserve(bool reserve, int port, double bandwith)
        //{
        //    //int port = lsr.Translate(nodeId);
        //    if (reserve)
        //    {
        //        double avb;
        //        currentAvB.TryGetValue(port, out avb);
        //        if (avb < bandwith)
        //            return false;
        //        avb -= bandwith;
        //        currentAvB.Remove(port);
        //        currentAvB.Add(port, avb);
        //        return true;
        //    }
        //    else
        //    {
        //        double avb;
        //        double AvB; //maksymalne AvB
        //        currentAvB.TryGetValue(port, out avb);
        //        neighboursAvB.TryGetValue(port, out AvB);
        //        avb += bandwith;
        //        if (avb > AvB)
        //            avb = AvB;
        //        currentAvB.Remove(port);
        //        currentAvB.Add(port, avb);
        //        return true;
        //    }
        //}
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
