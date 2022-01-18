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
        private SshHelper ssh = new SshHelper();
        private Logger logger = new Logger(Configuration.Instance.EnableDebug);
        private bool waiting = false;

        public async Task Run()
        {

            while (true)
            {

                // Insert connection request to the target device
                await http.InsertConnectionRequest();

                // Check device connection state to the ssh server
                DeviceConnectionStatus deviceConnectionDetails = await http.CheckDeviceConnectionState(ssh.PublicKey);
                if(deviceConnectionDetails != null)
                {
                    // Reset the connection if the client is connected with the wrong port (old connections)
                    if (ssh.ConnectionState == SshConnectionState.Open && ssh.CurrentForwardingPort != deviceConnectionDetails.SshForwarding)
                    {
                        ssh.CloseSshConnection("wrong port");
                    }


                    if (deviceConnectionDetails.State != ClientConnectionState.Connected)
                    {
                        logger.Output("Waiting for device connection..");
                        waiting = true;
                    }
                    else
                    {
                        if (ssh.ConnectionState != SshConnectionState.Open)
                        {
                            logger.Debug("Device has been connected, connecting developer to the ssh server..");

                            ssh.OpenSshConnection(deviceConnectionDetails);
                            ssh.EnableLocalForwarding(deviceConnectionDetails);
                            
                            logger.Output($"Developer SSH connection created: Device ssh server exposed on port {deviceConnectionDetails.SshForwarding})");
                            waiting = false;
                        }else if (waiting)
                        {
                            logger.Output($"Developer SSH connection re-established: Device ssh server exposed on port {deviceConnectionDetails.SshForwarding})");
                            waiting = false;
                        }
                    }
                }
                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
