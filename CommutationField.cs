using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelSwitchingRouter
{
    class CommutationField
    {
        private string space = " ";
        private LSR lsr = null;
        private List<InputTableEntry> InputTable = null;
        internal class InputTableEntry 
        {
            public int InInterface;
            public string InLabel;
            public int NPop;
            public int XCIndex;

            public InputTableEntry(int inI, string InL, int nPop, int xcindex) 
            {
                InInterface = inI;
                InLabel = InL;
                NPop = nPop;
                XCIndex = xcindex;
            }
            public override string ToString()
            {

                return base.ToString();
            }

            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType()) return false;
                var objAsEntry = obj as InputTableEntry;
                if (objAsEntry == null) return false;
                if ((this.InInterface == objAsEntry.InInterface) && (this.InLabel.Equals(objAsEntry.InLabel)))
                    return true;
                else
                    return false;
            }
        }
        
        private struct XCTableEntry 
        {
            public int OutTableIndex;
            public int LabelStackTableIndex;
        }
        private Dictionary<int, XCTableEntry> XCTable = null;
        
        private struct OutputTableEntry
        {
           public int OutInterface;
           public string OutTopLabel;
           public bool PushTopLabel;
        }
        private Dictionary<int, OutputTableEntry> OutputTable = null;
        
        private Dictionary<int, string> LabelStackTable = null;

        private Dictionary<int, int> IPTable = null;
        
        internal CommutationField(LSR router) 
        {
            lsr = router;
            InputTable = new List<InputTableEntry>();
            XCTable = new Dictionary<int,XCTableEntry>();
            OutputTable = new Dictionary<int, OutputTableEntry>();
            LabelStackTable = new Dictionary<int, string>();
            IPTable = new Dictionary<int, int>();
        }

        internal byte[] DealWithPacket(CsharpMPLS.Packet _packet) 
        {
            _packet.SetNodeID(lsr.nodeID); //ustawienie id wezla w pakiecie
            if (_packet.GetAndDecreaseTTL() < 0) //TTL sprawdzenie
                throw new Exception();
            InputTableEntry entry = null;
            try
            {
                 entry = this.findRow(_packet.ReturnPortNumber(), _packet.GetLabel());
                 Console.WriteLine("DealWithPacket:" + _packet.GetLabel() + "<<<LABEL");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw new Exception();
            }
            if (entry.XCIndex == 0 || (_packet.GetLabel().Equals(" ") && entry.XCIndex == 0))
            {
                _packet.SetPortNumber(IPTable[_packet.NativeIP()]);
                Console.WriteLine(_packet.ToString());
                return _packet.CreatePacketToSend();
            }
            for (int i = 0; i < entry.NPop; i++) //wykonanie POP n razy
            {
                _packet.PopLabel();
            }
            XCTableEntry xcEntry;
            if (!XCTable.TryGetValue(entry.XCIndex, out xcEntry))// proba pobrania XCTableEntry
                throw new Exception();
            if (xcEntry.LabelStackTableIndex != 0)//wrzuca label stack
            {
                try
                {
                    _packet.PushLabelStack(LabelStackTable[xcEntry.LabelStackTableIndex]);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in adding stack label  " + e.Message);
                    throw new Exception();
                }
            }
            OutputTableEntry outputEntry;
            try
            {
                if (OutputTable.TryGetValue(xcEntry.OutTableIndex, out outputEntry))
                    _packet.SetPortNumber(outputEntry.OutInterface); // ustawienie numeru portu
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw new Exception();
            }
            try
            {
                if (outputEntry.PushTopLabel)
                    _packet.PushLabel(outputEntry.OutTopLabel); //wsadzenie topowej etykiety
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw new Exception();
            }
            Console.WriteLine(_packet.ToString());
            return _packet.CreatePacketToSend();
        }

        internal bool addIPTableEntryToIPTable(int index, int port) 
        {
            if (IPTable.ContainsKey(index))
                IPTable.Remove(index);
            try
            {
                IPTable.Add(index, port);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Error IPTableEntry adding");
                return false;
            }
            return true;
        }
        internal bool addLabelStackEntryToLabelStackTable(int index, string stack) 
        {
            if (LabelStackTable.ContainsKey(index))
                LabelStackTable.Remove(index);
            try
            {
                LabelStackTable.Add(index, stack);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Error LabelStackEntry adding");
                return false;
            }
            return true;
        }
        internal bool addOutputTableEntryToOutputTable(int index, int outI, bool ifPush, string outL = " ")
        {
            if (OutputTable.ContainsKey(index))
                OutputTable.Remove(index);
            OutputTableEntry entry = new OutputTableEntry();
            try
            {
                entry.OutInterface = outI;
                if (ifPush == true)
                    entry.OutTopLabel = outL;
                entry.PushTopLabel = ifPush;
                OutputTable.Add(index, entry);
            }
            catch (Exception)
            {
                Console.WriteLine("Adding OutputTableEntry gone wrong");
                return false;
            }
            return true;  

        }
        internal bool addXCEntryToXCTable(int index, int outIndex, int lsIndex)
        {
            if (XCTable.ContainsKey(index))
                XCTable.Remove(index);
            XCTableEntry entry = new XCTableEntry();
            try
            {
                entry.OutTableIndex = outIndex;
                entry.LabelStackTableIndex = lsIndex;
                XCTable.Add(index, entry);
            }
            catch(Exception) 
            {
                Console.WriteLine("Adding XCTableEntry gone wrong");
                return false;
            }
            return true;  

        }

        internal bool deleteXCEntry(int index) 
        {
            if (XCTable.ContainsKey(index))
            {
                XCTable.Remove(index);
                return true;
            }
            else
                return true;

        }
        internal bool deleteIPEntry(int index)
        {
            if (IPTable.ContainsKey(index))
            {
                IPTable.Remove(index);
                return true;
            }
            else
                return true;

        }
        internal bool deleteOutputEntry(int index)
        {
            if (OutputTable.ContainsKey(index))
            {
                OutputTable.Remove(index);
                return true;
            }
            else
                return true;

        }
        internal bool deleteLabelStackEntry(int index)
        {
            if (LabelStackTable.ContainsKey(index))
            {
                LabelStackTable.Remove(index);
                return true;
            }
            else
                return true;

        }

        internal bool addInputEntryToInputTable(int inI, string inL, int nPop, int xcInd) 
        {
            //Console.WriteLine("||||||||||||||||||" + inL + "!!!!!");
            InputTableEntry entry = null;
            try
            {
                entry = findRow(inI, inL);
            }
            catch (ArgumentOutOfRangeException)
            {

                try
                {                    
                    entry = new InputTableEntry(inI, inL, nPop, xcInd);
                    InputTable.Add(entry);
                    //Console.WriteLine(entry.InInterface + entry.InLabel + entry.NPop + entry.XCIndex);
                }
                catch (Exception)
                {
                    Console.WriteLine("Adding InputTableEntry gone wrong");
                    return false;
                }
                return true;
            }
            catch (ArgumentNullException)
            {
                try
                {                    
                    entry = new InputTableEntry(inI, inL, nPop, xcInd);
                    InputTable.Add(entry);
                }
                catch (Exception)
                {
                    Console.WriteLine("Adding InputTableEntry gone wrong");
                    return false;
                }
                return true;
            }
            entry.NPop = nPop;
            entry.XCIndex = xcInd;
            return true;
        }

        internal InputTableEntry findRow(int inI, string inL)
        {   
            var entry = from e in InputTable
                      where (e.InInterface == inI && e.InLabel == inL)
                      select e;            
            return entry.ElementAt(0);
        }

        internal string GetXCEntry(int index) 
        {
            XCTableEntry entry;
            if (!XCTable.TryGetValue(index, out entry))
                return null;
            StringBuilder sb = new StringBuilder();
            sb.Append(GETSETKeywords.XC.ToString()).Append(" ").Append(index).
                Append(" ").Append(entry.OutTableIndex).Append(" ").Append(entry.LabelStackTableIndex);
            return sb.ToString();
        }

        internal string GetInEntry(int inI, string inL) 
        {
            InputTableEntry entry = null;
            try
            {
                //Console.WriteLine(inI + "|" + inL + "|");
                entry = findRow(inI, inL);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(GETSETKeywords.INTAB.ToString()).Append(space).Append(entry.InInterface).Append(space);
            if(!inL.Equals(" "))
                sb.Append(entry.InLabel).Append(space);
            sb.Append(entry.NPop).Append(space).Append(entry.XCIndex);
            return sb.ToString();
        }

        internal string GetOutEntry(int index) 
        {
            OutputTableEntry entry;
            if (!OutputTable.TryGetValue(index, out entry))
                return null;
            StringBuilder sb = new StringBuilder();
            sb.Append(GETSETKeywords.OUTTAB.ToString()).Append(space).Append(index).
                Append(space).Append(entry.OutInterface).Append(space).Append(entry.PushTopLabel).Append(space);
            if(entry.PushTopLabel == true)
                sb.Append(entry.OutTopLabel);
            return sb.ToString();
        }

        internal string GetLSEntry(int index)
        {
            string entry;
            if (!LabelStackTable.TryGetValue(index, out entry))
                return null;
            StringBuilder sb = new StringBuilder();
            sb.Append(GETSETKeywords.LS.ToString()).Append(space).Append(index).
                Append(space).Append(entry);
            return sb.ToString();
        }

        internal string GetIPEntry(int index) 
        {
            int entry;
            if (!IPTable.TryGetValue(index, out entry))
                return null;
            StringBuilder sb = new StringBuilder();
            sb.Append(GETSETKeywords.IP.ToString()).Append(" ").Append(index).
                Append(" ").Append(entry);
            return sb.ToString();
        }



        internal bool deleteInEntry(int inP, string inL) 
        {
            InputTableEntry entry = null;
            try
            {
                entry = findRow(inP, inL);
            }
            catch (Exception e) 
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("deleteInEntry propably couldnt find sauch InputTableEntry");
                return true;
            }
            if (entry != null)
                if (InputTable.Remove(entry))
                    return true;
            return false;
        }


    }

}
