using ApiServer.Application.Mapper;
using ApiServer.Application.Requests;
using ApiServer.Application.Responses;
using ApiServer.Infrastructure;
using ApiServer.Infrastructure.Models;
using ApiServer.Infrastructure.Repositories;
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
        private readonly ClientRepository clientRepository;
        public ManagementController(sshondemandContext dbContext, Queries queries, ClientRepository clientRepository)
        {
            this.dbContext = dbContext;
            this.queries = queries;
            this.clientRepository = clientRepository;
        }

        [HttpGet(template: "GetAllDevices")]
        public IEnumerable<Core.Entities.Client> GetAllDevices()
        {
            // esempio accesso generico
            // return this.clientRepository.GetAll().Where(c => c.IsDevice == true).Select(c => ClientMapper.Mapper.Map<Application.Entities.Client>(c));

            // accesso da repository custom
            return clientRepository.GetAllDevices().Select(c => ClientMapper.Mapper.Map<Core.Entities.Client>(c));
        }

        [HttpPost(template: "AddDevice")]
        public Core.Entities.Client AddDevice([FromBody] Application.Requests.ManagementRequestAddDevice newDevice)
        {
            return ClientMapper.Mapper.Map<Core.Entities.Client>(this.clientRepository.AddClient(newDevice.ClientName, true));
        }

        [HttpDelete(template: "DeleteDevice")]
        public bool DeleteDevice(int deviceId)
        {
            return this.clientRepository.DeleteDeviceById(deviceId);
        }

        [HttpGet(template: "GetAllDeveloper")]
        public IEnumerable<Core.Entities.Client> GetAllDeveloper()
        {
            return clientRepository.GetAllDeveloper().Select(c => ClientMapper.Mapper.Map<Core.Entities.Client>(c));
        }

        [HttpPost(template: "AddDeveloper")]
        public Core.Entities.Client AddDeveloper([FromBody] ManagementRequestAddDeveloper newDeveloper)
        {
            return ClientMapper.Mapper.Map<Core.Entities.Client>(this.clientRepository.AddClient(newDeveloper.ClientName, false));
        }

        [HttpDelete(template: "DeleteDeveloper")]
        public bool DeleteDeveloper(int developerId)
        {
            return this.clientRepository.DeleteDeveloperById(developerId);
        }

        [HttpGet(template: "GetDeveloperAuthorizations")]
        public ManagementResponseGetDeveloperAuthorizations GetDeveloperAuthorizations(int developerId)
        {
            var authorizations = this.dbContext.DeveloperAuthorizations
                .Where(c => c.DeveloperId == developerId)
                .Include(c => c.Device);

            var response = new ManagementResponseGetDeveloperAuthorizations();
            response.DeveloperId = developerId;
            response.AllowedDevices = new List<Core.Entities.Client>();
            foreach (var auth in authorizations)
            {
                response.AllowedDevices.Add(ClientMapper.Mapper.Map<Core.Entities.Client>(auth.Device));
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
