using ApiServer.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Infrastructure.Repositories
{
    public class DeviceRequestsRepository : BaseRepository<DeviceRequest>
    {
        public DeviceRequestsRepository(sshondemandContext dbContext) : base(dbContext) { }

        public void InsertDeviceConnectionRequest(string deviceName, string developerName)
        {
            var result = dbContext.DeviceRequests
                .SingleOrDefault(connectionRequest =>
                    connectionRequest.Client.ClientName == deviceName &&
                    connectionRequest.RequestedByClient.ClientName == developerName
                    );

            if (result != null)
            {
                result.IsRequested = true;
                result.RequestTimestamp = DateTime.UtcNow;
                dbContext.SaveChanges();
            }
        }

        public void DeactivateOldDeviceRequests(int maxRequestAgeInSeconds, out List<string> deactivatedClients)
        {
            var oldRequestTimeLimit = (DateTime.UtcNow - new TimeSpan(0, 0, maxRequestAgeInSeconds));
            var oldRequests = dbContext.DeviceRequests
                .Include(r => r.Client)
                .Where(r => r.IsRequested.Value && (r.RequestTimestamp < oldRequestTimeLimit))
                ;
            deactivatedClients = new List<string>();
            foreach (var oldRequest in oldRequests)
            {
                oldRequest.IsRequested = false;
                deactivatedClients.Add(oldRequest.Client.ClientName);
            }
            dbContext.SaveChanges();
        }

    }
}
