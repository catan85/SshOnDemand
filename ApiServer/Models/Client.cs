using System;
using System.Collections.Generic;

#nullable disable

namespace ApiServer.Models
{
    public partial class Client
    {
        public int Id { get; set; }
        public bool? IsDevice { get; set; }
        public bool? IsDeveloper { get; set; }
        public string ClientKey { get; set; }
        public string ClientName { get; set; }
    }
}
