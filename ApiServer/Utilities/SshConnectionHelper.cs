using SshOnDemandLibs;
using SshOnDemandLibs.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer
{
    public class Utilities
    {
        public static SshConnectionData CreateSshConnectionData()
        {
            SshConnectionData connectionData = new SshConnectionData();
            connectionData.AuthenticationMode = SshAuthMode.WithPassword;
            connectionData.Host = AppSettings.SshHost;
            connectionData.Port = AppSettings.SshPort;
            connectionData.Username = AppSettings.SshUser;
            connectionData.Password = AppSettings.SshPass;
            return connectionData;
        }
    }
}
