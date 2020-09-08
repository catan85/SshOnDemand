using System;
using System.Collections.Generic;
using System.Text;

namespace SshOnDemandLibs.Entities
{

    public class SshConnectionData
    {
        public SshAuthMode AuthenticationMode { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string PrivateKey { get; set; }

    }
}
