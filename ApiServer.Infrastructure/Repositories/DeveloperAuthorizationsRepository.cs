using ApiServer.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Infrastructure.Repositories
{
    public class DeveloperAuthorizationsRepository : BaseRepository<DeviceRequest>
    {
        public DeveloperAuthorizationsRepository(sshondemandContext dbContext) : base(dbContext) { }

        public bool IsDeveloperConnectionToDeviceAuthorized(string developerName, string deviceName)
        {
            return dbContext.DeveloperAuthorizations
                .Any(c => c.Developer.ClientName == developerName && c.Device.ClientName == deviceName);
        }

        public List<Client> GetDeveloperAuthorizedClients(int developerId)
        {
            var authorizations = this.dbContext.DeveloperAuthorizations
                .Where(c => c.DeveloperId == developerId)
                .Include(c => c.Device);

            return authorizations.Select(a => a.Device).ToList();
        }

        public void ChangeAuthorizations(int developerId, IEnumerable<int> devicesToEnable)
        {
            var currentAuths = this.dbContext.DeveloperAuthorizations.Where(a => a.DeveloperId == developerId);
            this.dbContext.DeveloperAuthorizations.RemoveRange(currentAuths);

            if (devicesToEnable != null && devicesToEnable.Any())
            {
                IEnumerable<DeveloperAuthorization> newAuthorizations = devicesToEnable
                .Select(d => new DeveloperAuthorization() { DeveloperId = developerId, DeviceId = d });
                this.dbContext.DeveloperAuthorizations.AddRange(newAuthorizations);
            }

            this.dbContext.SaveChanges();
        }

    }
}
