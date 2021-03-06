﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ApiServer.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SshOnDemandLibs;
using SshOnDemandLibs.Entities;

namespace ApiServer.Controllers
{
    public class DeviceController : ControllerBase
    {
        [AuthRequestAttribute]
        [AuthResponseAttribute]
        [HttpPost(template: "DeviceCheckRemoteConnectionRequest")]
        public IActionResult DeviceCheckRemoteConnectionRequest([FromBody] string devicePublicKey)
        {
            bool fault = false;
            string deviceIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Device identity is: " + deviceIdentity);

            // Check device connection authorization
            // superflua --> bool isDeviceAuthorized = PostgreSQLClass.IsDeviceConnectionAuthorized(deviceIdentity, out fault);  --> già fatto nel filter
            bool isDeviceConnectionRequested = PostgreSQLClass.IsDeviceConnectionRequested(deviceIdentity, out fault);

            if (isDeviceConnectionRequested && !fault)
            {
                // Verifica dello stato della connessione, se è già attiva non devo fare nulla
                DeviceConnectionStatus connectionStatus = PostgreSQLClass.CheckDeviceConnection(deviceIdentity, out fault);

                // Altrimenti devo fare in modo che venga attivata la nuova connessione
                if (connectionStatus.State != ClientConnectionState.Connected)
                {

                    SshConnectionData connectionData = Utilities.CreateSshConnectionData();
                    // Saving device public key to allow its connection to the ssh server
                    SshKeysManagement.SaveKeys(connectionData, AppSettings.SshUser, "device_" + deviceIdentity, devicePublicKey, AppSettings.SshAuthorizedKeysPath, connectionStatus.SshForwarding);

                    // Generating Ssh connection details
                    DeviceConnectionStatus connectionDetails = GenerateSshConnectionDetails();

                    // Inserting connection details to database

                    PostgreSQLClass.SetDeviceConnectionDetails(deviceIdentity, connectionDetails, out fault);

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

        [AuthRequestAttribute]
        [AuthResponseAttribute]
        [HttpPost(template: "DeviceSetConnectionState")]
        public IActionResult DeviceSetConnectionState([FromBody] ClientConnectionState deviceConnectionState)
        {
            bool fault = false;
            string deviceIdentity = (string)HttpContext.Items["ClientName"];

            PostgreSQLClass.SetDeviceConnectionState(deviceIdentity, deviceConnectionState, out fault);

            if (!fault)

            {
                return Ok($"Status changed to: {deviceConnectionState}");
            }
            else
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Database error");
            }
        }




        private DeviceConnectionStatus GenerateSshConnectionDetails()
        {
            bool fault = false;

            // Inserting device connection details
            DeviceConnectionStatus deviceConnectionDetails = new DeviceConnectionStatus();
            deviceConnectionDetails.SshHost = AppSettings.SshHost;
            deviceConnectionDetails.SshUser = AppSettings.SshUser;
            deviceConnectionDetails.SshPort = AppSettings.SshPort;
            deviceConnectionDetails.State = ClientConnectionState.Ready;

            List<int> usedPorts = PostgreSQLClass.GetForwardingPorts(out fault);

            if (!fault)
            {
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
            else
            {
                throw new Exception("Exception in max forward port determination");
            }

        }
    }
}