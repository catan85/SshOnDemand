using Renci.SshNet;
using SshOnDemandLibs;
using SshOnDemandLibs.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshOnDemandLibs
{
    public class SshHelper
    {

        public SshHelper()
        {
            // Keys generation at every application start
            SshKeys keys = SshKeysManagement.GenerateKeys();

            this.PublicKey = keys.PublicKey;
            this.PrivateKey = keys.PrivateKey;
   
        }

        public EnumSshConnectionState ConnectionState
        {
            get
            {

                var status =  this.Connection != null && this.Connection.IsConnected? EnumSshConnectionState.Open : EnumSshConnectionState.Closed;
                return status;
            }
        }

        public int CurrentForwardingPort
        {
            get;set;
        }

        private SshClient Connection = null;


        private string _publicKey;
        private string _privateKey;

        public string PublicKey { get => _publicKey; set => _publicKey = value; }
        public string PrivateKey { get => _privateKey; set => _privateKey = value; }


        public void OpenSshConnection(DeviceConnectionStatus connectionDetails)
        {
            SshConnectionData connectionData = new SshConnectionData();
            connectionData.AuthenticationMode = EnumSshAuthMode.WithCertificates;
            connectionData.Host = connectionDetails.SshHost;
            connectionData.Port = connectionDetails.SshPort;
            connectionData.Username = connectionDetails.SshUser;
            connectionData.PrivateKey = this.PrivateKey;

            Connect(connectionData);

        }

        public void OpenSshConnection(SshConnectionData connectionData)
        {
            Connect(connectionData);
        }


        // Questa è quella che dovrà fare il developer
        public void EnableLocalForwarding(DeviceConnectionStatus connectionDetails)
        {
            CurrentForwardingPort = connectionDetails.SshForwarding;
            CreateLocalForwarding("127.0.0.1", Convert.ToUInt32(connectionDetails.SshForwarding), "127.0.0.1", Convert.ToUInt32(connectionDetails.SshForwarding));
        }

        // Questa è quella che dovrà fare il device
        public void EnableRemoteForwarding(DeviceConnectionStatus connectionDetails)
        {
            CurrentForwardingPort = connectionDetails.SshForwarding;
            CreateRemoteForwarding("127.0.0.1", Convert.ToUInt32(connectionDetails.SshForwarding), "127.0.0.1", 22);
        }

        public void LaunchSshCommand(string command)
        {
            LaunchCommand(command);
        }

        public void CloseSshConnection(string reason)
        {
            Console.WriteLine($"Connection Closed [NOT_IMPL]: {reason}");
        }


        private void Connect(SshConnectionData connectionData)
        {
            ConnectionInfo connectionInfo = null;
            if (connectionData.AuthenticationMode == EnumSshAuthMode.WithCertificates)
            {

                WritePrivateKeyToFile(connectionData.PrivateKey);

                

                connectionInfo = new ConnectionInfo(connectionData.Host, connectionData.Port, connectionData.Username,
                new AuthenticationMethod[]{

                // Key Based Authentication (using keys in OpenSSH Format)
                new PrivateKeyAuthenticationMethod(connectionData.Username,new PrivateKeyFile[]{
                    new PrivateKeyFile(@".\private.key","")

                }),
                }
            );

            // Password based authentication
            }else if (connectionData.AuthenticationMode == EnumSshAuthMode.WithPassword)
            {
                connectionInfo = new ConnectionInfo(connectionData.Host, connectionData.Port, connectionData.Username,
                new AuthenticationMethod[]{
               
                new PasswordAuthenticationMethod(connectionData.Username,connectionData.Password),
                });
            }


            this.Connection = new SshClient(connectionInfo);
            this.Connection.Connect();
        }
        private void WritePrivateKeyToFile(string privateKey)
        {
            using (StreamWriter sw = new StreamWriter(@".\private.key"))
            {
                sw.Write(privateKey);
            }
        }


        private void CreateLocalForwarding(string remoteHost, uint remotePort, string localHost, uint localPort)
        {
            try
            {
                // forwarding locale: dalla 22 del server ssh alla 9091 del pc client
                // var port = new ForwardedPortLocal("127.0.0.1", 9091, "127.0.0.1", 22);
                var port = new ForwardedPortLocal(remoteHost, remotePort, localHost, localPort);
                this.Connection.AddForwardedPort(port);
                port.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void CreateRemoteForwarding(string remoteHost, uint remotePort, string localHost, uint localPort)
        {
            try
            {
                // esempio di forwarding remoto: dalla 5001 del pc client alla 50000 del server ssh
                //var port = new ForwardedPortRemote("127.0.0.1", 50000, "127.0.0.1", 5001);
                var port = new ForwardedPortRemote(remoteHost, remotePort, localHost, localPort);
                this.Connection.AddForwardedPort(port);
                port.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void LaunchCommand(string command)
        {
            try
            {
                using (var cmd = Connection.CreateCommand(command))
                {
                    cmd.Execute();
                    Console.WriteLine("Command>" + cmd.CommandText);
                    Console.WriteLine("Return Value = {0}", cmd.ExitStatus);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
