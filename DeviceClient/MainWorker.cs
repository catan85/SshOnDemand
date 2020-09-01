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
        private SshClient ssh = new SshClient();

        public async Task Run()
        {
            while (true)
            {

                DeviceConnectionStatus requestStatus = await http.CheckRemoteConnectionRequest();

                if (requestStatus != null && (requestStatus.State == ClientConnectionState.Ready) && ssh.ConnectionState == SshConnectionState.Closed)
                {
                    ssh.OpenSshConnection();
                    http.SetActiveDeviceConnection();
                }
                else if (requestStatus == null && ssh.ConnectionState == SshConnectionState.Open)
                {
                    ssh.CloseSshConnection();
                    http.ResetActiveDeviceConnection();
                }


                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
