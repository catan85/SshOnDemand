using System;

namespace DeviceClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting device application");
            MainWorker worker = new MainWorker();
            worker.Run().Wait();
        }
    }
}
