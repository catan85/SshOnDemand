using SshOnDemandLibs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceClient
{
    class MainWorker
    {
        private HttpCaller http = new HttpCaller();
        private SshHelper ssh = new SshHelper();

        public async Task Run()
        {
            while (true)
            {

                DeviceConnectionStatus connectionDetails = await http.CheckRemoteConnectionRequest(ssh.PublicKey);

                if (connectionDetails != null)
                {
                    // Reset the connection if the client is connected with the wrong port (old connections)
                    if (ssh.ConnectionState == EnumSshConnectionState.Open && ssh.CurrentForwardingPort != connectionDetails.SshForwarding)
                    {
                        ssh.CloseSshConnection("wrong port and is open");
                    }

                    if ((connectionDetails.State == EnumClientConnectionState.Ready) && ssh.ConnectionState != EnumSshConnectionState.Open)
                    {
                        // Attesa di 2 secondi per fare in modo che OpenSSH acquisisca i certificati che abbiamo appena caricato
                        System.Threading.Thread.Sleep(5000);

                        ssh.OpenSshConnection(connectionDetails);
                        ssh.EnableRemoteForwarding(connectionDetails);
                    }
                    else if ((connectionDetails.State == EnumClientConnectionState.NotRequest) && (ssh.ConnectionState == EnumSshConnectionState.Open))
                    {
                        ssh.CloseSshConnection("Not request and is open");
                    }
                }
                else
                {
                    ssh.CloseSshConnection("no connection details");
                }

                // Connection status is beign constantly setted beacuse the server constantly check the last timestamp 
                // If the client goes offline, the server will mark the device as not connected automatically
                if (ssh.ConnectionState == EnumSshConnectionState.Open)
                {
                    await http.SetActiveDeviceConnection();
                }
                else if (connectionDetails != null && (connectionDetails.State != EnumClientConnectionState.NotRequest))
                {
                    await http.SetClosedSshConnectionState();
                }


                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
