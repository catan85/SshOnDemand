using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ApiServer.Application.Mapper;
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
        private readonly AppSettings settings;
        private readonly Ssh ssh;

        public DeviceController(
                ClientConnectionsRepository clientConnections,
                AppSettings settings,
                Ssh ssh)
        {
            this.clientConnections = clientConnections;
            this.settings = settings;
            this.ssh = ssh;
        }

        [HmacAuthRequest]
        [HmacAuthResponse]
        [HttpPost(template: "DeviceCheckRemoteConnectionRequest")]
        public IActionResult DeviceCheckRemoteConnectionRequest([FromBody] string devicePublicKey)
        {

            #warning to move in infrastructure module
            string deviceIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Device identity is: " + deviceIdentity);

            var deviceConnectionRequest = clientConnections.GetByClientName(deviceIdentity);

            bool isDeviceConnectionRequested = deviceConnectionRequest != null;

            if (isDeviceConnectionRequested)
            {
                // Verifica dello stato della connessione, se è già attiva non devo fare nulla
                var deviceConnection = this.clientConnections.CheckDeviceConnection(deviceIdentity);

                Core.Entities.DeviceConnectionStatus connectionStatus = ClientConnectionMapper.Mapper.Map<Core.Entities.DeviceConnectionStatus>(deviceConnection);

                // Altrimenti devo fare in modo che venga attivata la nuova connessione
                if (connectionStatus.State != EnumClientConnectionState.Connected)
                {
                    this.ssh.SaveClientKeys(connectionStatus.SshForwarding, "device_" + deviceIdentity, devicePublicKey);

                    // Generating Ssh connection details
                    Core.Entities.DeviceConnectionStatus connectionDetails = GenerateSshConnectionDetails();

                    // Inserting connection details to database

                    this.clientConnections.SetDeviceConnectionDetails(deviceIdentity, connectionDetails);

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

            this.clientConnections.SetDeviceConnectionState(deviceIdentity, deviceConnectionState);

            return Ok($"Status changed to: {deviceConnectionState}");
        }


        private Core.Entities.DeviceConnectionStatus GenerateSshConnectionDetails()
        {

            // Inserting device connection details
            Core.Entities.DeviceConnectionStatus deviceConnectionDetails = new Core.Entities.DeviceConnectionStatus();
            deviceConnectionDetails.SshHost = settings.SshHost;
            deviceConnectionDetails.SshUser = settings.SshUser;
            deviceConnectionDetails.SshPort = settings.SshPort;
            deviceConnectionDetails.State = EnumClientConnectionState.Ready;

            using(sshondemandContext dbContext = new sshondemandContext())
            {
                List<int> usedPorts = clientConnections.GetForwardingPorts();

                for (int i = settings.SshFirstPort; i < settings.SshFirstPort + 1000; i++ )
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