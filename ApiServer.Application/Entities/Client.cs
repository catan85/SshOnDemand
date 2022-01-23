using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer.Application.Entities
{
    public class Client
    {
        public int Id { get; set; }
        public bool IsDevice { get; set; }
        public bool IsDeveloper { get; set; }
        public string ClientKey { get; set; }
        public string ClientName { get; set; }

        public Client()
        {

        }

    }
}
