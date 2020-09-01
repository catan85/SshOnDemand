using System;

namespace DeveloperClient
{
    class Program
    {
 
        static void Main(string[] args)
        {
            Console.WriteLine("Starting developer application");
            MainWorker worker = new MainWorker();
            worker.Run().Wait();
        }
    }
}
