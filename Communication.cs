using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections;
using CsharpMPLS;
using LabelSwitchingRouter;

namespace CsharpMPLS
{
    class Communication
    {
        private UdpClient client;
        private Thread receivingThread;
        private Encoding enc = Encoding.Unicode;
        private LSR lsr;

        public Communication(int localPortNumber, LSR _lsr)
        {   
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), localPortNumber);
            client = new UdpClient(localEndPoint);
            lsr = _lsr;
        }
        public void Start()
        {
            receivingThread = new Thread(Listening);
            receivingThread.IsBackground = true;
            receivingThread.Start();
        }
        public void Send(int remotePortNumber, byte[] toSend)
        {
            //byte[] toSend = enc.GetBytes(message);
            Console.WriteLine("To send: " + enc.GetString(toSend) + " to " + remotePortNumber);
            try
            {
                client.Send(toSend, toSend.Length, "127.0.0.1", remotePortNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
            }
        }
        private void Listening()
        {
            try
            {
                IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Console.WriteLine("Communication started");
                while (true)
                {
                    byte[] receivedBytes = client.Receive(ref remoteIPEndPoint);
                    lsr.ServeRequest(receivedBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
            }
        }

    }
}
