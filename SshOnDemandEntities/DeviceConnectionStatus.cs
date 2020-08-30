using System;
using System.Collections.Generic;
using System.Text;

namespace SshOnDemandEntities
{
    [Serializable]
    public class DeviceConnectionStatus
    {
        public short Status { get; set; }

        public string SshHost { get; set; }

        public int SshPort { get; set; }

        public int SshForwarding { get; set; }
    }
}
