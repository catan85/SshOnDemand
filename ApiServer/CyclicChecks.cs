using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SshOnDemandLibs;
using SshOnDemandLibs.Entities;

namespace ApiServer
{
    public class CyclicChecks
    {
        public CyclicChecks()
        {
            System.Timers.Timer t = new System.Timers.Timer(1000);
            t.AutoReset = true;
            t.Elapsed += T_Elapsed;
            t.Start();
        }

        private void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Running cyclic checks");

            CheckActiveDeveloperRequests();

            CheckActiveDeviceConnections();
        }


        private void CheckActiveDeveloperRequests()
        {
            bool fault = false;
            List<string> deactivatedClients = new List<string>();
            PostgreSQLClass.DeactivateOldRequests(15, out fault, out deactivatedClients);

            SshConnectionData connectionData = Utilities.CreateSshConnectionData();

            foreach (string deactivatedClient in deactivatedClients)
            {
                SshKeysManagement.UnloadKey(connectionData, deactivatedClient, AppSettings.SshAuthorizedKeysPath);
            }
        }

        private void CheckActiveDeviceConnections()
        {
            bool fault = false;
            PostgreSQLClass.ResetOldConnections(15, out fault);
        }
    }
}
