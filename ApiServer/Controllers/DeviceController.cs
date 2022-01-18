using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ApiServer.Filters;
using ApiServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SshOnDemandLibs;
using SshOnDemandLibs.Entities;

namespace ApiServer.Controllers
{
    public class DeviceController : ControllerBase
    {
        private readonly sshondemandContext dbContext;
        public DeviceController(sshondemandContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HmacAuthRequest]
        [HmacAuthResponse]
        [HttpPost(template: "DeviceCheckRemoteConnectionRequest")]
        public IActionResult DeviceCheckRemoteConnectionRequest([FromBody] string devicePublicKey)
        {
            bool fault = false;
            string deviceIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Device identity is: " + deviceIdentity);

            // Check device connection authorization
            // superflua --> bool isDeviceAuthorized = PostgreSQLClass.IsDeviceConnectionAuthorized(deviceIdentity, out fault);  --> già fatto nel filter
            bool isDeviceConnectionRequested = Queries.IsDeviceConnectionRequested(dbContext, deviceIdentity);

            if (isDeviceConnectionRequested && !fault)
            {
                // Verifica dello stato della connessione, se è già attiva non devo fare nulla
                DeviceConnectionStatus connectionStatus = Queries.CheckDeviceConnection(dbContext, deviceIdentity);

                // Altrimenti devo fare in modo che venga attivata la nuova connessione
                if (connectionStatus.State != ClientConnectionState.Connected)
                {

                    SshConnectionData connectionData = Utilities.CreateSshConnectionData();
                    // Saving device public key to allow its connection to the ssh server
                    SshKeysManagement.SaveKeys(connectionData, AppSettings.SshUser, "device_" + deviceIdentity, devicePublicKey, AppSettings.SshAuthorizedKeysPath, connectionStatus.SshForwarding);

                    // Generating Ssh connection details
                    DeviceConnectionStatus connectionDetails = GenerateSshConnectionDetails();

                    // Inserting connection details to database

                    Queries.SetDeviceConnectionDetails(dbContext, deviceIdentity, connectionDetails);

                    return Ok(connectionDetails);
                }
                // in questo caso torna connected (quello che ha detto il db)
                return Ok(connectionStatus);
            }
            else if (!isDeviceConnectionRequested)
            {
                DeviceConnectionStatus connectionStatus = new DeviceConnectionStatus() { State = ClientConnectionState.NotRequest };
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
        public IActionResult DeviceSetConnectionState([FromBody] ClientConnectionState deviceConnectionState)
        {
            string deviceIdentity = (string)HttpContext.Items["ClientName"];

            Queries.SetDeviceConnectionState(dbContext, deviceIdentity, deviceConnectionState);

            return Ok($"Status changed to: {deviceConnectionState}");
        }




        private DeviceConnectionStatus GenerateSshConnectionDetails()
        {


            // Inserting device connection details
            DeviceConnectionStatus deviceConnectionDetails = new DeviceConnectionStatus();
            deviceConnectionDetails.SshHost = AppSettings.SshHost;
            deviceConnectionDetails.SshUser = AppSettings.SshUser;
            deviceConnectionDetails.SshPort = AppSettings.SshPort;
            deviceConnectionDetails.State = ClientConnectionState.Ready;

            using(sshondemandContext dbContext = new sshondemandContext())
            {
                List<int> usedPorts = Queries.GetForwardingPorts(dbContext);

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