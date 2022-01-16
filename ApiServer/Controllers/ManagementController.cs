using ApiServer.Entities;
using ApiServer.Models;
using ApiServer.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer.Controllers
{
    public class ManagementController : ControllerBase
    {
        private readonly sshondemandContext dbContext;
        public ManagementController(sshondemandContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet(template: "GetAllDevices")]
        public IEnumerable<Entities.Client> GetAllDevices()
        {
            return this.dbContext.Clients.Where(c => c.IsDevice == true).Select(c => new Entities.Client(c));
        }

        [HttpPost(template: "AddDevice")]
        public bool AddDevice(Entities.Client device)
        {
            return false;
        }

        [HttpDelete(template: "DeleteDevice")]
        public bool DeleteDevice(Entities.Client device)
        {
            return false;
        }

        [HttpGet(template: "GetAllDeveloper")]
        public IEnumerable<Entities.Client> GetAllDeveloper()
        {
            return this.dbContext.Clients.Where(c => c.IsDeveloper == true).Select(c => new Entities.Client(c));
        }

        [HttpPost(template: "AddDeveloper")]
        public bool AddDeveloper(Entities.Client developer)
        {
            return false;
        }

        [HttpDelete(template: "DeleteDeveloper")]
        public bool DeleteDeveloper(Entities.Client developer)
        { 
            return false;
        }

        [HttpGet(template: "GetDeveloperAuthorizations")]
        public ResponseGetDeveloperAuthorizations GetDeveloperAuthorizations(int developerId)
        {
            var authorizations = this.dbContext.DeveloperAuthorizations
                .Where(c => c.DeveloperId == developerId)
                .Include(c => c.Device);

            var response = new ResponseGetDeveloperAuthorizations();
            response.DeveloperId = developerId;
            response.AllowedDevices = new List<Entities.Client>();
            foreach (var auth in authorizations)
            {
                response.AllowedDevices.Add(new Entities.Client(auth.Device));
            }
            return response;
  

        }
    }
}
