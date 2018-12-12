using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.SignalR.Client;


namespace Central_SignalR_Client
{
    
    public class SignalRTestClient
    {
        private static string huburl;
        public static void Main(string[] args)
        {
            FileStream ostrm;
            // settings
            StreamReader reader;
            try
            {
                ostrm = new FileStream("./config.txt", FileMode.Open, FileAccess.Read);
                reader = new StreamReader(ostrm);
                SignalRTestClient.huburl = reader.ReadLine();
                reader.Close();
                ostrm = null;

            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open config ");
                Console.WriteLine(e.Message);
                return;
            }

            // logging
            
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream("./log.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open log.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);

            // main
            try
            {
                var connection = new HubConnectionBuilder().
                WithUrl(SignalRTestClient.huburl).Build();
                try
                {
                    StartConn(connection).Wait();
                    Write("Connected");
                    // 
                }
                catch (Exception ex)
                { 
                    Write("Connection failed"+ ex.Message);
                }
                
                SendMsg(connection).Wait();
                string line = string.Empty;
                do
                {
                        connection.Closed += (error) =>
                         {
                             throw new Exception("Connection Timeout");
                             return Task.CompletedTask;
                         };
                } while (line == string.Empty);
                StopConn(connection).Wait();
            }
            catch (Exception ex)
            {
                Write("Disconnected : "+ ex.Message);

            }

            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
        }

        static private async Task StartConn(HubConnection connection)
        {
            await connection.StartAsync();
        }

        static private async Task SendMsg(HubConnection connection)
        {
            await connection.InvokeAsync("joinGroup",
                "TestClient");
        }

        static private async Task StopConn(HubConnection connection)
        {
            await connection.StopAsync();
        }

        static private void Write(string text)
        {
            Console.WriteLine("["+ DateTime.UtcNow + "]:" + text);
        }
    }


}
