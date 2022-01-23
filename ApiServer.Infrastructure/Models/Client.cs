using System;
using System.Collections.Generic;

#nullable disable

namespace ApiServer.Infrastructure.Models
{
    public partial class Client
    {
        public Client()
        {
            DeveloperAuthorizationDevelopers = new HashSet<DeveloperAuthorization>();
            DeveloperAuthorizationDevices = new HashSet<DeveloperAuthorization>();
            DeviceRequestRequestedByClients = new HashSet<DeviceRequest>();
        }

        public int Id { get; set; }
        public bool? IsDevice { get; set; }
        public bool? IsDeveloper { get; set; }
        public string ClientKey { get; set; }
        public string ClientName { get; set; }

        public virtual ClientConnection ClientConnection { get; set; }
        public virtual DeviceRequest DeviceRequestClient { get; set; }
        public virtual ICollection<DeveloperAuthorization> DeveloperAuthorizationDevelopers { get; set; }
        public virtual ICollection<DeveloperAuthorization> DeveloperAuthorizationDevices { get; set; }
        public virtual ICollection<DeviceRequest> DeviceRequestRequestedByClients { get; set; }
    }
}
