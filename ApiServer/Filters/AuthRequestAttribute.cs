using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
namespace ApiServer.Filters
{

    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthRequestAttribute : Attribute, IAsyncActionFilter
    {
        private static Dictionary<string, string> allowedApps = new Dictionary<string, string>();
        private readonly UInt64 requestMaxAgeInSeconds = 300; //Means 5 min
        private readonly string authenticationScheme = "hmacauth";
        
        public AuthRequestAttribute()
        {
            if (allowedApps.Count == 0)
            {
                bool fault = false;
                DataTable clientsTable = PostgreSQLClass.GetClientsDatatable(out fault);

                foreach (DataRow row in clientsTable.Rows)
                {
                    allowedApps.Add((string)row["client_name"], (string)row["client_key"]);
                }
            }
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {


            context.HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues authString);

            if (!StringValues.IsNullOrEmpty(authString) && authString.ToString().StartsWith("hmacauth "))
            {
                var authHeader = authString.ToString().Replace("hmacauth ", "");

                var authArray = authHeader.Split(":");

                if (authArray.Length == 4)
                {
                    var APPId = authArray[0];
                    var incomingBase64Signature = authArray[1];
                    var nonce = authArray[2];
                    var requestTimeStamp = authArray[3];

                    var isValid = IsValidRequest(context.HttpContext.Request, APPId, incomingBase64Signature, nonce, requestTimeStamp);

                    // Passaggio del client name al controller per conoscere l'identità del device
                    context.HttpContext.Items.Add("ClientName", APPId );

                    if (isValid.Result == false)
                    {
                        context.Result = new UnauthorizedResult();
                        return;
                    }

                }
                else
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }
            }
            else
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }

        private async Task<bool> IsValidRequest(HttpRequest req, string APPId, string incomingBase64Signature, string nonce, string requestTimeStamp)
        {
            string requestContentBase64String = "";
           
            string requestUri = UriHelper.GetEncodedUrl(req);
            requestUri = HttpUtility.UrlEncode(requestUri.ToLower());

            string requestHttpMethod = req.Method;
            if (!allowedApps.ContainsKey(APPId))
            {
                return false;
            }

            var sharedKey = allowedApps[APPId];
            if (isReplayRequest(nonce, requestTimeStamp))
            {
                return false;
            }

            string requestBody = ReadBodyAsString(req.HttpContext.Request);
            byte[] reqBodyByteArray = Encoding.ASCII.GetBytes(requestBody);

            byte[] hash = await ComputeHash(reqBodyByteArray);
            if (hash != null)
            {
                requestContentBase64String = Convert.ToBase64String(hash);
            }

            string signatureRawData = String.Format("{0}{1}{2}{3}{4}{5}", APPId, requestHttpMethod, requestUri, requestTimeStamp, nonce, requestContentBase64String);
            var secretKeyBytes = Convert.FromBase64String(sharedKey);
            byte[] signature = Encoding.UTF8.GetBytes(signatureRawData);
            using (HMACSHA256 hmac = new HMACSHA256(secretKeyBytes))
            {
                byte[] signatureBytes = hmac.ComputeHash(signature);
                return (incomingBase64Signature.Equals(Convert.ToBase64String(signatureBytes), StringComparison.Ordinal));
            }

        }

        private string ReadBodyAsString(HttpRequest request)
        {
            var initialBody = request.Body; // Workaround

            try
            {
                request.EnableBuffering();

                using (StreamReader reader = new StreamReader(request.Body))
                {
                    string text = reader.ReadToEnd();
                    return text;
                }
            }
            finally
            {
                // Workaround so MVC action will be able to read body as well
                request.Body = initialBody;
            }

            return string.Empty;
        }

        private bool isReplayRequest(string nonce, string requestTimeStamp)
        {
            if (System.Runtime.Caching.MemoryCache.Default.Contains(nonce))
            {
                return true;
            }
            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan currentTs = DateTime.UtcNow - epochStart;
            var serverTotalSeconds = Convert.ToUInt64(currentTs.TotalSeconds);
            var requestTotalSeconds = Convert.ToUInt64(requestTimeStamp);
            if ((serverTotalSeconds - requestTotalSeconds) > requestMaxAgeInSeconds)
            {
                return true;
            }
            System.Runtime.Caching.MemoryCache.Default.Add(nonce, requestTimeStamp, DateTimeOffset.UtcNow.AddSeconds(requestMaxAgeInSeconds));
            return false;
        }


        private static async Task<byte[]> ComputeHash(byte[] httpContent)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = null;
               
                if (httpContent.Length != 0)
                {
                    hash = md5.ComputeHash(httpContent);
                }
                return hash;
            }
        }
    }
}

