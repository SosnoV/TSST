using System;


namespace LabelSwitchingRouter
{
    class Program
    {
        static void Main(string[] args)
        {
            LSR lsr = null;
            //LSR l = new LSR();
            //l.Run();
            try
            {
                lsr = new LSR(args[0]);
                //lsr = new LSR("3");
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
