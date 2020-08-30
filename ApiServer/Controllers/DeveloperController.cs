using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ApiServer.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SshOnDemandEntities;

namespace ApiServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeveloperController : ControllerBase
    {
        [AuthRequestAttribute]
        [AuthResponseAttribute]

        [HttpGet(template: "DeveloperAuth")]
        public IActionResult DeveloperAuth()
        {
            return Ok("You're authenticated");
        }

        [HttpPost(template: "DeveloperDeviceConnectionRequest")]
        public IActionResult DeveloperDeviceConnectionRequest([FromBody] DeveloperDeviceConnectionRequestArgs args)
        {
            bool fault = false;
            string developerIdentity = (string)HttpContext.Items["ClientName"];

            // Check developer device connection authorization
            bool isDeveloperAuthorized = PostgreSQLClass.IsDeveloperConnectionToDeviceAuthorized(developerIdentity, args.DeviceName, out fault);

            if (isDeveloperAuthorized && !fault)
            {
                // Inserting device connection request
                PostgreSQLClass.InsertDeviceConnectionRequest(args.DeviceName, true, out fault);
                return Ok("Request has been set");
            }
            else  if (!isDeveloperAuthorized)
            {
                return Unauthorized("Develeper is not authorized to connect to this device");
            }
            else
            {   
                return StatusCode((int)HttpStatusCode.InternalServerError, "Database error");
            }


            
        }

    }
}