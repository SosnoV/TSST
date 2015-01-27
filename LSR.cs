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
        internal int managerPortNumber { get; private set; }
        internal int nodeID { get; private set; }
        internal CommutationField CF = null;
        internal Queue<Packet> LSR_FIFO = null;
        private char[] delimiters = { ' ' };
        private UnicodeEncoding enc = new UnicodeEncoding();
        private Communication communicationModule = null;
        private Thread servingFIFO;
        private Thread timer;

        //dodane:
        private int maximalBandwithIn;
        private int maximalBandwithOut;
        private int interfacesIn = 4;
        private int interfacesOut = 4;
        private int[] bandwithIfIn;
        private int[] bandwithIfOut;

        private bool loadLSRConfiguration(string nID = "0")
        {
            //portNumber = CsharpMPLS.Parser.ParseLSRConfiguration(nID);
            //if(portNumber == 0)
            //    return false;
            int managerPort = 0, wiresPort = 0;
            portNumber = Parser.ParseConfiguration(nID, out managerPort, out wiresPort);
            if (managerPort == 0 || wiresPort == 0 || portNumber == 0)
                return false;
            else
            {
                managerPortNumber = managerPort;
                wiresPortNumber = wiresPort;
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
            LSR_FIFO = new Queue<CsharpMPLS.Packet>();
            communicationModule = new Communication(portNumber, this);
            bandwithIfIn = new int[interfacesIn];
            bandwithIfOut = new int[interfacesOut];
            for (int i = 0; i < interfacesIn; i++)
            {
                bandwithIfIn[i] = 0;
            }
            for (int i = 0; i < interfacesOut; i++)
            {
                bandwithIfOut[i] = 0;
            }
            Console.WriteLine("/////////////// LSR NODE NUMBER: " + nodeID + "///////////////");
        }

        public void Run()
        {
            servingFIFO = new Thread(ServeFIFO);
            servingFIFO.IsBackground = true;
            timer = new Thread(KeepAlive);
            timer.IsBackground = true;
            communicationModule.Start();
            servingFIFO.Start();
            timer.Start();

            string register = Keywords.REGISTER.ToString() + "#" + nodeID.ToString() + "#" + portNumber.ToString();
            communicationModule.Send(managerPortNumber, enc.GetBytes(register));
        }
        private void KeepAlive()
        {
            string keepAlive = Keywords.KEEPALIVE.ToString() + "#" + nodeID.ToString();
            while (true)
            {
                Thread.Sleep(10000);
                communicationModule.Send(managerPortNumber, enc.GetBytes(keepAlive));
            }
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
            else if (cmdString.Contains(Keywords.SET.ToString()))
                return Keywords.SET;
            else if (cmdString.Contains(Keywords.GET.ToString()))
                return Keywords.GET;
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
            switch (keyword)
            {
                case Keywords.PACKET:
                    Console.WriteLine("Serve: " + keyword.ToString());
                    Packet _packet = new Packet(cmdString);
                    LSR_FIFO.Enqueue(_packet);
                    break;
                case Keywords.GET:
                    Console.WriteLine("Serve: " + keyword.ToString());
                    toSend = ServeGetRequest(cmdString);
                    break;
                case Keywords.SET:
                    Console.WriteLine("Serve: " + keyword.ToString());
                    toSend = ServeSetRequest(cmdString);
                    break;
                case Keywords.DELETE:
                    Console.WriteLine("Serve: " + keyword.ToString());
                    toSend = ServeDeleteRequest(cmdString);
                    break;
                default:
                    break;
            }
            if (toSend != null)
            {
                Console.WriteLine("To send: " + toSend);
                communicationModule.Send(managerPortNumber, enc.GetBytes(toSend));
            }
        }

        internal string ServeSetRequest(string cmd)
        {
            string[] array = cmd.Split(delimiters[0]);
            //foreach (var item in array)
            //{
            //    Console.WriteLine(item + "|");
            //}
            if (cmd.Contains(GETSETKeywords.INTAB.ToString()))
            {
                if (array.Length == 7)
                {

                    if ((CF.addInputEntryToInputTable(int.Parse(array[2]), array[3], int.Parse(array[4]), int.Parse(array[5]))) == false)
                        return null;
                    Console.WriteLine("added InputTable entry");
                }
                else
                {
                    if ((CF.addInputEntryToInputTable(int.Parse(array[2]), " ", int.Parse(array[3]), int.Parse(array[4]))) == false)
                        return null;
                    Console.WriteLine("added InputTable entry");
                }
            }
            else if (cmd.Contains(GETSETKeywords.OUTTAB.ToString()))
            {
                if (bool.Parse(array[4]) == true)//PushTopLabel == true
                {
                    if ((CF.addOutputTableEntryToOutputTable(int.Parse(array[2]), int.Parse(array[3]), bool.Parse(array[4]), array[5])) == false)
                        return null;
                    Console.WriteLine("added OutputTable entry");
                }
                else //PushTopLabel == false
                {
                    if ((CF.addOutputTableEntryToOutputTable(int.Parse(array[2]), int.Parse(array[3]), bool.Parse(array[4]))) == false)
                        return null;
                    Console.WriteLine("added OutputTable entry");
                }
            }
            else if (cmd.Contains(GETSETKeywords.XC.ToString()))
            {
                if ((CF.addXCEntryToXCTable(int.Parse(array[2]), int.Parse(array[3]), int.Parse(array[4]))) == false)
                    return null;
                Console.WriteLine("added XCputTable entry");
            }
            else if (cmd.Contains(GETSETKeywords.LS.ToString()))
            {
                if ((CF.addLabelStackEntryToLabelStackTable(int.Parse(array[2]), array[3])) == false)
                    return null;
                Console.WriteLine("added LabelStackTable entry");
            }
            else if (cmd.Contains(GETSETKeywords.IP.ToString()))
            {
                if ((CF.addIPTableEntryToIPTable(int.Parse(array[2]), int.Parse(array[3]))) == false)
                    return null;
                Console.WriteLine("added IPTable entry");
            }
            else if (cmd.Contains(GETSETKeywords.IFIN.ToString()))
            {
                try
                {
                    int index = int.Parse(array[2]) - 1;
                    bandwithIfIn[index] += int.Parse(array[3]);
                    Console.WriteLine("Added InputInterface Bandwith. Total: " + bandwithIfIn[index]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
            else if (cmd.Contains(GETSETKeywords.IFOUT.ToString()))
            {
                try
                {
                    int index = int.Parse(array[2]) - 1;
                    bandwithIfOut[index] += int.Parse(array[3]);
                    Console.WriteLine("Added OutputInterface Bandwith. Total: " + bandwithIfOut[index]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
            return ServeGetRequest(cmd);
        }

        internal string ServeGetRequest(string cmd) //zmienic dla in => rozna dlugosc tablic
        {
            string[] array = cmd.Split(delimiters[0]);
            string toAppend = null;
            StringBuilder sb = new StringBuilder();
            sb.Append(nodeID).Append(" ").Append(Keywords.RESP.ToString()).Append(" ");
            if (cmd.Contains(GETSETKeywords.INTAB.ToString()))
            {
                try
                {
                    if (array.Length == 7)
                    {
                        if ((toAppend = CF.GetInEntry(int.Parse(array[2]), array[3])) == null)
                            throw new Exception();
                    }
                    else
                    {
                        if ((toAppend = CF.GetInEntry(int.Parse(array[2]), " ")) == null)
                            throw new Exception();
                    }
                    sb.Append(toAppend);
                }
                catch (Exception)
                {
                    Console.WriteLine("Cannot Get IN");
                }

            }
            else if (cmd.Contains(GETSETKeywords.OUTTAB.ToString()))
            {
                //sb.Append(CF.GetOutEntry(int.Parse(array[2])));
                try
                {
                    if ((toAppend = CF.GetOutEntry(int.Parse(array[2]))) == null)
                        throw new Exception();
                    else
                        sb.Append(toAppend);
                }
                catch (Exception)
                {
                    Console.WriteLine("Cannot Get OUT");
                }
            }
            else if (cmd.Contains(GETSETKeywords.XC.ToString()))
            {
                //sb.Append(CF.GetXCEntry(int.Parse(array[2])));
                try
                {
                    if ((toAppend = CF.GetXCEntry(int.Parse(array[2]))) == null)
                        throw new Exception();
                    else
                        sb.Append(toAppend);
                }
                catch (Exception)
                {
                    Console.WriteLine("Cannot Get XC");
                }
            }
            else if (cmd.Contains(GETSETKeywords.LS.ToString()))
            {
                //sb.Append(CF.GetLSEntry(int.Parse(array[2])));
                try
                {
                    if ((toAppend = CF.GetLSEntry(int.Parse(array[2]))) == null)
                        throw new Exception();
                    else
                        sb.Append(toAppend);
                }
                catch (Exception)
                {
                    Console.WriteLine("Cannot Get LS");
                }
            }
            else if (cmd.Contains(GETSETKeywords.IP.ToString()))
            {
                //sb.Append(CF.GetIPEntry(int.Parse(array[2])));
                try
                {
                    if ((toAppend = CF.GetIPEntry(int.Parse(array[2]))) == null)
                        throw new Exception();
                    else
                        sb.Append(toAppend);
                }
                catch (Exception)
                {
                    Console.WriteLine("Cannot Get IP");
                }
            }
            else if (cmd.Contains(GETSETKeywords.IFIN.ToString()))
            {
                try
                {
                    int index = int.Parse(array[2]) - 1;
                    //bandwithIfIn[index] += int.Parse(array[3]);
                    int interfaceIn = index + 1;//to da numer interfejsu
                    sb.Append(GETSETKeywords.IFIN.ToString()).Append(" ").
                        Append(interfaceIn).Append(" ").Append(bandwithIfIn[index]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Cannot Get IFIN");
                }
            }
            else if (cmd.Contains(GETSETKeywords.IFOUT.ToString()))
            {
                try
                {
                    int index = int.Parse(array[2]) - 1;
                    //bandwithIfOut[index] += int.Parse(array[3]);
                    int interfaceOut = index + 1;//to da numer interfejsu
                    sb.Append(GETSETKeywords.IFOUT.ToString()).Append(" ").
                        Append(interfaceOut).Append(" ").Append(bandwithIfOut[index]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Cannot Get IFOUT");
                }
            }
            return sb.ToString();
        }

        internal string ServeDeleteRequest(string cmd)
        {
            string[] array = cmd.Split(delimiters[0]);
            int index = int.Parse(array[2]);
            StringBuilder sb = new StringBuilder();
            sb.Append(nodeID).Append(" ").Append(Keywords.RESP.ToString()).Append(" ").Append(Keywords.DELETE.ToString()).Append(" ");
            if (cmd.Contains(GETSETKeywords.INTAB.ToString()))
            {
                if (CF.deleteInEntry(index, array[3]))
                    sb.Append(GETSETKeywords.INTAB.ToString()).Append(" ").Append(index).Append(" ").Append(array[3]);
            }
            else if (cmd.Contains(GETSETKeywords.OUTTAB.ToString()))
            {
                if (CF.deleteOutputEntry(index))
                    sb.Append(GETSETKeywords.OUTTAB.ToString()).Append(" ").Append(index);
            }
            else if (cmd.Contains(GETSETKeywords.XC.ToString()))
            {
                if (CF.deleteXCEntry(index))
                    sb.Append(GETSETKeywords.XC.ToString()).Append(" ").Append(index);
            }
            else if (cmd.Contains(GETSETKeywords.LS.ToString()))
            {
                if (CF.deleteLabelStackEntry(index))
                    sb.Append(GETSETKeywords.LS.ToString()).Append(" ").Append(index);
            }
            else if (cmd.Contains(GETSETKeywords.IP.ToString()))
            {
                if (CF.deleteIPEntry(index))
                    sb.Append(GETSETKeywords.IP.ToString()).Append(" ").Append(index);
            }
            else if (cmd.Contains(GETSETKeywords.IFIN.ToString()))
            {
                try
                {
                    index -= 1;
                    bandwithIfIn[index] -= int.Parse(array[3]);
                    Console.WriteLine("Deleted InputInterface Bandwith. Total: " + bandwithIfIn[index]);
                    sb.Append(GETSETKeywords.IFIN.ToString()).Append(" ").Append(++index);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
            else if (cmd.Contains(GETSETKeywords.IFOUT.ToString()))
            {
                try
                {
                    index -= 1;
                    bandwithIfOut[index] -= int.Parse(array[3]);
                    Console.WriteLine("Deleted OutputInterface Bandwith. Total: " + bandwithIfOut[index]);
                    sb.Append(GETSETKeywords.IFOUT.ToString()).Append(" ").Append(++index);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
            return sb.ToString();
           }
        }

        public enum GETSETKeywords
        {
            INTAB,
            OUTTAB,
            XC,
            LS,
            IP,
            IFIN,
            IFOUT
        }

        public enum Keywords
        {
            PACKET,
            GET,
            SET,
            DELETE,
            GETRESP,
            SETRESP,
            RESP,
            KEEPALIVE,
            REGISTER,
            DEFAULT
        }
}
