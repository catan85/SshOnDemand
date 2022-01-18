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

    public class DeveloperController : ControllerBase
    {
        private readonly sshondemandContext dbContext;
        public DeveloperController(sshondemandContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HmacAuthRequest]
        [HmacAuthResponse]
        [HttpPost(template: "DeveloperDeviceConnectionRequest")]
        public IActionResult DeveloperDeviceConnectionRequest([FromBody] string deviceName)
        {
            string developerIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Developer identity is: " + developerIdentity);

            // Check developer device connection authorization
            bool isDeveloperAuthorized = Queries.IsDeveloperConnectionToDeviceAuthorized(dbContext, developerIdentity, deviceName);

            if (isDeveloperAuthorized)
            {
                // Inserting device connection request
                Queries.InsertDeviceConnectionRequest(dbContext, deviceName, developerIdentity);
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


        [HmacAuthRequest]
        [HmacAuthResponse]
        [HttpPost(template: "DeveloperCheckDeviceConnection")]
        public IActionResult DeveloperCheckDeviceConnection([FromBody] DeveloperCheckDeviceConnectionArgs args)
        {
            bool fault = false;
            string developerIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Developer identity is: " + developerIdentity);

            // Check developer device connection authorization
            bool isDeveloperAuthorized = Queries.IsDeveloperConnectionToDeviceAuthorized(dbContext, developerIdentity, args.DeviceName);

            if (isDeveloperAuthorized)
            {

                // Checking device connection status
                DeviceConnectionStatus status = Queries.CheckDeviceConnection(dbContext, args.DeviceName);


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