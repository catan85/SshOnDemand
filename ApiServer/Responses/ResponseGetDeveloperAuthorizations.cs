using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer.Responses
{
    public class ResponseGetDeveloperAuthorizations
    {
        public int DeveloperId { get; set; }
        public List<Entities.Client> AllowedDevices { get; set; }
    }
}
