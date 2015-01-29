using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CsharpMPLS
{
    class Parser
    {
        //private static string configurationLSRFile = "LSRconfiguration.txt";
        private static string configTxt = "config.txt";
        private static char[] delimiter = {'=', ' '};

        internal static Dictionary<int, double> portsAvB(int nodeId) 
        {
            string line;
            string[] data = null;
            int numberOfWires = 0;
            Dictionary<int, double> dic = new Dictionary<int, double>();
            StreamReader file = new System.IO.StreamReader(configTxt);
            while ((line = file.ReadLine()) != null)
            {
                if (String.Compare("number_of_wires", 0, line, 0, 15, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    data = line.Split('=');
                    numberOfWires = Int32.Parse(data[1]);
                    data = null;
                }
                else if (numberOfWires > 0)
                {
                    if (line.Contains(',') && !line.Contains('#'))
                    {
                        data = line.Split(',');
                        if (int.Parse(data[0]) == nodeId)
                        {
                            dic.Add(int.Parse(data[1]), double.Parse(data[4]));
                        }
                        else if (int.Parse(data[2]) == nodeId)
                        {
                            dic.Add(int.Parse(data[3]), double.Parse(data[4]));
                        }
                        numberOfWires--;
                        data = null;
                    }
                }

            }
            file.Close();
            return dic;
        }

        internal static int ParseConfiguration(string nID, out int managerPort, out int wiresPort)
        {
            string id = "Node" + nID;
            string line = null;
            int portNumber = 0;
            string[] array = null;
            managerPort = 0;
            wiresPort = 0;
            bool gotManager = false;
            bool gotWires = false;
            bool gotLocalPort = false;
            using (StreamReader sr = new StreamReader(configTxt))
            {
                while (!sr.EndOfStream)
                {
                    if (gotWires && gotManager && gotLocalPort)
                        return portNumber;
                    line = sr.ReadLine();
                    if (line.Contains(id))
                    {
                        array = line.Split(delimiter[0]);
                        if (int.TryParse(array[1], out portNumber))
                            gotLocalPort = true;
                    }//end of if
                    if (line.Contains("#manager"))
                    {
                        line = sr.ReadLine();
                        array = line.Split(delimiter[0]);
                        if (int.TryParse(array[1], out managerPort))
                            gotManager = true;
                    }
                    else if (line.Contains("#wires"))
                    {
                        line = sr.ReadLine();
                        array = line.Split(delimiter[0]);
                        if (int.TryParse(array[1], out wiresPort))
                            gotWires = true;
                    }
                    //Console.WriteLine("Manager: {0}, Wires: {1}, LocalPort: {2}", managerPort, wiresPort, portNumber);
                }
            }//end of using
            try
            {
                if (!(gotWires && gotManager && gotLocalPort))
                    throw new Exception();
            }
            catch (Exception)
            {
                Console.WriteLine("Manager: {0}, Wires: {1}, LocalPort: {2}", managerPort, wiresPort, portNumber);
                Console.WriteLine("Error getting manager,wires,local ports");
            }

            return portNumber;
        }
    }
}
