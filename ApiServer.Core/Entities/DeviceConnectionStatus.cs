using SshOnDemandLibs;
using System;

namespace ApiServer.Core.Entities
{
    [Serializable]
    public class DeviceConnectionStatus
    {
        public EnumClientConnectionState State { get; set; }

        public string SshHost { get; set; }

        public int SshPort { get; set; }

        public string SshUser { get; set; }

        public int SshForwarding { get; set; }
    }
}
