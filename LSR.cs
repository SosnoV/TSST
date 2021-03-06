﻿using CsharpMPLS;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LabelSwitchingRouter
{
    class LSR
    {
        internal int portNumber { get; private set; }
        internal int wiresPortNumber { get; private set; }
        internal int RCPortNumber { get; private set; }
        internal int nodeID { get; private set; }
        private int ASN;
        private int timeout = 20000;
        internal CommutationField CF = null;
        internal LRM lrm = null;
        internal Queue<Packet> LSR_FIFO = null;
        private char[] delimiters = { ' ' };
        private UnicodeEncoding enc = new UnicodeEncoding();
        private Communication communicationModule = null;
        private Thread servingFIFO;
        private Thread timer;
        private Thread broadcast;
        //private List<string> neighbours = null;
        //private Dictionary<string, int> nodesToPorts = null;
        private bool registered = false;

        /// <summary>
        /// Maps NodeId to Port connected with that node
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        internal int Translate(string nodeId) 
        {
            int port = 0;
            lrm.nodesToPorts.TryGetValue(nodeId, out port);
            return port;
        }

        private bool loadLSRConfiguration(string nID = "0")
        {
            //portNumber = CsharpMPLS.Parser.ParseLSRConfiguration(nID);
            //if(portNumber == 0)
            //    return false;
            int managerPort = 0, wiresPort = 0;
            portNumber = Parser.ParseConfiguration(nID, out managerPort, out wiresPort);
            if (managerPort == 0 || wiresPort == 0 || portNumber == 0)
            {
                Console.WriteLine("Error with ports");
                return false;
            }
            else
            {
                RCPortNumber = managerPort;
                wiresPortNumber = wiresPort;
                ASN = RCPortNumber;
                return true;
            }
        }

        public LSR(string id)
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
            lrm = new LRM(nodeID);
            LSR_FIFO = new Queue<CsharpMPLS.Packet>();
            //neighbours = new List<string>();
            //nodesToPorts = new Dictionary<string, int>();
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
            //timer.Start();

            //string register = Keywords.REGISTER.ToString() + " " + nodeID.ToString() + " " + portNumber.ToString();
            //communicationModule.Send(RCPortNumber, Register());
        }
        private void KeepAlive()
        {
            Console.WriteLine("Timer started");
            string keepAlive = Keywords.KEEP.ToString() + " " + nodeID.ToString();
            while (true)
            {
                Thread.Sleep(10000);
                Console.WriteLine(keepAlive);
                communicationModule.Send(RCPortNumber, enc.GetBytes(keepAlive));
            }
        }


        private void Register()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Keywords.REGISTER.ToString()).Append(" ").Append(nodeID).Append(" ").
                Append(portNumber).Append(" ");
            var neighbours = lrm.nodesToPorts.Keys;
            if (neighbours.Count > 0)
            {
                foreach (var item in neighbours)
                {
                    sb.Append(item).Append("#");
                }
                sb.Remove(sb.Length - 1, 1);
            }
            string msg = sb.ToString();
            Console.WriteLine("Register() method" + msg);
            communicationModule.Send(RCPortNumber, enc.GetBytes(msg));
        }

        //private void Broadcast()
        //{
        //    Random gen = new Random();
        //    int miliseconds = gen.Next(5, 12) * 1000;
        //    string msg = Keywords.BROADCAST.ToString() + " " + nodeID + " ";
        //    byte[] broadcastBytes = enc.GetBytes(msg);
        //    Thread.Sleep(miliseconds);
        //    communicationModule.Send(wiresPortNumber, broadcastBytes);
        //    Thread.Sleep(timeout);
        //    Register();
        //    timer.Start(); 
        //}

        private void Broadcast()
        {
            Random gen = new Random();
            int miliseconds = gen.Next(5, 12) * 1000;
            string msg = Keywords.BROADCAST.ToString() + " " + nodeID + " " + ASN;
            byte[] broadcastBytes = enc.GetBytes(msg);
            Thread.Sleep(miliseconds);
            communicationModule.Send(wiresPortNumber, broadcastBytes);
            Thread.Sleep(timeout);
            Register();
            registered = true;
            timer.Start();
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
            else if (cmdString.Contains(Keywords.BRESP.ToString()))
                return Keywords.BRESP;
            else if (cmdString.Contains(Keywords.RESERVE.ToString()))
                return Keywords.RESERVE;
            else if (cmdString.Contains(Keywords.DISRESERVE.ToString()))
                return Keywords.DISRESERVE;
            else if (cmdString.Contains(Keywords.SET.ToString()))
                return Keywords.SET;
            else if (cmdString.Contains(Keywords.DELETE.ToString()))
                return Keywords.DELETE;
            else if (cmdString.Contains(Keywords.YELL.ToString()))
                return Keywords.YELL;
            else
                return Keywords.DEFAULT;
        }

        internal void ServeRequest(byte[] command)
        {
            string cmdString = null;
            //string toSend = null;
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
                //case Keywords.BRESP:
                //    ServeBresp(cmdString);
                //    break;
                case Keywords.DISRESERVE:
                    ServeDisreserve(cmdString);
                    break;
                case Keywords.RESERVE:
                    ServeReserve(cmdString);
                    break;
                case Keywords.YELL:
                    ServeYell();
                    break;
                default:
                    break;
            }

        }

        private void ServeYell()
        {
            Random gen = new Random();
            int miliseconds = gen.Next(1, 5) * 1000;
            string msg = Keywords.BROADCAST.ToString() + " " + nodeID + " " + ASN;
            byte[] broadcastBytes = enc.GetBytes(msg);
            Thread.Sleep(miliseconds);
            communicationModule.Send(wiresPortNumber, broadcastBytes);
            return;
        }

        private void ServeReserve(string cmd)
        {
            Console.WriteLine("Serving RESERVE: " + cmd);
            string[] array = cmd.Split(delimiters[0]);
            string label = lrm.Reserve(array[1], Translate(array[1]), double.Parse(array[2]), array[3]);
            StringBuilder sb = new StringBuilder();
            sb.Append(Keywords.RESPONSE.ToString()).Append(" ").Append(nodeID).Append(" ").Append(array[3]).
                Append(" ").Append(label);
            communicationModule.Send(RCPortNumber, enc.GetBytes(sb.ToString()));
            return;
        }

        private void ServeDisreserve(string cmd)
        {
            Console.WriteLine("Serving DISRESERVE: " + cmd);
            string[] array = cmd.Split(delimiters[0]);
            lrm.Disreserve(array[1]);
            return;
        }

        //private void ServeBresp(string cmd)
        //{
        //    Console.WriteLine("Serving BRESP: " + cmd);
        //    string[] array = cmd.Split(delimiters[0]);
        //    //array 0 bresp
        //    //array 1 port przysyłajacego
        //    //array 2 nodeId sąsiada
        //    //array 3 port do tego sasiada
        //    //array 4 ASN sąsiada
        //    nodesToPorts.Add(array[2], int.Parse(array[3]));
        //    StringBuilder sb = new StringBuilder();
        //    if(array[4].Equals(ASN.ToString()))
        //        sb.Append(Keywords.NEIGHBOUR.ToString()).Append(" ").Append(nodeID).Append(" ").Append(array[2]);
        //    else
        //        sb.Append(Keywords.ENEIGHBOUR.ToString()).Append(" ").Append(nodeID).Append(" ").
        //            Append(array[2]).Append(" ").Append(array[4]);
        //    Console.WriteLine("SERVED BRESP, MSG TO RC: " + sb.ToString());
        //    communicationModule.Send(RCPortNumber, enc.GetBytes(sb.ToString()));
        //    return;
        //}

        //private void ServeReserve(string cmd)
        //{
        //    Console.WriteLine("Serving RESERVE: " + cmd);
        //    string[] array = cmd.Split(delimiters[0]);
        //    if (bool.Parse(array[3]))
        //    {
        //        string message = Keywords.CCRESPONSE.ToString() + " " + nodeID + " ";
        //        // string message = Keywords.CCRESPONSE.ToString() + " " + array[1] + " ";
        //        if (lrm.Reserve(true, Translate(array[1]), double.Parse(array[2])))
        //        {
        //            message += "YES";
        //        }
        //        else
        //        {
        //            message += "NO";
        //        }
        //        communicationModule.Send(RCPortNumber, enc.GetBytes(message));
        //    }
        //    else
        //    {
        //        if (lrm.Reserve(false, Translate(array[1]), int.Parse(array[2])))
        //            Console.WriteLine("DISRESERVE DONE");
        //        else
        //            Console.WriteLine("DISRESERVE UPS");
        //    }
        //}

        //private void ServeBroadcast(string cmd)
        //{
        //    Console.WriteLine("Serving BROADCAST: " + cmd);
        //    string[] array = cmd.Split(delimiters[0]);
        //    //array 0 broadcast
        //    //array 1 nodeid przysyłajacego
        //    //array 2 port przysylajacego
        //    //array 3 port tego (przyjmujacego
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(Keywords.BRESP.ToString()).Append(" ").Append(array[3]).Append(" ").Append(nodeID).
        //        Append(" ").Append(array[2]).Append(" ").Append(ASN);
        //    communicationModule.Send(wiresPortNumber, enc.GetBytes(sb.ToString()));
        //    return;
        //}

        private void ServeBroadcast(string cmd)
        {
            Console.WriteLine("Serving BROADCAST: " + cmd);
 	        string[] array = cmd.Split(delimiters[0]);
            //array 0 broadcast
            //array 1 nodeid przysyłajacego
            //array 2 ASN
            //array 3 port tego odbierajacego
            try
            {
                lrm.nodesToPorts.Add(array[1], int.Parse(array[3]));
            }
            catch (ArgumentException) 
            {
                return;
            }
            if (!registered)
                return;
            StringBuilder sb = new StringBuilder();
            int asn = int.Parse(array[2]);
            if (asn == ASN || asn == 0)
            {
                sb.Append(Keywords.NEIGHBOUR.ToString()).Append(" ").Append(nodeID).Append(" ").Append(array[1]);
            }
            else
            {
                sb.Append(Keywords.ENEIGHBOUR.ToString()).Append(" ").Append(nodeID).Append(" ").
                    Append(array[1]).Append(" ").Append(array[2]);
            }
            Console.WriteLine("SERVED BROADCAST, MSG TO RC: " + sb.ToString());
            communicationModule.Send(RCPortNumber, enc.GetBytes(sb.ToString()));
            return;
        }

        internal void ServeSetRequest(string cmd)
        {
            Console.WriteLine("Serving SET: " + cmd);
            string[] array = cmd.Split(delimiters[0]);
            if (CF.addInputEntryToInputTable(Translate(array[1]), array[2],
                Translate(array[3]), array[4]))
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
            Console.WriteLine("Serving DELETE: " + cmd);
            string[] array = cmd.Split(delimiters[0]);
            if (CF.deleteInEntry(Translate(array[1]), array[2]))
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
    }
        public enum Keywords
        {
            YELL,
            BROADCAST,
            BRESP,
            NEIGHBOUR,
            ENEIGHBOUR,
            RESERVE,
            DISRESERVE,
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

