using System;
using SshOnDemandLibs;
namespace DeviceProvisioning
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();
            Console.WriteLine("          Device provisioning");
            Console.WriteLine();
            Console.WriteLine("----------------------------------------");

            Console.WriteLine();
            Console.WriteLine("Note: Run this software with administrative rights.");
            Console.WriteLine();
            Console.WriteLine("Please insert the device Shared Key and press ENTER");


            string sharedKey = Console.ReadLine();

            try
            {
                TpmHelper.SaveHmacKey(sharedKey);
                Console.WriteLine();

                Console.WriteLine("Shared key written succesfully.");

            }
            catch(Exception ex)
            {
                Console.WriteLine();

                Console.WriteLine($"Shared key saving throws an exception: {ex.Message}");
            }

            Console.WriteLine("Press any key to quit.");
            Console.ReadKey();



        }
    }
}
