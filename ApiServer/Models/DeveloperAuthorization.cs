using System;
using System.Collections.Generic;

#nullable disable

namespace ApiServer.Models
{
    public partial class DeveloperAuthorization
    {
        public int? DeveloperId { get; set; }
        public int? DeviceId { get; set; }

        public virtual Client Developer { get; set; }
        public virtual Client Device { get; set; }
    }
}
