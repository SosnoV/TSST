using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace LabelSwitchingRouter
{
    class Program
    {
        static void Main(string[] args)
        {

            LSR lsr = null;
            try
            {
                lsr = new LSR(args[0]);
                lsr.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            
            Console.ReadLine();
        
        }
    }
}
