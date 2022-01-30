using ApiServer.Core.Entities;
using System.Collections.Generic;

namespace ApiServer.Core.Repositories
{
    public interface IClientsRepository
    {
        Client AddClient(string deviceName, bool isDevice);
        bool DeleteDeveloperById(int developerId);
        bool DeleteDeviceById(int deviceId);
        IEnumerable<Client> GetAllDeveloper();
        IEnumerable<Client> GetAllDevices();
    }
}