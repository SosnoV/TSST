using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpMPLS
{
    class Parser
    {
        //private static string configurationLSRFile = "LSRconfiguration.txt";
        private static string configTxt = "config.txt";
        private static char[] delimiter = {'=', ' '};

        internal static int ParseConfiguration(string nID, out int managerPort, out int wiresPort, out int AvB)
        {
            string id = "Node" + nID;
            string line = null;
            int portNumber = 0;
            AvB = 0;
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
                        string[] lineSplit = line.Split(delimiter[1]); //split po spacji
                        array = lineSplit[1].Split(delimiter[0]); //split by dostac port number
                        lineSplit = lineSplit[2].Split(delimiter[0]); // spkit by dostac AvB
                        if (int.TryParse(array[1], out portNumber) && int.TryParse(lineSplit[1], out AvB))
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
