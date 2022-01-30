using ApiServer.Core;
using ApiServer.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Infrastructure.Repositories
{
    public class ClientRepository : BaseRepository<Infrastructure.Models.Client>
    {
        public ClientRepository(sshondemandContext dbContext) : base(dbContext) { }

        public Client AddClient(string clientName, bool isDevice)
        {
            if (this.dbContext.Clients.Any(c => c.ClientName == clientName))
                return null;

            KeysGenerator keyGen = new KeysGenerator();

            var newClient = new Client()
            {
                ClientName = clientName,
                IsDevice = isDevice,
                IsDeveloper = !isDevice,
                ClientKey = keyGen.GetNewKey()
            };

            this.dbContext.Clients.Add(newClient);
            this.dbContext.SaveChanges();
            return newClient;
        }


        public IEnumerable<Client> GetAllDevices()
        {
            return dbContext.Clients.Where(c => c.IsDevice == true);
        }

        public IEnumerable<Client> GetAllDeveloper()
        {
            return dbContext.Clients.Where(c => c.IsDeveloper == true);
        }

        public bool DeleteDeviceById(int deviceId)
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

        public bool DeleteDeveloperById(int developerId)
        {
            var clientToRemove = this.dbContext.Clients.SingleOrDefault(c => c.Id == developerId);
            if (clientToRemove != null)
            {
                var authsToRemove = this.dbContext.DeveloperAuthorizations.Where(a => a.DeveloperId == developerId);
                this.dbContext.DeveloperAuthorizations.RemoveRange(authsToRemove);

                var devReqToRemove = this.dbContext.DeviceRequests.Where(r => r.RequestedByClientId == developerId);
                this.dbContext.DeviceRequests.RemoveRange(devReqToRemove);

                this.dbContext.Clients.Remove(clientToRemove);

                this.dbContext.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
