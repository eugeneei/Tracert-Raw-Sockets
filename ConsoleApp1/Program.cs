using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Text;


public class Tracert
{
    public static void Main(string[] argv)
    {
        int trys = 3;
        Console.Write("tracert ");
        string address = Console.ReadLine();

        byte[] data = new byte[1024];
        int recv, timestart, timestop;
        Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);

        bool flagHostEntry = false;
        while (!flagHostEntry)
        {
            try
            {
                IPHostEntry temp = Dns.GetHostEntry(address);
                flagHostEntry = true;
            }
            catch (Exception)
            {
                Console.Write("\nВведен неверный адрес, попробуйте еще раз: ");
                address = Console.ReadLine();
            };
        };

        IPHostEntry iphe = Dns.GetHostEntry(address);
        IPEndPoint iep = new IPEndPoint(iphe.AddressList[0], 0);
        EndPoint ep = (EndPoint)iep;
        ICMP packet = new ICMP(); 

        packet.Type = 0x08;
        packet.Code = 0x00;
        packet.Checksum = 0;
        Buffer.BlockCopy(BitConverter.GetBytes(1), 0, packet.Message, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(1), 0, packet.Message, 2, 2);
        data = Encoding.ASCII.GetBytes("test packet");
        Buffer.BlockCopy(data, 0, packet.Message, 4, data.Length);
        packet.MessageSize = data.Length + 4;
        int packetsize = packet.MessageSize + 4;

        UInt16 chcksum = packet.getChecksum();
        packet.Checksum = chcksum;

        host.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);

        try
        {
            if (Dns.GetHostEntry(address).HostName == address)
                Console.WriteLine("\nТрассировка маршрута к {0} [{1}]", address, Dns.GetHostEntry(address).AddressList[0].ToString());
            else
                Console.WriteLine("\nТрассировка маршрута к {0} [{1}]", Dns.GetHostEntry(address).HostName, address);
        }
        catch (Exception)
        {
            Console.WriteLine("\nТрассировка маршрута к {0}", address);
        };
        Console.WriteLine("с максимальным числом прыжков 30:\n");

        int falsecount = 0;
        bool flagExit = true;
        for (int i = 1; i <= 30 && flagExit; i++)
        {
            Console.Write("{0,3} ", i);
            for (int j = 0; j < trys; j++)
            {
                host.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, i);
                timestart = Environment.TickCount;
                host.SendTo(packet.getBytes(), packetsize, SocketFlags.None, iep);
                try
                {
                    data = new byte[1024];
                    recv = host.ReceiveFrom(data, ref ep);
                    timestop = Environment.TickCount;

                    ICMP response = new ICMP(data, recv);
                    if (response.Type == 11)
                        if (timestop == timestart)
                            Console.Write("    1 мс");
                        else
                            Console.Write("{0,5} мс", timestop - timestart);
                    if (response.Type == 0)
                    {
                        if (timestop == timestart)
                            Console.Write("    1 мс");
                        else
                            Console.Write("{0,5} мс", timestop - timestart);
                        flagExit = false;    
                    }
                    falsecount = 0;

                }
                catch (SocketException)
                {
                    Console.Write("    *   ");
                    falsecount++;
                }
            }
            if (falsecount >= trys)
            {
                Console.WriteLine("   Превышен интервал ожидания для запроса.");
                if (falsecount == trys * 3)
                {
                    Console.WriteLine("\nНевозможно связаться с удаленным хостом.");
                    flagExit = false;
                }
            }
            else
            {
                try
                {
                    string strEndPoint = Regex.Replace(ep.ToString(), ":.*", "");
                        Console.Write("   {0} [{1}]", Dns.GetHostEntry(strEndPoint).HostName, strEndPoint);
                }
                catch (Exception)
                {
                        Console.Write("   {0}", Regex.Replace(ep.ToString(), ":.*", ""));
                };
                Console.WriteLine();
            }
        }

        host.Close();
        Console.WriteLine("\nТрассировка завершина.\n");
        Console.Read();
    }
}