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

                if (connectionDetails != null && (connectionDetails.State == ClientConnectionState.Ready) && ssh.ConnectionState == SshConnectionState.Closed)
                {
                    ssh.OpenSshConnection(connectionDetails);
                    ssh.EnableRemoteForwarding(connectionDetails);
                }
                else if (connectionDetails == null && ssh.ConnectionState == SshConnectionState.Open)
                {
                    ssh.CloseSshConnection();
                }

                // Connection status is beign constantly setted beacuse the server constantly check the last timestamp 
                // If the client goes offline, the server will mark the device as not connected automatically
                if (ssh.ConnectionState == SshConnectionState.Open)
                {
                    await http.SetActiveDeviceConnection();
                }
                else
                {
                    await http.ResetActiveDeviceConnection();
                }


                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
