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

        public static void SaveKeys(SshConnectionData sftpConnectionData, string sshUsername, string clientName, string publicKey, string authorizedKeyPath)
        {
            // Inserire quì la connessione ssh e salvataggio della chiave pubblica sul server
            // Lo faccio tramite SSH, in questo modo è possibile tenere webserver e server ssh anche separati
            SftpHelper sftp = new SftpHelper();

            // download authorized_keys
            bool fileExists = false;
            sftp.DownloadFile(sftpConnectionData, authorizedKeyPath, Constants.TEMP_AUTHORIZED_KEYS_FILENAME , out fileExists);

            // aggiunta della nuova chiave ad authorized_keys
            AddKeyToAuthorized(publicKey, clientName);

            // upload di authorized_keys
            sftp.UploadFile(sftpConnectionData, Constants.TEMP_AUTHORIZED_KEYS_FILENAME, authorizedKeyPath);
            
        }


        private static void AddKeyToAuthorized(string publicKey, string clientName)
        {
            using (StreamWriter sw = new StreamWriter(Constants.TEMP_AUTHORIZED_KEYS_FILENAME))
            {
                sw.Write(publicKey + " " + clientName);
            }
        }

        public static void UnloadKeys(string clientName) {
           #warning da implementare

            // posso utilizzare il commendo in fondo alle chiavi pubbliche per riconoscere i client autorizzati

            //dowload authorized_keys

            // ciclo sulle chiavi per trovare quella da rimuovere

            // salvo authorized_keys in locale

            // upload di authorized keys modificato
        }

    }
}
