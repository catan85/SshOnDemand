using System;
using System.Collections.Generic;
using System.Text;


namespace SshOnDemandEntities
{
    [Serializable]
    public class DeviceConnectionStatus
    {
        public ClientConnectionState State { get; set; }

        public string SshHost { get; set; }

        public int SshPort { get; set; }

        public int SshForwarding { get; set; }
    }
}
