using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MailTest
{
    public class SMTPClientTest : ITest
    {
        private Action<string> Log { get; set; }
        SmtpClient client { get; set; }
        MailMessage msg { get; set; }
        public SMTPClientTest(Action<string> log, string server, string port, string from, string to, string subject, string body, bool enableSSL, string username, string password, bool authRequired)
        {
            const string defaultEmail = "someone@somewhere.com";
            Log = log;

            msg = new MailMessage
            {
                From = new MailAddress(String.IsNullOrWhiteSpace(@from) ? defaultEmail : @from),
                Subject = subject ?? "Test",
                Body = body ?? "Test"
            };

            var recipients = new List<string>(to.Split(';'));
            recipients.Where(a=>!String.IsNullOrWhiteSpace(a)).ToList().ForEach(msg.To.Add);

            if (msg.To.Count == 0)
            {
                msg.To.Add(defaultEmail);
            }

            if (String.IsNullOrWhiteSpace(server)) server = "mail.somewhere.com";

            client = new SmtpClient
            {
                Host = server,
                Port = int.TryParse(port, out var iPort) ? iPort : 25,
                EnableSsl = enableSSL,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!authRequired) return;

            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(username, password);

        }
        
        public Action<string> LogLine { get; set; }

        public void Run(Action<string> log)
        {
            LogLine = log;
            SocketTraceListener.Start();
            var ui = Dispatcher.CurrentDispatcher;
            Task.Run(() =>
            {
                try
                {
                    client.Send(msg);
                }
                catch (Exception ex)
                {
                    ui.Invoke(delegate { LogLine($"### {ex.Message}"); });
                }

                ui.Invoke(delegate
                {
                    LogLine("### DIAGNOSTICS TRACE START");
                    Log(SocketTraceListener.Stop());
                    LogLine("### DIAGNOSTICS TRACE END");
                });

            });
        }
    }
}
