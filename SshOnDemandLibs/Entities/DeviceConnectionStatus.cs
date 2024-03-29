﻿using System;
using System.Collections.Generic;
using System.Text;


namespace SshOnDemandLibs
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
