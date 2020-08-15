using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiServer.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiServer.Controllers
{
    
    public class SecretController : ControllerBase
    {
        [AuthRequestAttribute]
        [AuthResponseAttribute]

        [HttpGet(template: "secret")]
        public IActionResult GetSecret()
        {
            return Ok("Non ho segreti");
        }
    }
}