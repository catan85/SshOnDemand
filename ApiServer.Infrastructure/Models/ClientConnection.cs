using System;
using System.Collections.Generic;

#nullable disable

namespace ApiServer.Infrastructure.Models
{
    public partial class ClientConnection
    {
        public int ClientId { get; set; }
        public short? Status { get; set; }
        public DateTime? ConnectionTimestamp { get; set; }
        public string SshIp { get; set; }
        public int? SshPort { get; set; }
        public int? SshForwarding { get; set; }
        public string SshUser { get; set; }

        public virtual Client Client { get; set; }
    }
}
