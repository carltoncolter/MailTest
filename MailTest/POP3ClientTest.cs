using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Pop3;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MailTest
{
    public class POP3ClientTest : ITest
    {
        private Action<string> Log { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public bool SSL { get; set; }
        public int Port { get; set; }
        public POP3ClientTest(Action<string> log, string server, string port, string username, string password, bool useSSL)
        {
            Log = log;

            Server = server;
            Port = int.TryParse(port, out var iPort) ? iPort : 110;
            Username = username;
            Password = password;
            SSL = useSSL;
        }
        
        public Action<string> LogLine { get; set; }

        public void Run(Action<string> log)
        {
            LogLine = log;
            SocketTraceListener.Start();
            var ui = Dispatcher.CurrentDispatcher;
            uint numberOfEmails = 0;
            Task.Run(() =>
            {
                try
                {
                    var client = new Pop3Client();
                    client.Connect(Server, Port, SSL);
                    client.SendAuthUserPass(Username, Password);
                   
                    numberOfEmails = client.GetEmailCount();
                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    ui.Invoke(delegate { LogLine($"### {ex.Message}"); });
                }

                ui.Invoke(delegate
                {
                    Log($"### {numberOfEmails} Messages.");
                    LogLine("### DIAGNOSTICS TRACE START");
                    Log(SocketTraceListener.Stop());
                    LogLine("### DIAGNOSTICS TRACE END");
                });

            });
        }
    }
}
