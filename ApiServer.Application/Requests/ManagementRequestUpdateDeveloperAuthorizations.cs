using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Application.Requests
{
    public class ManagementRequestUpdateDeveloperAuthorizations
    {
        public int DeveloperId { get; set; }
        public IEnumerable<int> AuthorizedDeviceIds { get; set; }
    }
}
