using System;
using System.Collections.Generic;
using System.Text;

namespace SshOnDemandLibs
{
    public class Logger
    {
        public bool EnableDebug { get; set; }

        public void Debug(string message)
        {
            if (this.EnableDebug)
            {
                Console.WriteLine(message);
            }
        }

        public void Output(string message)
        {
            Console.WriteLine(message);
        }

        public Logger(bool enableDebug)
        {
            EnableDebug = enableDebug;
        }

    }
}
