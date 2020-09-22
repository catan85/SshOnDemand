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
                    if (ssh.ConnectionState == SshConnectionState.Open && ssh.CurrentForwardingPort != connectionDetails.SshForwarding)
                    {
                        ssh.CloseSshConnection();
                    }

                    if ((connectionDetails.State == ClientConnectionState.Ready) && ssh.ConnectionState != SshConnectionState.Open)
                    {
                        // Attesa di 2 secondi per fare in modo che OpenSSH acquisisca i certificati che abbiamo appena caricato
                        System.Threading.Thread.Sleep(5000);

                        ssh.OpenSshConnection(connectionDetails);
                        ssh.EnableRemoteForwarding(connectionDetails);
                    }
                    else if ((connectionDetails.State == ClientConnectionState.NotRequest) && (ssh.ConnectionState == SshConnectionState.Open))
                    {
                        ssh.CloseSshConnection();
                    }
                }
                else
                {
                    ssh.CloseSshConnection();
                }

                // Connection status is beign constantly setted beacuse the server constantly check the last timestamp 
                // If the client goes offline, the server will mark the device as not connected automatically
                if (ssh.ConnectionState == SshConnectionState.Open)
                {
                    await http.SetActiveDeviceConnection();
                }
                else if (connectionDetails != null && (connectionDetails.State != ClientConnectionState.NotRequest))
                {
                    await http.SetClosedSshConnectionState();
                }


                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
