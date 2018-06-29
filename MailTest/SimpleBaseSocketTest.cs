using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MailTest
{
    public abstract class SimpleBaseSocketTest : ITest
    {
        protected SimpleBaseSocketTest()
        {
            RemoteLog = s => { };
            Server = null;
            Port = 0;
        }

        public Socket tSocket { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        private Dispatcher UI { get; set; }
        public Action<string> RemoteLog { get; set; }

        public void Run(Action<string> log)
        {
            UI = Dispatcher.CurrentDispatcher;
            RemoteLog = log;
            Task.Run(() => Run());
        }

        public void Log(string msg)
        {
            UI.Invoke(delegate { RemoteLog(msg); });
        }

        public void Send(string data)
        {
            CheckForConnection();

            try
            {
                tSocket.Send(Encoding.ASCII.GetBytes($"{data}"));
                Log($"> {data}");
            }
            catch (Exception ex)
            {
                throw new Exception($"### {ex.Message}");
            }
        }

        public void CheckForConnection()
        {
            if (tSocket == null)
                throw new Exception("The connection was closed.");
        }

        public string Receive()
        {
            CheckForConnection();

            var numArray = new byte[256];
            string received;
            try
            {
                var count = tSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                received = Encoding.ASCII.GetString(numArray, 0, count);
                Log($"< {received}");
            }
            catch (Exception ex)
            {
                throw new Exception($"### {ex.Message}");
            }

            return received;
        }

        public void Disconnect()
        {
            if (tSocket.Connected) tSocket.Disconnect(true);
            tSocket = null;
        }

        public void Connect()
        {
            Log($"### Attempting to connect to: {Server}:{Port}");

            Socket lastConnectedSocket = null;
            try
            {
                // Test connection to each endpoint
                var addresses = Dns.GetHostEntry(Server).AddressList;
                int failures = 0;

                var endpoints = addresses.Select(a => new IPEndPoint(a, Port));
                foreach (var endpoint in endpoints)
                    using (var currentSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                    {
                        currentSocket.Connect(endpoint);
                        if (!currentSocket.Connected)
                        {
                            Log($"### CONNECTION FAILED: {Server}:{Port} ###");
                            failures++;
                        }
                        else
                        {
                            lastConnectedSocket = currentSocket;
                            Log($"### CONNECTED TO: {Server}:{Port}");
                        }
                    }

                Log($"### FAILED TO CONNECT TO {failures} ENDPOINTS");
            }
            catch (Exception ex)
            {
                throw new Exception($"### {ex.Message}");
            }

            // If none were successful, then throw an error.
            if (lastConnectedSocket == null)
                throw new Exception($"Error : connecting to {Server}");

            tSocket = lastConnectedSocket;
        }

        public abstract void Run();
    }
}