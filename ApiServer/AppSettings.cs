using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer
{
    public class AppSettings
    {
        public static string OS { get; set; }
        public static string ApiKey { get; set; }

        public static string SshAuthorizedKeysPath { get; set; }

        public static string DbConnectionString { get; set; }

        public static string SshHost { get; set; }

        public static string SshUser { get; set; }

        public static string SshPass { get; set; }

        public static int SshPort { get; set; }

        public static int SshFirstPort { get; set; }
    }
}
