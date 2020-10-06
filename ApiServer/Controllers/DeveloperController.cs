using System;
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
        public IActionResult DeveloperDeviceConnectionRequest([FromBody] string deviceName)
        {
            bool fault = false;
            string developerIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Developer identity is: " + developerIdentity);

            // Check developer device connection authorization
            bool isDeveloperAuthorized = PostgreSQLClass.IsDeveloperConnectionToDeviceAuthorized(developerIdentity, deviceName, out fault);

            if (isDeveloperAuthorized && !fault)
            {
                // Inserting device connection request
                PostgreSQLClass.InsertDeviceConnectionRequest(deviceName, developerIdentity, true, out fault);
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


        [AuthRequestAttribute]
        [AuthResponseAttribute]
        [HttpPost(template: "DeveloperCheckDeviceConnection")]
        public IActionResult DeveloperCheckDeviceConnection([FromBody] DeveloperCheckDeviceConnectionArgs args)
        {
            bool fault = false;
            string developerIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Developer identity is: " + developerIdentity);

            // Check developer device connection authorization
            bool isDeveloperAuthorized = PostgreSQLClass.IsDeveloperConnectionToDeviceAuthorized(developerIdentity, args.DeviceName, out fault);

            if (isDeveloperAuthorized && !fault)
            {

                // Checking device connection status
                DeviceConnectionStatus status = PostgreSQLClass.CheckDeviceConnection(args.DeviceName, out fault);


                if (status.State == ClientConnectionState.Ready || status.State == ClientConnectionState.Connected)
                {
                    // Saving Developer public key to allow its connection to the ssh server
                    SshConnectionData connectionData = Utilities.CreateSshConnectionData();
                    SshKeysManagement.SaveKeys(connectionData, AppSettings.SshUser, "developer_" + developerIdentity, args.DeveloperSshPublicKey, AppSettings.SshAuthorizedKeysPath, status.SshForwarding);
                }

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