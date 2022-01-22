using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer.Entities
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

        public Client(Models.Client client)
        {
            this.Id = client.Id;
            this.ClientName = client.ClientName;
            this.ClientKey = client.ClientKey;
            this.IsDeveloper = client.IsDeveloper.Value;
            this.IsDevice = client.IsDevice.Value;
        }
    }
}
