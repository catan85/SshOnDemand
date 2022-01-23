using System;
using System.Collections.Generic;

#nullable disable

namespace ApiServer.Infrastructure.Models
{
    public partial class DeviceRequest
    {
        public int ClientId { get; set; }
        public bool? IsRequested { get; set; }
        public DateTime? RequestTimestamp { get; set; }
        public int? RequestedByClientId { get; set; }

        public virtual Client Client { get; set; }
        public virtual Client RequestedByClient { get; set; }
    }
}
