using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            PostgreSQLClass.DeactivateOldRequests(15, out fault);
        }

        private void CheckActiveDeviceConnections()
        {
            bool fault = false;
            PostgreSQLClass.ResetOldConnections(15, out fault);
        }
    }
}
