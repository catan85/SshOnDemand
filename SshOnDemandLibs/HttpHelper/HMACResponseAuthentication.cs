using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SshOnDemandLibs
{
    public class HMACResponseAuthentication
    {
        private static readonly UInt64 requestMaxAgeInSeconds = 300; //Means 5 min
        public static bool IsResponseAuthenticated(HttpResponseMessage response)
        {
            bool authenticated = false;

            response.Headers.TryGetValues("Authorization", out IEnumerable<string> authString);


            if (authString != null && authString.Count() > 0 && authString.First().ToString().StartsWith("hmacauth "))
            {
                var authHeader = authString.First().ToString().Replace("hmacauth ", "");

                var authArray = authHeader.Split(':');

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


            if (returnedAPPId != HMACDelegatingHandler.ClientId)
            {
                return false;
            }

            var sharedKey = HMACDelegatingHandler.ClientKey;

            if (isReplayRequest(response, returnedAPPId, incomingBase64Signature, nonce, requestTimeStamp))
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
