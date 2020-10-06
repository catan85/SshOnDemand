using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using SshOnDemandLibs.Entities;
using SshOnDemandLibs.Ssh;

namespace SshOnDemandLibs
{
    public class SshKeysManagement
    {
        private static string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
               Environment.OSVersion.Platform == PlatformID.MacOSX)
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

        private static string operativeSystem = (Environment.OSVersion.Platform == PlatformID.Unix ||
               Environment.OSVersion.Platform == PlatformID.MacOSX) ? "Linux" : "Windows";

        public static SshKeys GenerateKeys()
        {
            SshKeys keys = new SshKeys();

            var returnValue = ShellHelper.Bash($"ssh-keygen -t ecdsa -q -f \"{homePath}/.ssh/temp\" -N \"\"",operativeSystem);
            keys.PrivateKey = File.ReadAllText($"{homePath}/.ssh/temp");
            keys.PublicKey = File.ReadAllText($"{homePath}/.ssh/temp.pub");

            // Deleting temporary files
            File.Delete($"{homePath}/.ssh/temp");
            File.Delete($"{homePath}/.ssh/temp.pub");

            return keys;
        }

        private static object locker = new object();

        public static void SaveKeys(SshConnectionData sftpConnectionData, string sshUsername, string clientName, string publicKey, string authorizedKeyPath,int? forwardingPort)
        {
            lock(locker)
            {
                // Inserire quì la connessione ssh e salvataggio della chiave pubblica sul server
                // Lo faccio tramite SSH, in questo modo è possibile tenere webserver e server ssh anche separati
                SftpHelper sftp = new SftpHelper();

                // download authorized_keys
                sftp.DownloadFile(sftpConnectionData, authorizedKeyPath, Constants.TEMP_AUTHORIZED_KEYS_FILENAME);

                #warning la verifica della presenza funziona, ma non è un metodo efficiente. forse andrebbe fatto a database prima di scaricare il file delle auth
                // check if authorized keys already contains the client key
                bool clientAlreadyEnabled = IsClientAlreadyEnabled(publicKey, clientName);

                // aggiunta della nuova chiave ad authorized_keys
                if (!clientAlreadyEnabled)
                { 
                    AddKeyToAuthorized(publicKey, clientName, forwardingPort);
                }
            
                // upload di authorized_keys
                sftp.UploadFile(sftpConnectionData, Constants.TEMP_AUTHORIZED_KEYS_FILENAME, authorizedKeyPath);
            }
        }

        private static bool IsClientAlreadyEnabled(string publicKey, string clientName)
        {
            var allKeys = File.ReadAllText(Constants.TEMP_AUTHORIZED_KEYS_FILENAME);
            return allKeys.Contains(publicKey.Remove(publicKey.Length - 2, 2) + " " + clientName);
        }


        private static void AddKeyToAuthorized(string publicKey, string clientName, int? forwardingPort)
        {
            using (StreamWriter sw = new StreamWriter(Constants.TEMP_AUTHORIZED_KEYS_FILENAME, append:true))
            {

                string enablePortForwardingPrefix = "";
                if (forwardingPort.HasValue)
                    enablePortForwardingPrefix = $"command=\"echo 'Port forwarding only account.'\",port-forwarding,permitopen=\"127.0.0.1:{forwardingPort}\" ";

                string publicKeyString = publicKey.Remove(publicKey.Length - 2, 2);
                sw.WriteLine($"{enablePortForwardingPrefix}{publicKeyString} {clientName}");
            }
        }

        public static void UnloadKey(SshConnectionData sftpConnectionData, string clientName, string authorizedKeyPath)
        {
            lock (locker)
            {
                // Inserire quì la connessione ssh e salvataggio della chiave pubblica sul server
                // Lo faccio tramite SSH, in questo modo è possibile tenere webserver e server ssh anche separati
                SftpHelper sftp = new SftpHelper();

                // download authorized_keys
                sftp.DownloadFile(sftpConnectionData, authorizedKeyPath, Constants.TEMP_AUTHORIZED_KEYS_FILENAME);

                // check if authorized keys already contains the client key
                bool clientCurrentlyLoaded = IsClientCurrentlyLoaded(clientName);

                // aggiunta della nuova chiave ad authorized_keys
                if (clientCurrentlyLoaded)
                {
                    Console.WriteLine($"Unloading {clientName} key.");

                    RemoveAuthorizedKey(clientName);

                    // upload di authorized_keys
                    sftp.UploadFile(sftpConnectionData, Constants.TEMP_AUTHORIZED_KEYS_FILENAME, authorizedKeyPath);
                }
            }
        }

        private static bool IsClientCurrentlyLoaded(string clientName)
        {
            var allKeys = File.ReadAllText(Constants.TEMP_AUTHORIZED_KEYS_FILENAME);
            return allKeys.Contains(clientName);
        }

        private static void RemoveAuthorizedKey(string clientName)
        {
            string[] keys = File.ReadAllLines(Constants.TEMP_AUTHORIZED_KEYS_FILENAME);
            using (StreamWriter sw = new StreamWriter(Constants.TEMP_AUTHORIZED_KEYS_FILENAME, false))
            {
                foreach (string key in keys)
                {
                    if (!key.Contains(clientName))
                    {
                        sw.WriteLine(key);
                    }
                }
            }

        }
    }
}
