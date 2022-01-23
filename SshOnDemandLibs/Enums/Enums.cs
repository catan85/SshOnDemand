using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SshOnDemandLibs
{
    public enum EnumClientConnectionState { Disconnected = 0, Ready = 1, Connected = 2, NotRequest = 3, ClosedSsh = 4};
    public enum EnumSshConnectionState { Closed = 0, Open = 1 };
    public enum EnumSshAuthMode { WithPassword, WithCertificates };

    public enum EnumSshKeyCommand { LoadNewKey, UnloadKey }
}
