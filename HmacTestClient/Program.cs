using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SshOnDemandLibs;

namespace HmacTestClient
{
    class Program
    {

        private static readonly UInt64 requestMaxAgeInSeconds = 300; //Means 5 min
        private static string apiBaseAddress = "https://localhost:5001/";

        static void Main(string[] args)
        {
            RunAsync().Wait();
            Console.ReadLine();
        }

        static async Task RunAsync()
        {
            Console.WriteLine("Calling the back-end API");

            HMACDelegatingHandler customDelegatingHandler = new HMACDelegatingHandler();

            HttpClient client = HttpClientFactory.Create(customDelegatingHandler);

            ConsoleKey key = ConsoleKey.S;
            HttpResponseMessage response = null;

            while (true)
            {
                // azioni developer usano un'api key developer
                if (key == ConsoleKey.S || key == ConsoleKey.P || key == ConsoleKey.O || key == ConsoleKey.I)
                {
                    HMACDelegatingHandler.APPId = "378ce77c-5b45-4126-9dfa-0371daa51563";
                    HMACDelegatingHandler.APIKey = "anI4ICTj9bs+gNQRa3aBbbQmsYCGvNIKB1qTkWZoj/k=";
                }

                if (key == ConsoleKey.Q || key == ConsoleKey.W || key == ConsoleKey.E)
                {
                    // azioni del device usano un'api key device
                    HMACDelegatingHandler.APPId = "50148590-1b48-4cf5-a76d-8a7f9474a3de";
                    HMACDelegatingHandler.APIKey = "U8a2xaaYz2sNhEGDO9T4Ms9Wf4AWMQv+gDpmYJx+YmI=";
                }


                if (key == ConsoleKey.S)
                {
                    response = await TestProtocol(client);
                }

                else if (key == ConsoleKey.P)
                {
                    response = await DeveloperConnectionRequest_FAIL(client);
                }

                else if (key == ConsoleKey.O)
                {
                    response = await DeveloperConnectionRequest_DONE(client);
                }

                else if (key == ConsoleKey.I)
                {
                    response = await DeveloperCheckDeviceConnectionState(client);
                }

                else if (key == ConsoleKey.Q)
                {
                    response = await DeviceCheckConnectionRequest(client);
                }

                else if (key == ConsoleKey.W)
                {
                    response = await DeviceSetActiveConnectionStatus(client);
                }

                if (response.IsSuccessStatusCode)
                {
                    bool authenticated = CheckResponseAuthentication(response);
                    if (authenticated)
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseString);
                    }
                }
                else
                {
                    Console.WriteLine("Failed to call the API. HTTP Status: {0}, Reason {1}", response.StatusCode, response.ReasonPhrase);
                    string responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseString);
                }

