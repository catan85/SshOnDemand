using ApiServer.Application.Mapper;
using ApiServer.Application.Requests;
using ApiServer.Application.Responses;
using ApiServer.Infrastructure;
using ApiServer.Infrastructure.Models;
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
        private readonly Queries queries;

        public ManagementController(sshondemandContext dbContext, Queries queries)
        {
            this.dbContext = dbContext;
            this.queries = queries;
        }

        [HttpGet(template: "GetAllDevices")]
        public IEnumerable<Application.Entities.Client> GetAllDevices()
        {
            return this.dbContext.Clients.Where(c => c.IsDevice == true).Select(c => ClientMapper.Mapper.Map<Application.Entities.Client>(c));
        }

        [HttpPost(template: "AddDevice")]
        public bool AddDevice([FromBody] Application.Requests.ManagementRequestAddDevice newDevice)
        {
            return this.queries.AddDevice(ClientMapper.Mapper.Map<Client>(newDevice));
        }

        [HttpDelete(template: "DeleteDevice")]
        public bool DeleteDevice(int deviceId)
        {
            var clientToRemove = this.dbContext.Clients.SingleOrDefault(c => c.Id == deviceId);
            if (clientToRemove != null)
            {
                var authsToRemove = this.dbContext.DeveloperAuthorizations.Where(a => a.DeviceId == deviceId);
                this.dbContext.DeveloperAuthorizations.RemoveRange(authsToRemove);

                var devReqToRemove = this.dbContext.DeviceRequests.Where(r => r.ClientId == deviceId);
                this.dbContext.DeviceRequests.RemoveRange(devReqToRemove);
               
                var cliConnToRemove = this.dbContext.ClientConnections.Where(c => c.ClientId == deviceId);
                this.dbContext.ClientConnections.RemoveRange(cliConnToRemove);

                this.dbContext.Clients.Remove(clientToRemove);

                this.dbContext.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }    
        }

        [HttpGet(template: "GetAllDeveloper")]
        public IEnumerable<Application.Entities.Client> GetAllDeveloper()
        {
            return this.dbContext.Clients.Where(c => c.IsDeveloper == true).Select(c => ClientMapper.Mapper.Map<Application.Entities.Client>(c));
        }

        [HttpPost(template: "AddDeveloper")]
        public bool AddDeveloper([FromBody] ManagementRequestAddDeveloper newDeveloper)
        {
            return this.queries.AddDeveloper(ClientMapper.Mapper.Map<Client>(newDeveloper));
        }

        [HttpDelete(template: "DeleteDeveloper")]
        public bool DeleteDeveloper(int developerId)
        { 
            return false;
        }

        [HttpGet(template: "GetDeveloperAuthorizations")]
        public ManagementResponseGetDeveloperAuthorizations GetDeveloperAuthorizations(int developerId)
        {
            var authorizations = this.dbContext.DeveloperAuthorizations
                .Where(c => c.DeveloperId == developerId)
                .Include(c => c.Device);

            var response = new ManagementResponseGetDeveloperAuthorizations();
            response.DeveloperId = developerId;
            response.AllowedDevices = new List<Application.Entities.Client>();
            foreach (var auth in authorizations)
            {
                response.AllowedDevices.Add(ClientMapper.Mapper.Map<Application.Entities.Client>(auth.Device));
            }
            return response;
        }

        /*
        [HttpPost(template: "UpdateDeveloperAuthorizations")]
        public bool UpdateDeveloperAuthorizations(ManagementRequestUpdateDeveloperAuthorizations body)
        {
          
        }
        */
    }
}
