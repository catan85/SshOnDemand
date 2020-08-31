using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace ApiServer
{
    public class KeysGeneration
    {
        private static string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
               Environment.OSVersion.Platform == PlatformID.MacOSX)
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
        public static void GenerateAndSaveKeys(string clientName)
        {

            var returnValue = ShellHelper.Bash($"ssh-keygen -t ecdsa -q -f \"{homePath}/.ssh/temp\" -N \"\"");

            var privateKey = System.IO.File.ReadAllText($"{homePath}/.ssh/temp");
            var publicKey = System.IO.File.ReadAllText($"{homePath}/.ssh/temp.pub");

            string copyCmd = "";
            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                copyCmd = $"cp \"{homePath}/.ssh/temp.pub\" \"{AppSettings.SshAuthorizedKeysFolder}\"/{clientName}.pub";
            }
            else
            {
                copyCmd = $"copy \"{homePath}\\.ssh\\temp.pub\" \"{AppSettings.SshAuthorizedKeysFolder}\"\\{clientName}.pub";
            }

            var r = ShellHelper.Bash(copyCmd);

        }

        public static void SaveKeys(string clientName, string key)
        {
            using (StreamWriter sw = new StreamWriter("temp.pub"))
            {
                sw.Write(key);
            }

            string copyCmd = "";
            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                copyCmd = $"cp \"temp.pub\" \"{AppSettings.SshAuthorizedKeysFolder}\"/{clientName}.pub";
            }
            else
            {
                copyCmd = $"copy \"temp.pub\" \"{AppSettings.SshAuthorizedKeysFolder}\"\\{clientName}.pub";
            }
            var r = ShellHelper.Bash(copyCmd);
        }
    }
}
