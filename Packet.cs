using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpMPLS
{
    //Version 1 NOT TESTED
    //Version 1.1 NOT TESTED:
    //Added definiton to CreatPacketToSend() method
    //Added GetData() method
    //Added sealed keyword
    //Added Packet(int, int, List<string>, int, string)
    //Version 1.2 NOT TESTED
    //Added nodeID field and functionality to manage this field
    //Added GetNodeIDandPortNumber() method (changed GetPortNumber() method)
    //Added SetNodeID(int) method
    //Changed Packet(byte[]) contructor by adding getLabelStack() method to it.
    //Added empty label functionality to PopLabel() and PushLabel() and added new constructor //Added Packet(int, int, int, string)
    //Started creating TestMethod()
    //Version 1.21 NOT TESTED
    //ADDED DESTINATION CLIENT ID functionality - Radek miałeś racje :)

    sealed class Packet
    {
        //<Sosna:>
        //Format pakietu MPLS dla naszego uzytku to string o nastepujacej budowie:
        //
        //IDWezla<int>,Port<int>#Etykieta1<string||int>,Etykieta2,Etykieta3,...EtykietaN-1,EtykietaN#TTL<int>#DestinationClientID#DATA<string>
        //
        //Po DATA możemy dodac jeszcze jedno pole czyli Reszte kodową, ale nie wiem, czy to jest potrzebne,
        //moim zdaniem z pkt widzenia projektu to nie bardzo
        //</Sosna>
        
        private Encoding enc = Encoding.Unicode;
        private char[] delimiters = {'#',',', '|'};
        private string[] packetStrings = null;
        private string[] labelStackarray = null;
        private List<string> labelStack = null;
        private bool gotLabel = false,
                     gotPortAndNodeID = false,
                     decreasedTTL = false; 
        private int TTL,
                    nodeID,
                    port,
                    destinationClientID;
        private string portstring,
            labels,
            TTLstring,
            clientIDstring,
            data;

        public Packet(string packet) 
        {
            string[] prePreparation = packet.Split(delimiters[2]);
            packetStrings = prePreparation[1].Split(delimiters[0]);
            portstring = packetStrings[0];
            labels = packetStrings[1];
            TTLstring = packetStrings[2];
            clientIDstring = packetStrings[3];
            data = packetStrings[4];
            this.getLabelStack();
            this.getNodeIDandPortNumber();
            this.getDestinationClientID();
            this.GetAndDecreaseTTL();
        }

        public Packet(byte[] receivedBytes) 
        {
            string packet = enc.GetString(receivedBytes);
            string[] prePreparation = packet.Split(delimiters[2]);
            packetStrings = prePreparation[1].Split(delimiters[0]);
            portstring = packetStrings[0];
            labels = packetStrings[1];
            TTLstring = packetStrings[2];
            clientIDstring = packetStrings[3];
            data = packetStrings[4];
            this.getLabelStack();
            this.getNodeIDandPortNumber();
            this.getDestinationClientID();
            this.GetAndDecreaseTTL();
            //TEST PURPOSE:
            //Console.WriteLine(packet + "\nPo pierwszym split:");
            //foreach (var item in packetStrings)
            //{
            //    Console.Write(item + " ");
            //}
        }
        //TESTED - WORKED V1.2
        public Packet(int id, int p, List<string> newLabels, int ttl, int clientID, string dataToSend)
        {
            nodeID = id;
            port = p;
            labelStack = newLabels; //TESTED -worked
            TTL = ttl;
            destinationClientID = clientID;
            data = dataToSend;
        }
        //TESTED - WORKED V1.2
        public Packet(int id, int p, int ttl, int clientID, string dataToSend)
        {
            nodeID = id;
            port = p;
            labelStack = new List<string>();
            labelStack.Add(" ");
            TTL = ttl;
            destinationClientID = clientID;
            data = dataToSend;
        }
        public int ReturnDestinationClientID() { return destinationClientID; }
        private int getDestinationClientID() 
        {
            if (int.TryParse(clientIDstring, out destinationClientID))
                return destinationClientID;
            else
                throw new Exception();
        }
        public int ReturnNodeID() { return nodeID; }
        public int ReturnPortNumber() { return port; }
        
        private void getNodeIDandPortNumber() 
        {
            string[] nodeANDport = portstring.Split(delimiters[1]);


            if (int.TryParse(nodeANDport[0], out nodeID) && int.TryParse(nodeANDport[1], out port))
            {
                gotPortAndNodeID = true;
            }
            else
                throw new Exception();
        }

        public bool SetPortNumber(int p) 
        {
            if (gotPortAndNodeID == true)
            {
                port = p;
                return true;
            }
            else
                return false;
        }

        public bool SetNodeID(int id) 
        {
            if (gotPortAndNodeID == true)
            {
                nodeID = id;
                return true;
            }
            else
                return false;
        }

        private void getLabelStack()
        {
            labelStackarray = labels.Split(delimiters[1]);
            labelStack = labelStackarray.ToList<string>();
        }

        public string GetLabel() 
        {
            if (labelStack != null)
            {
                gotLabel = true;
                return labelStack.ElementAt(0);
            }
            else
                throw new Exception();
        }
        
        public bool PopLabel()//przy wyniku false sprawdzic czy etykieta jest pusta
        {
            if (gotLabel == true)
            {
                if (labelStack.Count == 1 && !labelStack.ElementAt(0).Equals(" "))
                    SwapLabel(" ");
                else if (labelStack.Count == 1 && labelStack.ElementAt(0).Equals(" "))
                    return false;
                else
                    labelStack.RemoveAt(0);
                return true;
            }
            else
                return false;
        }

        public bool PushLabel(string label) 
        {
            if (gotLabel == true)
            {
                if (labelStack.ElementAt(0) == " ")
                {
                    labelStack[0] = label;
                    return true;
                }
                else
                {
                    labelStack.Insert(0, label);
                    return true;
                }
            }
            else
                return false;
        }

        public bool SwapLabel(string label) 
        {
            if (gotLabel == true)
            {
                labelStack[0] = label;
                return true;
            }
            else
                return false;
        }
        public int NativeIP() 
        {
            while (true)
            {
                if (!PopLabel())
                    break;
            }
            return getDestinationClientID();
        }

        public int GetAndDecreaseTTL()
        {
            if (decreasedTTL == true)
            {
                return TTL;
            }
            else
            {
                if (int.TryParse(TTLstring, out TTL))
                {
                    decreasedTTL = true;
                    return --TTL; 
                }
                else
                    throw new Exception();
            }
        }

        public string GetData() 
        {
            return data;
        }

        public byte[] CreatePacketToSend() 
        {
            //PACKET|NodeID,Port<int>#Etykieta1<string||int>,Etykieta2,Etykieta3,...EtykietaN-1,EtykietaN#TTL<int>#DestinationClientID#DATA<string>
            StringBuilder sb = new StringBuilder();
            sb.Append(LabelSwitchingRouter.Keywords.PACKET.ToString()).Append("|").Append(nodeID).Append(",").Append(port).Append("#");
            foreach (var label in labelStack)
            {
                sb.Append(label).Append(",");
            }
            sb.Remove(sb.Length - 1, 1).Append("#").Append(TTL).Append("#").Append(destinationClientID).Append("#").Append(data);
            //Console.WriteLine(sb.ToString()); //wykomentowac po testach
            return enc.GetBytes(sb.ToString());
        }

        

        public void PushLabelStack(string labelStackString)
        {
            string[] array = labelStackString.Split(delimiters[0]);
            foreach (var label in array)
            {
                this.PushLabel(label);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("NodeID: ").Append(nodeID).Append(" Port: ").Append(port).Append(" LabelStack: ");
            foreach (var label in labelStack)
            {
                sb.Append(label).Append(" ");
            }
            sb.Append("TTL: ").Append(TTL);
            sb.Append("DestinationClientId: ").Append(getDestinationClientID());
            sb.Append("Dane: ").Append(data);
            return sb.ToString();
        }
    }
}
