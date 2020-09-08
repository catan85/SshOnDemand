using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using SshOnDemandLibs.Entities;

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

        public static void SaveKeys(string clientName, string publicKey, string publicKeyFolder)
        {
            // Inserire quì la connessione ssh e salvataggio della chiave pubblica sul server
            // Lo faccio tramite SSH, in questo modo è possibile tenere webserver e server ssh anche separati

            #warning da implementare

            // download authorized_keys

            // modifica di authorized_keys

            // upload di authorized_keys

            // aggiungi anche il nome del client in fondo alle public key, serve da commento ma anche per poi fare l'unload


            using (StreamWriter sw = new StreamWriter("temp.pub"))
            {
                sw.Write(publicKey);
            }

            string copyCmd = "";
            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                copyCmd = $"cp \"temp.pub\" \"{publicKeyFolder}\"/{clientName}.pub";
            }
            else
            {
                copyCmd = $"copy \"temp.pub\" \"{publicKeyFolder}\"\\{clientName}.pub";
            }
            var r = ShellHelper.Bash(copyCmd, operativeSystem);
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
