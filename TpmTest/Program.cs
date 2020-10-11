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


                    // Salvataggio della chiave in un algoritmo HMAC nel TPM
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
        private static string CalculateHmacString(string signature, string secret)
        {

            byte[] signatureByteArray = Encoding.UTF8.GetBytes(signature);
            byte[] secretByteArray = Convert.FromBase64String(secret);

            // Crea l’oggetto hmacAlgorithm dando in ingresso la chiave privata condivisa
            HMACSHA256 hmacAlgorithm = new HMACSHA256(secretByteArray);
            // Esegue il calcolo dell’hash cifrato
            byte[] hmacByteArray = hmacAlgorithm.ComputeHash(signatureByteArray);
            // converte in stringa il valore calcolato e lo torna
            return Convert.ToBase64String(hmacByteArray);
        }

        private static string CalculateHmacStringTpm(string signature)
        {
            byte[] signatureByteArray = Encoding.UTF8.GetBytes(signature);

            byte[] hmacByteArray = TpmHelper.SignHmac(signatureByteArray);
            return Convert.ToBase64String(hmacByteArray);
        }




    }
}
