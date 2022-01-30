using ApiServer.Core.Entities;
using SshOnDemandLibs;
using SshOnDemandLibs.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Infrastructure
{
    public class Ssh
    {
        readonly AppSettings settings;
        public Ssh(AppSettings settings)
        {
            this.settings = settings;
        }
        public void SaveClientKeys(
            int portForwarding,
            string clientIdentity,
            string clientSshPublicKey)
        {
            // Saving Developer public key to allow its connection to the ssh server
            SshConnectionData sftpConnectionData = GetSftpConnectionData();
            SshKeysManagement.SaveKeys(
                sftpConnectionData, 
                settings.SshUser, 
                clientIdentity,
                clientSshPublicKey, 
                settings.SshAuthorizedKeysPath,
                portForwarding
                );
            
        }


        public void UnloadClientKeys(List<string> clientNames)
        {
            SshConnectionData sftpConnectionData = GetSftpConnectionData();

            foreach (string clientName in clientNames)
            {
                SshKeysManagement.UnloadKey(sftpConnectionData, clientName, settings.SshAuthorizedKeysPath);
            }
        }

        private SshConnectionData GetSftpConnectionData()
        {
            SshConnectionData connectionData = new SshConnectionData();
            connectionData.AuthenticationMode = EnumSshAuthMode.WithPassword;
            connectionData.Host = settings.SshHost;
            connectionData.Port = settings.SshPort;
            connectionData.Username = settings.SshUser;
            connectionData.Password = settings.SshPass;
            return connectionData;
        }
    }
}
