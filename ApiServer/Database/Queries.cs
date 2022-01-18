namespace ApiServer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using SshOnDemandLibs;
    using ApiServer.Models;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Classe di interfacciamento con database Postgresql
    /// </summary>
    public static class Queries
    {

        #region common

        public static DeviceConnectionStatus CheckDeviceConnection(sshondemandContext dbContext, string deviceName)
        {
            DeviceConnectionStatus status = new DeviceConnectionStatus();
            status.State = ClientConnectionState.Disconnected;
            var connectionStatus = dbContext.ClientConnections.Where(c => c.Client.ClientName == deviceName).SingleOrDefault();
            if (connectionStatus != null)
            {
                status.State = (ClientConnectionState)(connectionStatus.Status);
                status.SshHost = connectionStatus.SshIp;
                status.SshPort = connectionStatus.SshPort.Value;
                status.SshForwarding = connectionStatus.SshForwarding.Value;
                status.SshUser = connectionStatus.SshUser;
            }
            return status;
        }
        #endregion

        #region device queries


        public static bool IsDeviceConnectionRequested(sshondemandContext dbContext, string deviceName)
        {
            var request = dbContext.DeviceRequests.SingleOrDefault(r => r.Client.ClientName == deviceName);

            if (request != null)
            {
                return request.IsRequested.Value;
            }
            else
            {
                return false;
            }
        }

        public static void SetDeviceConnectionDetails(sshondemandContext dbContext, string deviceName, DeviceConnectionStatus status)
        {
          
            var device = dbContext.Clients.SingleOrDefault(c => c.ClientName == deviceName);
            var existDeviceRequest = dbContext.DeviceRequests.Any(r => r.ClientId == device.Id);

            if (!existDeviceRequest)
            {
                var newConnection = new ClientConnection() {
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

        public static void SetDeviceConnectionState(sshondemandContext dbContext, string deviceName, ClientConnectionState state)
        {
            var clientConnection = dbContext.ClientConnections.SingleOrDefault(c => c.Client.ClientName == deviceName);
            if (clientConnection!=null)
            {
                clientConnection.Status = (short)state;
                clientConnection.ConnectionTimestamp = DateTime.UtcNow;
                dbContext.SaveChanges();
            }
        }

        public static List<int> GetForwardingPorts(sshondemandContext dbContext)
        {
            var connections = dbContext.ClientConnections.Where(cc => cc.Status != 0);
            List<int> result = new List<int>();
            foreach (var connection in connections)
            {
                result.Add(connection.SshForwarding.Value);
            }
            return result;
        }

        #endregion

        #region developer queries
        public static bool IsDeveloperConnectionToDeviceAuthorized(sshondemandContext dbContext, string developerName, string deviceName)
        {
            return dbContext.DeveloperAuthorizations
                .Any(c => c.Developer.ClientName == developerName && c.Device.ClientName == deviceName);
        }

        public static void InsertDeviceConnectionRequest(sshondemandContext dbContext, string deviceName, string developerName)
        {
            var result = dbContext.DeviceRequests
                .SingleOrDefault(connectionRequest => 
                    connectionRequest.Client.ClientName == deviceName &&
                    connectionRequest.RequestedByClient.ClientName == developerName &&
                    !dbContext.DeviceRequests.Any(existingRequest => 
                            existingRequest.ClientId == connectionRequest.ClientId &&
                            existingRequest.IsRequested == true
                            )
                    );

            if (result != null)
            {
                result.IsRequested = true;
                result.RequestTimestamp = DateTime.UtcNow;
                dbContext.SaveChanges();
            }
        }

        #endregion

        #region cyclic checks
        public static void DeactivateOldRequests(sshondemandContext dbContext, int maxRequestAgeInSeconds, out List<string> deactivatedClients)
        {
            var oldRequestTimeLimit = (DateTime.UtcNow - new TimeSpan(0, 0, maxRequestAgeInSeconds));
            var oldRequests = dbContext.DeviceRequests
                .Include(r=>r.Client)
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

        public static void ResetOldConnections(sshondemandContext dbContext, int maxConnectionAgeInSeconds, out List<string> deactivatedClients)
        {
            var oldConnectionsTimeLimit = (DateTime.UtcNow - new TimeSpan(0, 0, maxConnectionAgeInSeconds));
            var oldConnections = dbContext.ClientConnections
                .Include(c=>c.Client)
                .Where(c => (c.Status != 0) && c.ConnectionTimestamp < oldConnectionsTimeLimit);
            deactivatedClients = new List<string>();
            foreach (var oldConnection in oldConnections)
            {
                oldConnection.Status = 0;
                deactivatedClients.Add(oldConnection.Client.ClientName);
            }
            dbContext.SaveChanges();
        }

        #endregion

    }
}