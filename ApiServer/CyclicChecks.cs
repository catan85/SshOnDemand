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
        readonly AppSettings settings;

        public CyclicChecks(AppSettings settings)
        {
            System.Timers.Timer t = new System.Timers.Timer(1000);
            t.AutoReset = true;
            t.Elapsed += T_Elapsed;
            t.Start();

            this.settings = settings;
        }

        private void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Running cyclic checks");

            CheckActiveDeveloperRequests();

            CheckActiveDeviceConnections();
        }


        private void CheckActiveDeveloperRequests()
        {
            List<string> deactivatedClientNames = new List<string>();
            using (sshondemandContext dbContext = new sshondemandContext())
            {
                Queries q = new Queries(dbContext);
                q.DeactivateOldRequests( 15, out deactivatedClientNames);
            }

            Ssh ssh = new Ssh(settings);
            ssh.UnloadClientKeys(deactivatedClientNames);
        }

        private void CheckActiveDeviceConnections()
        {
            List<string> deactivatedClients = new List<string>();
            using (sshondemandContext dbContext = new sshondemandContext())
            {
                Queries q = new Queries(dbContext);
                q.ResetOldConnections(15, out deactivatedClients);
            }
            Ssh ssh = new Ssh(settings);
            ssh.UnloadClientKeys(deactivatedClients);
        }
    }
}
