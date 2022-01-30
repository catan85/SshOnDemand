using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ApiServer.Application.Mapper;
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
        private readonly Ssh ssh;

        public DeveloperController(sshondemandContext dbContext, Queries queries, Ssh ssh)
        {
            this.dbContext = dbContext;
            this.queries = queries;
            this.ssh = ssh;
        }

        [HmacAuthRequest]
        [HmacAuthResponse]
        [HttpPost(template: "DeveloperDeviceConnectionRequest")]
        public IActionResult DeveloperDeviceConnectionRequest([FromBody] string deviceName)
        {
            string developerIdentity = (string)HttpContext.Items["ClientName"];

            Console.WriteLine("Developer identity is: " + developerIdentity);

            // Check developer device connection authorization
            bool isDeveloperAuthorized = this.queries.IsDeveloperConnectionToDeviceAuthorized( developerIdentity, deviceName);

            if (isDeveloperAuthorized)
            {
                // Inserting device connection request or updating the existing one
                this.queries.InsertDeviceConnectionRequest( deviceName, developerIdentity);
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
            bool isDeveloperAuthorized = this.queries.IsDeveloperConnectionToDeviceAuthorized( developerIdentity, args.DeviceName);

            if (isDeveloperAuthorized)
            {

                // Checking device connection status
                var deviceConnection = this.queries.CheckDeviceConnection(args.DeviceName);

                Core.Entities.DeviceConnectionStatus currentDeviceConnectionStatus = ClientConnectionMapper.Mapper.Map<Core.Entities.DeviceConnectionStatus>(deviceConnection);
                
                if (currentDeviceConnectionStatus.State == EnumClientConnectionState.Ready || currentDeviceConnectionStatus.State == EnumClientConnectionState.Connected)
                {
                    this.ssh.SaveClientKeys(currentDeviceConnectionStatus.SshForwarding, "developer_" + developerIdentity, args.DeveloperSshPublicKey);
                }

                return Ok(currentDeviceConnectionStatus);
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