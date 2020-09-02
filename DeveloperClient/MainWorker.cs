using SshOnDemandLibs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeveloperClient
{
    class MainWorker
    {
        private HttpCaller http = new HttpCaller();
        private SshClient ssh = new SshClient();

        public async Task Run()
        {

            while (true)
            {
                // Insert connection request to the target device
                await http.InsertConnectionRequest(ssh.PublicKey);

                // Check device connection state to the ssh server
                DeviceConnectionStatus deviceConnectionDetails = await http.CheckDeviceConnectionState();
                if(deviceConnectionDetails != null)
                {
                    if (deviceConnectionDetails.State != ClientConnectionState.Connected)
                    {
                        Console.WriteLine("Waiting for device connection..");
                    }
                    else
                    {
                        Console.WriteLine("Device has been connected, connecting developer to the ssh server..");
                        ssh.OpenSshConnectionRemotelyForwarded(deviceConnectionDetails);
                    }
                }
                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
