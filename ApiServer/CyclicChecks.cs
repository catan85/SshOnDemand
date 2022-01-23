using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiServer.Infrastructure;
using ApiServer.Infrastructure.Models;
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
            #warning VIOLAZIONE DEPENDENCY INVERSION
            using (sshondemandContext dbContext = new sshondemandContext())
            {
                #warning VIOLAZIONE DEPENDENCY INVERSION
                Queries q = new Queries();
                q.DeactivateOldRequests(dbContext, 15, out deactivatedClients);
            }
            
            SshConnectionData connectionData = Utilities.CreateSshConnectionData();

            foreach (string deactivatedClient in deactivatedClients)
            {
                SshKeysManagement.UnloadKey(connectionData, deactivatedClient, AppSettings.SshAuthorizedKeysPath);
            }
        }

        private void CheckActiveDeviceConnections()
        {
            List<string> deactivatedClients = new List<string>();
            using (sshondemandContext dbContext = new sshondemandContext())
            {
                #warning VIOLAZIONE DEPENDENCY INVERSION
                Queries q = new Queries();
                q.ResetOldConnections(dbContext, 15, out deactivatedClients);
            }

            SshConnectionData connectionData = Utilities.CreateSshConnectionData();

            foreach (string deactivatedClient in deactivatedClients)
            {
                SshKeysManagement.UnloadKey(connectionData, deactivatedClient, AppSettings.SshAuthorizedKeysPath);
            }
        }
    }
}
