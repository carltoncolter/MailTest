using System;
using System.Text.RegularExpressions;

namespace MailTest
{
    public class POP3SimpleTest : SimpleBaseSocketTest, ITest
    {
        
        public string Username { get; set; }
        public string Password { get; set; }

        public POP3SimpleTest(string server, string port, string username, string password)
        {
            Server = server;
            Port = int.TryParse(port, out var iPort) ? iPort : 110;
            Username = username;
            Password = password;
        }

        private bool ReceiveOk() => Receive().Substring(0, 3).Equals("+OK");

        public override void Run()
        {
            Log("### STARTING POP TEST");
            try
            {
                Connect();

                if (!ReceiveOk())
                    throw new Exception("### Invalid initial POP3 response!");

                Send($"user {Username}");

                if (!ReceiveOk())
                    throw new Exception("### Username not accepted!");
                Send($"pass {Password}");

                if (!ReceiveOk())
                    throw new Exception("### Password not accepted!");

                long num = 0;
                Send("stat");
                var input = Receive();
                if (Regex.Match(input, "^.*\\+OK[ |\t]+([0-9]+)[ |\t]+.*$").Success)
                    num = long.Parse(
                        Regex.Replace(input.Replace("\r\n", ""), "^.*\\+OK[ |\t]+([0-9]+)[ |\t]+.*$", "$1"));

                Send("quit");
                Log($"### {Convert.ToString(num)} Messages in Inbox.");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }
    }
}