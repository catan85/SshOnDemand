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
        Logger logger = null;
        bool enableTpm = false;

        public HMACResponseAuthentication(bool enableDebug, bool enableTpmSigning)
        {
            logger = new Logger(enableDebug);
            enableTpm = enableTpmSigning;
        }

        private readonly UInt64 requestMaxAgeInSeconds = 300; //Means 5 min
        public bool IsResponseAuthenticated(HttpResponseMessage response)
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
                        logger.Output("Invalid response");
                    }
                    else
                    {
                        logger.Debug("Valid response");
                        authenticated = true;
                    }

                }
                else
                {
                    logger.Output("Invalid response");
                }
            }

            return authenticated;
        }

        private async Task<bool> IsValidResponse(HttpResponseMessage response, string returnedAPPId, string incomingBase64Signature, string nonce, string requestTimeStamp)
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
                logger.Debug("Full response content: " + contentString);
                MD5 md5 = MD5.Create();
                byte[] responseContentHash = md5.ComputeHash(content);
                responseContentBase64String = Convert.ToBase64String(responseContentHash);
                logger.Debug("Hashed response: " + responseContentBase64String);
            }

            var requestHttpMethod = response.RequestMessage.Method;

            string requestUri = HttpUtility.UrlEncode(response.RequestMessage.RequestUri.AbsoluteUri.ToLower());

            string digestString = String.Format("{0}{1}{2}{3}{4}{5}", returnedAPPId, requestHttpMethod, requestUri, requestTimeStamp, nonce, responseContentBase64String);
            
            byte[] digestBytes = Encoding.UTF8.GetBytes(digestString);

            byte[] signatureBytes = SignHmac(digestBytes,sharedKey);

            var signatureString = Convert.ToBase64String(signatureBytes);
            return (incomingBase64Signature.Equals(signatureString, StringComparison.Ordinal));
        }

        private byte[] SignHmac(byte[] digestBytes, string sharedKey)
        {
            if (enableTpm)
            {
                return TpmHelper.SignHmac(digestBytes);
            }
            else
            {
                var secretKeyBytes = Convert.FromBase64String(sharedKey);
                using (HMACSHA256 hmac = new HMACSHA256(secretKeyBytes))
                {
                    return hmac.ComputeHash(digestBytes);
                }
            }
        }

        private bool isReplayRequest(HttpResponseMessage response, string APPId, string incomingBase64Signature, string nonce, string responseTimestamp)
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
