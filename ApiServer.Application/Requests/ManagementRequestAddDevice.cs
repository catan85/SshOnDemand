using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Application.Requests
{
    public class ManagementRequestAddDevice
    {
        public string ClientKey { get; set; }
        public string ClientName { get; set; }
    }
}
