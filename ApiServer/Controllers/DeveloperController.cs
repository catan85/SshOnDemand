using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ApiServer.Filters;
using ApiServer.Infrastructure;
using ApiServer.Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SshOnDemandLibs;
using SshOnDemandLibs.Entities;

namespace ApiServer.Controllers
{

    public class DeveloperController : ControllerBase
    {
        private readonly sshondemandContext dbContext;
        private readonly Queries queries;
        public DeveloperController(sshondemandContext dbContext, Queries queries)
        {
            this.dbContext = dbContext;
            this.queries = queries;
        }

        [HmacAuthRequest]
        [HmacAuthResponse]
        [HttpPost(template: "DeveloperDeviceConnectionRequest")]
        public IActionResult DeveloperDeviceConnectionRequest([FromBody] string deviceName)
        {
            string developerIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Developer identity is: " + developerIdentity);

            // Check developer device connection authorization
            bool isDeveloperAuthorized = this.queries.IsDeveloperConnectionToDeviceAuthorized(dbContext, developerIdentity, deviceName);

            if (isDeveloperAuthorized)
            {
                // Inserting device connection request or updating the existing one
                this.queries.InsertDeviceConnectionRequest(dbContext, deviceName, developerIdentity);
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
            bool isDeveloperAuthorized = this.queries.IsDeveloperConnectionToDeviceAuthorized(dbContext, developerIdentity, args.DeviceName);

            if (isDeveloperAuthorized)
            {

                // Checking device connection status
                Core.Entities.DeviceConnectionStatus status = this.queries.CheckDeviceConnection(dbContext, args.DeviceName);


                if (status.State == EnumClientConnectionState.Ready || status.State == EnumClientConnectionState.Connected)
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