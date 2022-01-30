using ApiServer.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using SshOnDemandLibs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Infrastructure.Repositories
{
    public class ClientConnectionsRepository : BaseRepository<ClientConnection>
    {
        public ClientConnectionsRepository(sshondemandContext dbContext) : base(dbContext) { }

        public ClientConnection GetByClientName(string clientName)
        {
            return this.dbContext.ClientConnections.SingleOrDefault(c => c.Client.ClientName == clientName);
        }

        public List<int> GetForwardingPorts()
        {
            var connections = dbContext.ClientConnections.Where(cc => cc.Status != 0);
            List<int> result = new List<int>();
            foreach (var connection in connections)
            {
                result.Add(connection.SshForwarding.Value);
            }
            return result;
        }

        public ClientConnection CheckDeviceConnection(string deviceName)
        {
            Core.Entities.DeviceConnectionStatus status = new Core.Entities.DeviceConnectionStatus();
            status.State = EnumClientConnectionState.Disconnected;
            var connectionStatus = dbContext.ClientConnections.Where(c => c.Client.ClientName == deviceName).SingleOrDefault();
            return connectionStatus;
        }

        public void SetDeviceConnectionDetails(string deviceName, Core.Entities.DeviceConnectionStatus status)
        {
            var device = dbContext.Clients.SingleOrDefault(c => c.ClientName == deviceName);
            var existDeviceRequest = dbContext.DeviceRequests.Any(r => r.ClientId == device.Id);

            if (!existDeviceRequest)
            {
                var newConnection = new ClientConnection()
                {
                    Client = device,
                    Status = (short)status.State,
                    ConnectionTimestamp = DateTime.UtcNow,
                    SshIp = status.SshHost,
                    SshPort = status.SshPort,
                    SshUser = status.SshUser,
                    SshForwarding = status.SshForwarding
                };
                dbContext.ClientConnections.Add(newConnection);
            }

            var deviceConnection = dbContext.ClientConnections.SingleOrDefault(c => c.ClientId == device.Id);
            if (deviceConnection != null)
            {
                deviceConnection.Status = (short)status.State;
                deviceConnection.ConnectionTimestamp = DateTime.UtcNow;
                deviceConnection.SshIp = status.SshHost;
                deviceConnection.SshPort = status.SshPort;
                deviceConnection.SshUser = status.SshUser;
                deviceConnection.SshForwarding = status.SshForwarding;
            }

            dbContext.SaveChanges();
        }

        public void SetDeviceConnectionState(string deviceName, EnumClientConnectionState state)
        {
            var clientConnection = dbContext.ClientConnections.SingleOrDefault(c => c.Client.ClientName == deviceName);
            if (clientConnection != null)
            {
                clientConnection.Status = (short)state;
                clientConnection.ConnectionTimestamp = DateTime.UtcNow;
                dbContext.SaveChanges();
            }
        }

        public void ResetOldConnections(int maxConnectionAgeInSeconds, out List<string> deactivatedClients)
        {
            var oldConnectionsTimeLimit = (DateTime.UtcNow - new TimeSpan(0, 0, maxConnectionAgeInSeconds));
            var oldConnections = dbContext.ClientConnections
                .Include(c => c.Client)
                .Where(c => (c.Status != 0) && c.ConnectionTimestamp < oldConnectionsTimeLimit);
            deactivatedClients = new List<string>();
            foreach (var oldConnection in oldConnections)
            {
                oldConnection.Status = 0;
                deactivatedClients.Add(oldConnection.Client.ClientName);
            }
            dbContext.SaveChanges();
        }
    }
}
