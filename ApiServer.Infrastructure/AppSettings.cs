using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer
{
    public class AppSettings
    {
        public string OS { get; set; }
        public string ApiKey { get; set; }

        public string SshAuthorizedKeysPath { get; set; }

        public string DbConnectionString { get; set; }

        public string SshHost { get; set; }

        public string SshUser { get; set; }

        public string SshPass { get; set; }

        public int SshPort { get; set; }

        public int SshFirstPort { get; set; }
    }
}
