using System;
using System.Security.Cryptography;

namespace GeneratorAppIdAppKey
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                using (var cryptoProvider = new RNGCryptoServiceProvider())
                {
                    var APPID = Guid.NewGuid();
                    byte[] secretKeyByteArray = new byte[32]; //256 bit
                    cryptoProvider.GetBytes(secretKeyByteArray);
                    var APIKey = Convert.ToBase64String(secretKeyByteArray);

                    Console.WriteLine("AppId : " + APPID);
                    Console.WriteLine("AppKey: " + APIKey);
                }
                System.Console.ReadLine();
            }

        }
    }
}
