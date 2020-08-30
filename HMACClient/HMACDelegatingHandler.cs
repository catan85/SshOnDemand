using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
namespace HMACClient
{
    public class HMACDelegatingHandler : DelegatingHandler
    {
        // First obtained the APP ID and API Key from the server
        // The APIKey MUST be stored securely in db or in the App.Config
        public static string APPId = "378ce77c-5b45-4126-9dfa-0371daa51563";
        public static string APIKey = "anI4ICTj9bs+gNQRa3aBbbQmsYCGvNIKB1qTkWZoj/k=";

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            string requestContentBase64String = string.Empty;

            //Get the Request URI
            string requestUri = HttpUtility.UrlEncode(request.RequestUri.AbsoluteUri.ToLower());


            //Get the Request HTTP Method type
            string requestHttpMethod = request.Method.Method;

            //Calculate UNIX time
            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = DateTime.UtcNow - epochStart;
            string requestTimeStamp = Convert.ToUInt64(timeSpan.TotalSeconds).ToString();

            //Create the random nonce for each request
            string nonce = Guid.NewGuid().ToString("N");

            //Checking if the request contains body, usually will be null wiht HTTP GET and DELETE

            if (request.Content != null)
            {
                // Hashing the request body, so any change in request body will result a different hash
                // we will achieve message integrity
                byte[] content = await request.Content.ReadAsByteArrayAsync();
                MD5 md5 = MD5.Create();
                byte[] requestContentHash = md5.ComputeHash(content);

                requestContentBase64String = Convert.ToBase64String(requestContentHash);
            }

            //Creating the raw signature string by combinging
            //APPId, request Http Method, request Uri, request TimeStamp, nonce, request Content Base64 String
            string signatureRawData = String.Format("{0}{1}{2}{3}{4}{5}", APPId, requestHttpMethod, requestUri, requestTimeStamp, nonce, requestContentBase64String);

            //Converting the APIKey into byte array
            var secretKeyByteArray = Convert.FromBase64String(APIKey);

            //Converting the signatureRawData into byte array
            byte[] signature = Encoding.UTF8.GetBytes(signatureRawData);

            //Generate the hmac signature and set it in the Authorization header
            using (HMACSHA256 hmac = new HMACSHA256(secretKeyByteArray))
            {
                byte[] signatureBytes = hmac.ComputeHash(signature);
                string requestSignatureBase64String = Convert.ToBase64String(signatureBytes);

                //Setting the values in the Authorization header using custom scheme (hmacauth)
                request.Headers.Authorization = new AuthenticationHeaderValue("hmacauth", string.Format("{0}:{1}:{2}:{3}", APPId, requestSignatureBase64String, nonce, requestTimeStamp));
                Console.WriteLine(request.Content);
            }

            response = await base.SendAsync(request, cancellationToken);
            return response;
        }
    }

}