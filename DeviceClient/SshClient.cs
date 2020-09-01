using SshOnDemandLibs;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceClient
{
    class SshClient
    {

        public SshConnectionState ConnectionState = SshConnectionState.Closed;

        public void OpenSshConnection()
        {
            Console.WriteLine("Connection Opened");
            ConnectionState = SshConnectionState.Open;
        }

        public void CloseSshConnection()
        {
            Console.WriteLine("Connection Closed");
            ConnectionState = SshConnectionState.Closed;
        }
    }
}
