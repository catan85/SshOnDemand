using System;
using System.Security.Cryptography;
using System.Text;
using SshOnDemandLibs;


namespace TpmTest
{
    class Program
    {
        

        static void Main(string[] args)
        {
            string secret = "";
            string stringToBeCyphered = "LaMiaStringaDaCifrare";
            while (true)
            {
                Console.WriteLine("Premere: \nW per scrivere il segreto,\nR per leggere il segreto da TPM,\nH per calcolare HMAC,\nT per calcolare HMAC con TPM\n");
                var operation = Console.ReadKey(true);

                if (operation.Key == ConsoleKey.W)
                {
                    Console.WriteLine("Inserire il segreto da memorizzare:");

                    secret = Console.ReadLine();

                    Console.WriteLine($"Salvataggio stringa: {secret}");

                    byte[] b = Encoding.ASCII.GetBytes(secret);

                    // Salvataggio della chiave come valore nel TPM
                    // TpmHelper.SaveValueIntoTpm(2500, b, b.Length);

                 
                    TpmHelper.SaveHmacKey(secret);

                }

                if (operation.Key == ConsoleKey.R)
                {
                    byte[] c = TpmHelper.ReadValueFromTpm(2500, 150);
                    secret = Encoding.ASCII.GetString(c);
                    secret = secret.Trim('\0');
                    Console.WriteLine("La stringa memorizzata nel TPM è:");
                    Console.WriteLine(secret);
                }

                if (operation.Key == ConsoleKey.H)
                {
                    secret = "HeeFfpsSelN8c5zIJuZn7mQ28MBGIAoqp8Nf94N2eGM=";
                    string crypted = CalculateHmacString(stringToBeCyphered, secret);
                    Console.WriteLine("La stringa cifrata con System.Security.Cryptography è:");
                    Console.WriteLine(crypted);
                }

                if (operation.Key == ConsoleKey.T)
                {
                    string crypted = CalculateHmacStringTpm(stringToBeCyphered);
                    Console.WriteLine("La stringa cifrata con Tpm è:");
                    Console.WriteLine(crypted);
                }
            }
        }



        // Calcolatore HMAC con using System.Security.Cryptography;
        private static string CalculateHmacString(string value, string secret)
        {

            byte[] valueByteArray = Encoding.UTF8.GetBytes(value);
            byte[] secretByteArray = Convert.FromBase64String(secret);

            // Crea l’oggetto hmacAlgorithm dando in ingresso la chiave privata condivisa
            HMACSHA256 hmacAlgorithm = new HMACSHA256(secretByteArray);
            // Esegue il calcolo dell’hash cifrato
            byte[] hmacByteArray = hmacAlgorithm.ComputeHash(valueByteArray);
            // converte in stringa il valore calcolato e lo torna
            return Convert.ToBase64String(hmacByteArray);
        }

        private static string CalculateHmacStringTpm(string value)
        {
            byte[] valueByteArray = Encoding.UTF8.GetBytes(value);

            byte[] hmacByteArray = TpmHelper.CalculateHmac(valueByteArray);
            return Convert.ToBase64String(hmacByteArray);
        }




    }
}
