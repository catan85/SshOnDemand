using System;

namespace DeviceClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Starting device application");
            MainWorker worker = new MainWorker();
            worker.Run().Wait();
        }
    }
}
