using System;
using System.Collections.Generic;
using System.Linq;

namespace LabelSwitchingRouter
{
    class CommutationField
    {
        //private string space = " ";
        private LSR lsr = null;
        private List<LabelSwitchingTableEntry> LabelSwitchingTable = null;
   
        internal class LabelSwitchingTableEntry 
        {
            public int InInterface;
            public string InLabel;
            public int OutInterface;
            public string OutLabel;

            public LabelSwitchingTableEntry(int inI, string InL, int nPop, string xcindex) 
            {
                InInterface = inI;
                InLabel = InL;
                OutInterface = nPop;
                OutLabel = xcindex;
            }
            public override string ToString()
            {

                return base.ToString();
            }

            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType()) return false;
                var objAsEntry = obj as LabelSwitchingTableEntry;
                if (objAsEntry == null) return false;
                if ((this.InInterface == objAsEntry.InInterface) && (this.InLabel.Equals(objAsEntry.InLabel)))
                    return true;
                else
                    return false;
            }
        }
        
        
        internal CommutationField(LSR router) 
        {
            lsr = router;
            LabelSwitchingTable = new List<LabelSwitchingTableEntry>();
        }

        internal byte[] DealWithPacket(CsharpMPLS.Packet _packet) 
        {
            _packet.SetNodeID(lsr.nodeID); //ustawienie id wezla w pakiecie
            if (_packet.GetAndDecreaseTTL() < 0) //TTL sprawdzenie
                throw new Exception();
            LabelSwitchingTableEntry entry = null;
            try
            {
                 entry = this.findRow(_packet.ReturnPortNumber(), _packet.GetLabel());
                 _packet.SwapLabel(entry.OutLabel);
                 _packet.SetPortNumber(entry.OutInterface);
                 Console.WriteLine("DealWithPacket:" + _packet.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw new Exception();
            }
           
            return _packet.CreatePacketToSend();
        }

        

        internal bool addInputEntryToInputTable(int inI, string inL, int outI, string outL) 
        {
            //Console.WriteLine("||||||||||||||||||" + inL + "!!!!!");
            LabelSwitchingTableEntry entry = null;
            try
            {
                entry = findRow(inI, inL);
            }
            catch (ArgumentOutOfRangeException)
            {

                try
                {
                    entry = new LabelSwitchingTableEntry(inI, inL, outI, outL);
                    LabelSwitchingTable.Add(entry);
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
                    entry = new LabelSwitchingTableEntry(inI, inL, outI, outL);
                    LabelSwitchingTable.Add(entry);
                }
                catch (Exception)
                {
                    Console.WriteLine("Adding InputTableEntry gone wrong");
                    return false;
                }
                return true;
            }
            
            //entry.NPop = outI;
            //entry.XCIndex = xcInd;
            return true;
        }

        internal LabelSwitchingTableEntry findRow(int inI, string inL)
        {   
            var entry = from e in LabelSwitchingTable
                      where (e.InInterface == inI && e.InLabel == inL)
                      select e;            
            return entry.ElementAt(0);
        }

        
        //internal string GetInEntry(int inI, string inL) 
        //{
        //    LabelSwitchingTableEntry entry = null;
        //    try
        //    {
        //        //Console.WriteLine(inI + "|" + inL + "|");
        //        entry = findRow(inI, inL);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return null;
        //    }
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(GETSETKeywords.INTAB.ToString()).Append(space).Append(entry.InInterface).Append(space);
        //    if(!inL.Equals(" "))
        //        sb.Append(entry.InLabel).Append(space);
        //    sb.Append(entry.OutInterface).Append(space).Append(entry.OutLabel);
        //    return sb.ToString();
        //}

        internal bool deleteInEntry(int inP, string inL) 
        {
            LabelSwitchingTableEntry entry = null;
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
                if (LabelSwitchingTable.Remove(entry))
                    return true;
            return false;
        }


    }

}
