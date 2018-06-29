using System;
using System.Collections.Generic;
using System.Linq;

namespace MailTest
{
    public class SMTPSimpleTest : SimpleBaseSocketTest, ITest
    {
        public string Sender { get; set; }
        public List<string> Recipients { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }

        public SMTPSimpleTest(string server, string port, string from, string to, string subject, string body)
        {
            Server = server;
            Port = int.TryParse(port, out var iPort) ? iPort : 25;
            Sender = from;
            Recipients = new List<string>(to.Split(';'));
            Subject = subject;
            Body = body;
        }

        public override void Run()
        {
            Log("### STARTING SMTP TEST");
            try
            {
                Connect();
                if (Receive().Substring(0, 3).IndexOf("220", StringComparison.Ordinal) == -1)
                {
                    Disconnect();
                    throw new Exception("### Invalid initial SMTP response");
                }

                string senderDomain;
                try
                {
                    senderDomain = Sender.Split('@')[1];
                    Send($"helo {senderDomain}");
                }
                catch
                {
                    Quit();
                    throw new Exception("### Invalid from address.");
                }

                if (Receive().Substring(0, 3) != "250")
                {
                    Log("### Unexpected Response to HELO - Attempting EHLO");
                    Send($"ehlo {senderDomain}");
                    if (Receive().Substring(0, 3) != "250")
                    {
                        Quit();
                        throw new Exception("### EHLO not accepted!");
                    }
                }

                Send($"mail from: <{Sender}>");
                if (Receive().Substring(0, 3) != "250")
                {
                    Quit();
                    throw new Exception("### From not accepted!");
                }

                foreach (var recipient in Recipients)
                {
                    try
                    {
                        Send($"RCPT TO: <{recipient}>");
                        if (Receive().Substring(0, 3) == "250") continue;
                        Quit();
                        throw new Exception("### Recipient not accepted!");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"### {ex.Message}");
                    }
                }

                Send("DATA");
                if (Receive().Substring(0, 3) != "354")
                    throw new Exception("### DATA not accepted!");

                Send($"SUBJECT: {Subject}");

                foreach (var recipient in Recipients)
                    Send($"TO: {recipient}");

                Send($"FROM: {Sender}");

                if (Body.Length > 0)
                {
                    Send("MIME-Version: 1.0");
                    Send("Content-Type: text/html;");
                    Send(" charset=\"iso-8859-1\"");
                    Body.Split('\r').Select(a => a.Replace("\n", "")).ToList().ForEach(Send);
                }
                else
                    Send("");

                Send(".");

                if (Receive().Substring(0, 3) != "250")
                {
                    Quit();
                    throw new Exception("### Message not accepted!");
                }

                Send("QUIT");
                if (Receive().IndexOf("221", StringComparison.Ordinal) == -1)
                    throw new Exception("### Error on quit!");
                Disconnect();
                throw new Exception("### MESSAGE SENT! - disconnected.");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void Quit()
        {
            Send("QUIT");
            Disconnect();
        }
    }
}