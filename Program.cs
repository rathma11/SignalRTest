using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;


namespace Central_SignalR_Client
{

    public class SignalRTestClient
    {
        private static string huburl;
        private static int clientNumber = 1;
        private static string hubGroup = "TestGroup";
        private static int connectionsCount = 0;
        private static int maxConnectionsCount = 0;

        public static void Main(string[] args)
        {
            FileStream stream;
            // settings
            StreamReader reader;
            try
            {
                stream = new FileStream("./config.txt", FileMode.Open, FileAccess.Read);
                reader = new StreamReader(stream);
                SignalRTestClient.huburl = reader.ReadLine();
                SignalRTestClient.clientNumber = int.Parse(reader.ReadLine());
                reader.Close();
                stream = null;

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
                stream = new FileStream("./log.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(stream);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open log.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);

            // main

            for (int i = 0; i < SignalRTestClient.clientNumber; i++)
            {
                Task t = new Task(() =>
                {
                    BasicClient(i);
                });
                t.RunSynchronously();

                // new Thread(delegate () {
                //     BasicClient(i);
                // }).Start();
            }
            Write("MaxConnectionsCount: "  + SignalRTestClient.maxConnectionsCount);
            Console.SetOut(oldOut);
            writer.Close();
            stream.Close();
        }

        static private void BasicClient(int id)
        {
            bool connected = false;

            try
            {
                var connection = new HubConnectionBuilder().
                WithUrl(SignalRTestClient.huburl).Build();
             
                connection.On<string, string>("ReceiveMessage", (user, message) =>
                {
                
                });
                do
                {
                    try
                    {
                        StartConn(connection).Wait();
                        Write(id + " Connected");
                        connected = true;
                    }
                    catch (Exception ex)
                    {
                        Write(id + " Connection failed " + ex.Message);
                        connected = false;
                    }

                    SendMsg(connection, "joinGroup", "TestGroup").Wait();
                    Write(id + " Groupjoined");
                    SendMsg(connection, "LeaveGroup", "TestGroup").Wait();
                    Write(id + " LeaveGroup");
                    StopConn(connection).Wait();
                    Write(id + " Disconnected");
                    connection.Closed += async (error) =>
                    {
                        connected = false;
                        SignalRTestClient.connectionsCount --;
                        Write("ConnectionsCount: "  + SignalRTestClient.connectionsCount);
                        await Task.Delay(new Random().Next(0, 5) * 1000);
                        StartConn(connection).Wait();
                    };
                } while (connected);
            }
            catch (Exception ex)
            {
                Write(id + " Disconnected : " + ex.Message);
                connected = false;
            }
        } 

        static private async Task StartConn(HubConnection connection)
        {
            await connection.StartAsync();
            SignalRTestClient.connectionsCount ++;
            Write("ConnectionsCount: "  + SignalRTestClient.connectionsCount);
            if (SignalRTestClient.connectionsCount > SignalRTestClient.maxConnectionsCount)
            {
                SignalRTestClient.maxConnectionsCount = SignalRTestClient.connectionsCount;
            }
        }

        static private async Task SendMsg(HubConnection connection, string method, string msg)
        {
            await connection.InvokeAsync(method, msg);
        }

        static private async Task StopConn(HubConnection connection)
        {
            await connection.StopAsync();
        }

        static private void Write(string text)
        {
            Console.WriteLine("[" + DateTime.UtcNow + "]:" + text);
        }
    }


}
