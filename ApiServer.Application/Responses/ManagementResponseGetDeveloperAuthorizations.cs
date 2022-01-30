using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer.Application.Responses
{
    public class ManagementResponseGetDeveloperAuthorizations
    {
        public int DeveloperId { get; set; }
        public List<Core.Entities.Client> AllowedDevices { get; set; }
    }
}
