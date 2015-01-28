using CsharpMPLS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LabelSwitchingRouter
{
    class LSR
    {
        internal int portNumber { get; private set; }
        internal int wiresPortNumber { get; private set; }
        internal int RCPortNumber { get; private set; }
        internal int nodeID { get; private set; }
        private int a;
        internal CommutationField CF = null;
        internal LRM lrm = null;
        internal Queue<Packet> LSR_FIFO = null;
        private char[] delimiters = { ' ' };
        private UnicodeEncoding enc = new UnicodeEncoding();
        private Communication communicationModule = null;
        private Thread servingFIFO;
        private Thread timer;
        private Thread broadcast;
        private List<string> neighbours = null;
        


        private bool loadLSRConfiguration(string nID = "0")
        {
            //portNumber = CsharpMPLS.Parser.ParseLSRConfiguration(nID);
            //if(portNumber == 0)
            //    return false;
            int managerPort = 0, wiresPort = 0, avb = 0;
            portNumber = Parser.ParseConfiguration(nID, out managerPort, out wiresPort, out avb);
            if (managerPort == 0 || wiresPort == 0 || portNumber == 0)
                return false;
            else
            {
                RCPortNumber = managerPort;
                wiresPortNumber = wiresPort;
                a = avb;
                return true;
            }
        }

        public LSR(string id = "0")
        {
            try
            {
                if (!loadLSRConfiguration(id))
                    throw new Exception();
            }
            catch (Exception)
            {
                Console.WriteLine("Error loading configuration. LSR constructor");
            }
            nodeID = int.Parse(id);
            CF = new CommutationField(this);
            lrm = new LRM(a);
            LSR_FIFO = new Queue<CsharpMPLS.Packet>();
            neighbours = new List<string>();
            communicationModule = new Communication(portNumber, this);
            Console.WriteLine("/////////////// LSR NODE NUMBER: " + nodeID + "///////////////");
        }

        public void Run()
        {
            servingFIFO = new Thread(ServeFIFO);
            servingFIFO.IsBackground = true;
            timer = new Thread(KeepAlive);
            timer.IsBackground = true;
            broadcast = new Thread(Broadcast);
            broadcast.IsBackground = true;
            

            communicationModule.Start();
            servingFIFO.Start();
            broadcast.Start();
            timer.Start();

            //string register = Keywords.REGISTER.ToString() + " " + nodeID.ToString() + " " + portNumber.ToString();
            communicationModule.Send(RCPortNumber, Register());
        }
        private void KeepAlive()
        {
            string keepAlive = Keywords.KEEP.ToString() + " " + nodeID.ToString();
            while (true)
            {
                Thread.Sleep(10000);
                communicationModule.Send(RCPortNumber, enc.GetBytes(keepAlive));
            }
        }


        private byte[] Register() 
        {
            return enc.GetBytes(Keywords.REGISTER.ToString() + " " + nodeID.ToString() + " " + portNumber.ToString());
        }

        private void Broadcast()
        {
            Random gen = new Random();
            int miliseconds = gen.Next(5, 15) * 1000;
            string msg = Keywords.BROADCAST.ToString() + " " + nodeID;
            byte[] msgBytes = enc.GetBytes(msg);
            Thread.Sleep(miliseconds);
            communicationModule.Send(wiresPortNumber, msgBytes);
        }
        public void ServeFIFO()
        {
            while (true)
            {
                if (LSR_FIFO.Count != 0)
                {
                    Console.WriteLine("Serving Packet from FIFO");
                    try
                    {
                        communicationModule.Send(wiresPortNumber, CF.DealWithPacket(LSR_FIFO.Dequeue()));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Cannot deal with packet");
                        Console.WriteLine(e.Message);
                    }

                }
                else
                {
                    Thread.Sleep(10);
                }
            }

        }

        internal Keywords DecisionMethod(byte[] command, out string cmdString)
        {

            UnicodeEncoding enc = new UnicodeEncoding();
            cmdString = enc.GetString(command);
            if (cmdString.Contains(Keywords.PACKET.ToString()))
                return Keywords.PACKET;
            else if (cmdString.Contains(Keywords.BROADCAST.ToString()))
                return Keywords.BROADCAST;
            else if (cmdString.Contains(Keywords.RESERVE.ToString()))
                return Keywords.RESERVE;
            else if (cmdString.Contains(Keywords.SET.ToString()))
                return Keywords.SET;
            else if (cmdString.Contains(Keywords.DELETE.ToString()))
                return Keywords.DELETE;
            else
                return Keywords.DEFAULT;
        }

        internal void ServeRequest(byte[] command)
        {
            string cmdString = null;
            string toSend = null;
            Keywords keyword = this.DecisionMethod(command, out cmdString);
            Console.WriteLine("Serve: " + keyword.ToString());
            switch (keyword)
            {
                case Keywords.PACKET:  
                    Packet _packet = new Packet(cmdString);
                    LSR_FIFO.Enqueue(_packet);
                    break;
                case Keywords.SET:
                    ServeSetRequest(cmdString);
                    break;
                case Keywords.DELETE:
                    ServeDeleteRequest(cmdString); 
                    break;
                case Keywords.BROADCAST:
                    ServeBroadcast(cmdString);
                    break;
                case Keywords.RESERVE:
                    ServeReserve(cmdString);
                    break;
                default:
                    break;
            }
            if (toSend != null)
            {
                Console.WriteLine("To send: " + toSend);
                communicationModule.Send(RCPortNumber, enc.GetBytes(toSend));
            }
        }

        private void ServeReserve(string cmd)
        {
            string[] array = cmd.Split(delimiters[0]);
            if(bool.Parse(array[3]))
            {
                string message = Keywords.CCRESPONSE.ToString() + " " + nodeID + " ";
                // string message = Keywords.CCRESPONSE.ToString() + " " + array[1] + " ";
                if(lrm.Reserve(true, array[1], int.Parse(array[2])))
                {
                    message += "YES";
                }
                else
                    message += "NO";
                communicationModule.Send(RCPortNumber, enc.GetBytes(message);
            }
            else
                lrm.Reserve(false, array[1], int.Parse(array[2]));
        }

        private void ServeBroadcast(string cmd)
        {
 	        string[] array = cmd.Split(delimiters[0]);
            foreach (var neigh in neighbours)
	        {
		        if(array[1].Equals(neigh))
                    return;
	        }
            neighbours.Add(array[1]);
            lrm.AddNeighbourAVB(array[1]);
            StringBuilder sb = new StringBuilder();
            sb.Append(Keywords.NEIGHBOUR.ToString()).Append(" ").Append(array[1]);
            //foreach (var item in neighbours)
            //{
            //    sb.Append(item).Append("#");
            //}
            //sb.Remove(sb.Length-1, 1);
            communicationModule.Send(RCPortNumber, enc.GetBytes(sb.ToString());
            return;
        }

        internal void ServeSetRequest(string cmd)
        {
            string[] array = cmd.Split(delimiters[0]);
            if (CF.addInputEntryToInputTable(int.Parse(array[1]), array[2], int.Parse(array[3]), array[4]))
            {
                Console.WriteLine("SET obsłużone pomyślnie");
            }
            else
            {
                Console.WriteLine("SET nie obsłużone");
            }
            return;
        }

       

        internal void ServeDeleteRequest(string cmd)
        {
            string[] array = cmd.Split(delimiters[0]);
            if(CF.deleteInEntry(int.Parse(array[1]), array[2]))
            {
                Console.WriteLine("DELETE obsłużone pomyślnie");
            }
            else
            {
                Console.WriteLine("DELETE nie obsłużone");
            }
            return;
        }

        //public enum GETSETKeywords
        //{
        //    INTAB,
        //    OUTTAB,
        //    XC,
        //    LS,
        //    IP,

        //}

        public enum Keywords
        {
            BROADCAST,
            NEIGHBOUR,
            RESERVE,
            PACKET,
            SET,
            DELETE,
            RESPONSE,
            CCRESPONSE,
            KEEP,
            REGISTER,
            DEFAULT
        }
}
