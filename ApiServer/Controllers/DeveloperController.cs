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

    public class DeveloperController : ControllerBase
    {

        [AuthRequestAttribute]
        [AuthResponseAttribute]
        [HttpPost(template: "DeveloperDeviceConnectionRequest")]
        public IActionResult DeveloperDeviceConnectionRequest([FromBody] DeveloperDeviceConnectionRequestArgs args)
        {
            bool fault = false;
            string developerIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Developer identity is: " + developerIdentity);

            // Check developer device connection authorization
            bool isDeveloperAuthorized = PostgreSQLClass.IsDeveloperConnectionToDeviceAuthorized(developerIdentity, args.DeviceName, out fault);

            if (isDeveloperAuthorized && !fault)
            {
                SshConnectionData connectionData = CreateSshConnectionData();
                
                // Saving Developer public key to allow its connection to the ssh server
                SshKeysManagement.SaveKeys(connectionData, AppSettings.SshUser, "developer_" + developerIdentity, args.DeveloperSshPublicKey, AppSettings.SshAuthorizedKeysPath);

                // Inserting device connection request
                PostgreSQLClass.InsertDeviceConnectionRequest(args.DeviceName, true, out fault);
                return Ok("Request has been set");
            }
            else  if (!isDeveloperAuthorized)
            {
                return StatusCode(403, "Develeper is not authorized to connect to this device");
            }
            else
            {   
                return StatusCode((int)HttpStatusCode.InternalServerError, "Database error");
            }
        }

        private SshConnectionData CreateSshConnectionData()
        {
            SshConnectionData connectionData = new SshConnectionData();
            connectionData.AuthenticationMode = SshAuthMode.WithPassword;
            connectionData.Host = AppSettings.SshHost;
            connectionData.Port = AppSettings.SshPort;
            connectionData.Username = AppSettings.SshUser;
            connectionData.Password = AppSettings.SshPass;
            return connectionData;
        }

        [AuthRequestAttribute]
        [AuthResponseAttribute]
        [HttpPost(template: "DeveloperCheckDeviceConnection")]
        public IActionResult DeveloperCheckDeviceConnection([FromBody] string deviceName)
        {
            bool fault = false;
            string developerIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Developer identity is: " + developerIdentity);

            // Check developer device connection authorization
            bool isDeveloperAuthorized = PostgreSQLClass.IsDeveloperConnectionToDeviceAuthorized(developerIdentity, deviceName, out fault);

            if (isDeveloperAuthorized && !fault)
            {
                // Checking device connection status
                DeviceConnectionStatus status = PostgreSQLClass.CheckDeviceConnection(deviceName, out fault);

                return Ok(status);
            }
            else if (!isDeveloperAuthorized)
            {
                return StatusCode(403, "Develeper is not authorized to connect to this device");
            }
            else
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Database error");
            }
        }

    }
}