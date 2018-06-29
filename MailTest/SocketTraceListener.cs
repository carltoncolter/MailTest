using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using MahApps.Metro.Controls;

namespace MailTest
{
    public class SocketTraceListener : TraceListener
    {
        public static bool Logging;
        public static StringBuilder Log
        {
            get;
            set;
        }

        static SocketTraceListener()
        {
            Log = new StringBuilder();
        }

        public SocketTraceListener()
        {
            Filter = new EventTypeFilter(SourceLevels.All);
            Name = "SocketTraceListener";
        }

        public static void Start()
        {
            ClearLog();
            Logging = true;
        }

        public static string Stop()
        {
            Logging = false;
            var log = Log.ToString();
            ClearLog();
            return log;
        }

        public override void Write(string message)
        {
            Log.Append(message);
        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }

        public static void ClearLog()
        {
            Log.Clear();
            Log.Capacity = 256; // reset capacity to 256.
        }
    }
}