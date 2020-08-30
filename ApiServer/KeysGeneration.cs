using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer
{
    public class KeysGeneration
    {
        public static void GenerateKeys()
        {
            string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX)
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            var returnValue = ShellHelper.Bash($"ssh-keygen -t ecdsa -q -f \"{homePath}/.ssh/temp\" -N \"\"");

            var privateKey = System.IO.File.ReadAllText($"{homePath}/.ssh/temp");
            var publicKey = System.IO.File.ReadAllText($"{homePath}/.ssh/temp.pub");

            Console.WriteLine("private: \n" + privateKey);
            Console.WriteLine("public: \n" + publicKey);

        }
    }
}
