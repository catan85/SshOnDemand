using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SshOnDemandLibs
{
    public enum ClientConnectionState { Disconnected = 0, Ready = 1, Connected = 2 };
    public enum SshConnectionState { Closed = 0, Open = 1 };
    public enum SshAuthMode { WithPassword, WithCertificates };
}
