using SshOnDemandLibs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SshOnDemandLibs
{
    public class SshClient
    {

        public SshClient()
        {
            // Inserire quì la generazione della coppia di chiavi del client
            this.PublicKey = "Client public Key";
           
        }

        public SshConnectionState ConnectionState = SshConnectionState.Closed;

        private string _publicKey;

        public string PublicKey { get => _publicKey; set => _publicKey = value; }

        public void OpenSshConnectionLocallyForwarded(DeviceConnectionStatus connectionDetails)
        {
            Console.WriteLine($@"Connection Opened, ip: {connectionDetails.SshHost}:{connectionDetails.SshPort} L22 --> R{connectionDetails.SshForwarding}");
            ConnectionState = SshConnectionState.Open;
        }

        public void OpenSshConnectionRemotelyForwarded(DeviceConnectionStatus connectionDetails)
        {
            Console.WriteLine($@"Connection Opened, ip: {connectionDetails.SshHost}:{connectionDetails.SshPort} R{connectionDetails.SshForwarding} --> L{connectionDetails.SshForwarding}");
            ConnectionState = SshConnectionState.Open;
        }

        public void CloseSshConnection()
        {
            Console.WriteLine("Connection Closed");
            ConnectionState = SshConnectionState.Closed;
        }
    }
}
