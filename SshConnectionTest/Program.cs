using Renci.SshNet;
using System;

namespace SshConnectionTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, this is a test SSH connection");

            // Setup Credentials and Server Information
            ConnectionInfo ConnNfo = new ConnectionInfo("192.168.0.106", 22, "pi",
                new AuthenticationMethod[]{

                // Pasword based Authentication
                // new PasswordAuthenticationMethod("pi","catanf85"),

                // Key Based Authentication (using keys in OpenSSH Format)
                new PrivateKeyAuthenticationMethod("pi",new PrivateKeyFile[]{
                    new PrivateKeyFile(@"..\test.keys","")
                }),
                }
            );

            // Execute a (SHELL) Command - prepare upload directory
            using (var sshclient = new SshClient(ConnNfo))
            {
                sshclient.KeepAliveInterval = new TimeSpan(0, 0, 10);
                sshclient.Connect();
                using (var cmd = sshclient.CreateCommand("mkdir -p /tmp/uploadtest && chmod +rw /tmp/uploadtest"))
                {
                    cmd.Execute();
                    Console.WriteLine("Command>" + cmd.CommandText);
                    Console.WriteLine("Return Value = {0}", cmd.ExitStatus);
                }



                if (sshclient.IsConnected)
                {
                   
                    try
                    {
                        // esempio di forwarding locale: dalla 22 del server ssh alla 9091 del pc client
                        var port = new ForwardedPortLocal("127.0.0.1",9091,"127.0.0.1", 22);
                        sshclient.AddForwardedPort(port);
                        port.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }


                    try
                    {
                        // esempio di forwarding remoto: dalla 5001 del pc client alla 50000 del server ssh
                        var port = new ForwardedPortRemote( "127.0.0.1", 50000, "127.0.0.1", 5001);
                        sshclient.AddForwardedPort(port);
                        port.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                }

                Console.ReadLine();
                sshclient.Disconnect();
            }




        }
    }
}