                key = Console.ReadKey(true).Key;
                Console.WriteLine("-----------------------------------------------------------");

            }

        }

        private static async Task<HttpResponseMessage> TestProtocol(HttpClient client)
        {
            Console.WriteLine("Test della chiamata a get, metodo secret, è un metodo di test per lo sviluppo del meccanismo");
            return await client.GetAsync(apiBaseAddress + "secret");
        }

        private static async Task<HttpResponseMessage> DeveloperConnectionRequest_FAIL(HttpClient client)
        {
            Console.WriteLine("Test di una richiesta di connessione da parte dello sviluppatore, NON VALIDA (device name inesistente)");
            DeveloperDeviceConnectionRequestArgs args = new DeveloperDeviceConnectionRequestArgs();
            args.DeveloperSshPublicKey = "abcde";
            args.DeviceName = "blabla";

            return await client.PostAsJsonAsync(apiBaseAddress + "DeveloperDeviceConnectionRequest", args);
        }
        private static async Task<HttpResponseMessage> DeveloperConnectionRequest_DONE(HttpClient client)
        {
            Console.WriteLine("Test di inserimento di una richiesta di connessione da parte dello sviluppatore, VALIDA");
            DeveloperDeviceConnectionRequestArgs args = new DeveloperDeviceConnectionRequestArgs();
            args.DeveloperSshPublicKey = "abcde";
            args.DeviceName = "50148590-1b48-4cf5-a76d-8a7f9474a3de";
            return await client.PostAsJsonAsync(apiBaseAddress + "DeveloperDeviceConnectionRequest", args);
        }

        private static async Task<HttpResponseMessage> DeveloperCheckDeviceConnectionState(HttpClient client)
        {
            Console.WriteLine("Test di lettura stato connessione del device da parte dello sviluppatore, VALIDA");
            string deviceName = "50148590-1b48-4cf5-a76d-8a7f9474a3de";

            return await client.PostAsJsonAsync(apiBaseAddress + "DeveloperCheckDeviceConnection", deviceName);
        }

        private static async Task<HttpResponseMessage> DeviceCheckConnectionRequest(HttpClient client)
        {
            Console.WriteLine("Test di verifica richieste di connessione da parte del device, VALIDA");
            return await client.PostAsJsonAsync(apiBaseAddress + "DeviceCheckRemoteConnectionRequest", "dev pub ssh key");
        }

        private static async Task<HttpResponseMessage> DeviceSetActiveConnectionStatus(HttpClient client)
        {
            Console.WriteLine("Imposta stato di connessione del client ATTIVO");
            return await client.PostAsJsonAsync(apiBaseAddress + "DeviceSetConnectionState", ClientConnectionState.Connected);
        }

        private static bool CheckResponseAuthentication(HttpResponseMessage response)
        {
            bool authenticated = false;

            response.Headers.TryGetValues("Authorization", out IEnumerable<string> authString);


            if (authString != null && authString.Count() > 0 && authString.First().ToString().StartsWith("hmacauth "))
            {
                var authHeader = authString.First().ToString().Replace("hmacauth ", "");

                var authArray = authHeader.Split(":");

                if (authArray.Length == 4)
                {
                    var APPId = authArray[0];
                    var incomingBase64Signature = authArray[1];
                    var nonce = authArray[2];
                    var requestTimeStamp = authArray[3];

                    var isValid = IsValidResponse(response, APPId, incomingBase64Signature, nonce, requestTimeStamp);

                    if (isValid.Result == false)
                    {
                        Console.WriteLine("Invalid response");
                    }
                    else
                    {
                        Console.WriteLine("Valid response");
                        authenticated = true;
                    }

                }
                else
                {
                    Console.WriteLine("Invalid response");
                }
            }

            return authenticated;
        }

        private static async Task<bool> IsValidResponse(HttpResponseMessage response, string returnedAPPId, string incomingBase64Signature, string nonce, string requestTimeStamp)
        {
            string responseContentBase64String = "";


            if (returnedAPPId != HMACDelegatingHandler.APPId)
            {
                return false;
            }

            var sharedKey = HMACDelegatingHandler.APIKey;

            if (isReplayRequest(response,returnedAPPId, incomingBase64Signature, nonce, requestTimeStamp))
            {
                return false;
            }


            if (response.Content != null)
            {
                // Hashing the request body, so any change in request body will result a different hash
                // we will achieve message integrity
                byte[] content = await response.Content.ReadAsByteArrayAsync();
                
                string contentString = Encoding.UTF8.GetString(content, 0, content.Length);
                Console.WriteLine("Full response content: " + contentString);
                MD5 md5 = MD5.Create();
                byte[] responseContentHash = md5.ComputeHash(content);
                responseContentBase64String = Convert.ToBase64String(responseContentHash);
                Console.WriteLine("Hashed response: " + responseContentBase64String);
            }

            var requestHttpMethod = response.RequestMessage.Method;

            string requestUri = HttpUtility.UrlEncode(response.RequestMessage.RequestUri.AbsoluteUri.ToLower());

            string signatureRawData = String.Format("{0}{1}{2}{3}{4}{5}", returnedAPPId, requestHttpMethod, requestUri, requestTimeStamp, nonce, responseContentBase64String);
            var secretKeyBytes = Convert.FromBase64String(sharedKey);
            byte[] signature = Encoding.UTF8.GetBytes(signatureRawData);
            using (HMACSHA256 hmac = new HMACSHA256(secretKeyBytes))
            {
                byte[] signatureBytes = hmac.ComputeHash(signature);

                var calculatedSignature = Convert.ToBase64String(signatureBytes);

                return (incomingBase64Signature.Equals(calculatedSignature, StringComparison.Ordinal));
            }

        }

        private static bool isReplayRequest(HttpResponseMessage response, string APPId, string incomingBase64Signature, string nonce, string responseTimestamp)
        {
            if (System.Runtime.Caching.MemoryCache.Default.Contains(nonce))
            {
                return true;
            }
            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan currentTs = DateTime.UtcNow - epochStart;
            var serverTotalSeconds = Convert.ToUInt64(currentTs.TotalSeconds);
            var requestTotalSeconds = Convert.ToUInt64(responseTimestamp);
            if ((serverTotalSeconds - requestTotalSeconds) > requestMaxAgeInSeconds)
            {
                return true;
            }
            System.Runtime.Caching.MemoryCache.Default.Add(nonce, responseTimestamp, DateTimeOffset.UtcNow.AddSeconds(requestMaxAgeInSeconds));
            return false;
        }
    }
}