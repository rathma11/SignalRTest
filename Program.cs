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
        private static int msgRecievedCount = 0;

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
                SignalRTestClient.hubGroup = reader.ReadLine();
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
                stream = new FileStream("./log.txt", FileMode.Append, FileAccess.Write);
                writer = new StreamWriter(stream);

            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open log.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);
            Console.WriteLine();
            Write("****************** NEW TEST ********************");

            // main
            Task[] tasks = new Task[SignalRTestClient.clientNumber];
            for (int i = 0; i < SignalRTestClient.clientNumber; i++)
            {
                Task t = new Task(() =>
                {
                    BasicClient(i);
                });
                tasks[i] = t;
                t.RunSynchronously();

                // new Thread(delegate () {
                //     BasicClient(i);
                // }).Start();
            }
            Task.WaitAll(tasks);
            SenderClient(1);
            Write("MaxConnectionsCount: " + SignalRTestClient.maxConnectionsCount);
            Write("MsgRecievedCount: " + SignalRTestClient.msgRecievedCount);
            Console.SetOut(oldOut);
            writer.Close();
            stream.Close();
        }

        static private void BasicClient(int id)
        {
            bool disconnect = false;
            bool connected = false;

            try
            {
                var connection = new HubConnectionBuilder().
                WithUrl(SignalRTestClient.huburl).Build();
                connection.ServerTimeout = new TimeSpan(0,1,0);
                connection.On<string, string>("Send", (ticket, state) =>
                {
                    SignalRTestClient.msgRecievedCount++;
                    Write(" Message recieved: " + ticket + " " + state);
                    StopConn(connection).Wait();
                });

                try
                {
                    StartConn(connection).Wait();
                    Write(id + " Connected");
                    connected = true;
                }
                catch (Exception ex)
                {
                    Write(id + " Connection failed " + ex.Message);
                }
                if (connected)
                {
                    SendMsg(connection, "joinGroup", SignalRTestClient.hubGroup, id).Wait();

                    connection.Closed += async (error) =>
                    {
                        Write(" Connection closed:  " + error);
                
                        connected = false;
                        SignalRTestClient.connectionsCount--;
                        Write("ConnectionsCount: " + SignalRTestClient.connectionsCount);
                        if (!disconnect)
                        {
                            await Task.Delay(new Random().Next(0, 5) * 1000);
                            StartConn(connection).Wait();
                            SendMsg(connection, "joinGroup", SignalRTestClient.hubGroup, id).Wait();
                        }

                    };
                    
                }
            }
            catch (Exception ex)
            {
                Write(id + " Disconnected : " + ex.Message);
                disconnect = true;
            }
        }

        static private void SenderClient(int id)
        {

            try
            {
                var connection = new HubConnectionBuilder().
                WithUrl(SignalRTestClient.huburl).Build();
               
                try
                {
                    StartConn(connection).Wait();
                    Write(id + " SENDER Connected");
                }
                catch (Exception ex)
                {
                    Write(id + " SENDER Connection failed " + ex.Message);
                }

                SendMsg(connection, "joinGroup", SignalRTestClient.hubGroup, id).Wait();
                //SendMsg(connection, "SendToOthers", "newTicket", "newstate", id).Wait();
                SendMsg(connection, "SendToOthersInGroup", SignalRTestClient.hubGroup, "newTicket", "newstate", id).Wait();
                Task.Delay(30000);
                StopConn(connection).Wait();


                connection.Closed += async (error) =>
                {
                    SignalRTestClient.connectionsCount--;
                    Write("ConnectionsCount: " + SignalRTestClient.connectionsCount);
                    await Task.Delay(5000);
                };
            }
            catch (Exception ex)
            {
                Write(id + " SENDER Disconnected : " + ex.Message);
            }
        }

        static private async Task StartConn(HubConnection connection)
        {
            await connection.StartAsync();
            SignalRTestClient.connectionsCount++;
            Write("ConnectionsCount: " + SignalRTestClient.connectionsCount);
            if (SignalRTestClient.connectionsCount > SignalRTestClient.maxConnectionsCount)
            {
                SignalRTestClient.maxConnectionsCount = SignalRTestClient.connectionsCount;
            }
        }

        static private async Task SendMsg(HubConnection connection, string method, string msg, int id)
        {
            await connection.InvokeAsync(method, msg);
            Write(id + " method: " + method + " message: " + msg);
        }

        static private async Task SendMsg(HubConnection connection, string method, string ticketId, string state, int id)
        {
            await connection.InvokeAsync(method, ticketId, state);
            Write(id + " method: " + method +  " ticketId: " + ticketId + " state: " + state );
        }

        static private async Task SendMsg(HubConnection connection, string method, string group, string ticketId, string state, int id)
        {
            await connection.InvokeAsync(method, group, ticketId, state, 0);
            Write(id + " method: " + method +  " ticketId: " + ticketId + " state: " + state );
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
