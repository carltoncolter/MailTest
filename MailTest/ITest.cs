using System;

namespace MailTest
{
    internal interface ITest
    {
        void Run(Action<String> log);
    }
}