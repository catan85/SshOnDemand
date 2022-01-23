using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ApiServer.Filters;
using ApiServer.Infrastructure;
using ApiServer.Infrastructure.Models;
using ApiServer.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SshOnDemandLibs;
using SshOnDemandLibs.Entities;

namespace ApiServer.Controllers
{
    public class DeviceController : ControllerBase
    {
        private readonly ClientConnectionsRepository clientConnections;

        private readonly Queries queries;
        public DeviceController(
                Queries queries
                , ClientConnectionsRepository clientConnections)
        {
            this.queries = queries;
            this.clientConnections = clientConnections;
        }

        [HmacAuthRequest]
        [HmacAuthResponse]
        [HttpPost(template: "DeviceCheckRemoteConnectionRequest")]
        public IActionResult DeviceCheckRemoteConnectionRequest([FromBody] string devicePublicKey)
        {
   
            string deviceIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Device identity is: " + deviceIdentity);

            var deviceConnectionRequest = clientConnections.GetByClientName(deviceIdentity);

            bool isDeviceConnectionRequested = deviceConnectionRequest != null;

            if (isDeviceConnectionRequested)
            {
                // Verifica dello stato della connessione, se è già attiva non devo fare nulla
                Core.Entities.DeviceConnectionStatus connectionStatus = this.queries.CheckDeviceConnection( deviceIdentity);

                // Altrimenti devo fare in modo che venga attivata la nuova connessione
                if (connectionStatus.State != EnumClientConnectionState.Connected)
                {

                    SshConnectionData connectionData = Utilities.CreateSshConnectionData();
                    // Saving device public key to allow its connection to the ssh server
                    SshKeysManagement.SaveKeys(connectionData, AppSettings.SshUser, "device_" + deviceIdentity, devicePublicKey, AppSettings.SshAuthorizedKeysPath, connectionStatus.SshForwarding);

                    // Generating Ssh connection details
                    Core.Entities.DeviceConnectionStatus connectionDetails = GenerateSshConnectionDetails();

                    // Inserting connection details to database

                    this.queries.SetDeviceConnectionDetails(deviceIdentity, connectionDetails);

                    return Ok(connectionDetails);
                }
                // in questo caso torna connected (quello che ha detto il db)
                return Ok(connectionStatus);
            }
            else if (!isDeviceConnectionRequested)
            {
                DeviceConnectionStatus connectionStatus = new DeviceConnectionStatus() { State = EnumClientConnectionState.NotRequest };
                return Ok(connectionStatus);
            }
            else
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Database error");
            }
        }

        [HmacAuthRequest]
        [HmacAuthResponse]
        [HttpPost(template: "DeviceSetConnectionState")]
        public IActionResult DeviceSetConnectionState([FromBody] EnumClientConnectionState deviceConnectionState)
        {
            string deviceIdentity = (string)HttpContext.Items["ClientName"];

            this.queries.SetDeviceConnectionState(deviceIdentity, deviceConnectionState);

            return Ok($"Status changed to: {deviceConnectionState}");
        }




        private Core.Entities.DeviceConnectionStatus GenerateSshConnectionDetails()
        {


            // Inserting device connection details
            Core.Entities.DeviceConnectionStatus deviceConnectionDetails = new Core.Entities.DeviceConnectionStatus();
            deviceConnectionDetails.SshHost = AppSettings.SshHost;
            deviceConnectionDetails.SshUser = AppSettings.SshUser;
            deviceConnectionDetails.SshPort = AppSettings.SshPort;
            deviceConnectionDetails.State = EnumClientConnectionState.Ready;

            using(sshondemandContext dbContext = new sshondemandContext())
            {
                List<int> usedPorts = this.queries.GetForwardingPorts();

                for (int i = AppSettings.SshFirstPort; i < AppSettings.SshFirstPort + 1000; i++ )
                {
                    if (!usedPorts.Contains(i))
                    {
                        deviceConnectionDetails.SshForwarding = i;
                        break;
                    }
                }

                return deviceConnectionDetails;
            }
        }
    }
}