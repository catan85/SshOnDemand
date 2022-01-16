using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ApiServer.Filters
{
    public class HmacAuthResponseAttribute : ResultFilterAttribute
    {


        private static Dictionary<string, string> allowedApps = new Dictionary<string, string>();
        private readonly UInt64 requestMaxAgeInSeconds = 300; //Means 5 min
        private readonly string authenticationScheme = "hmacauth";


        public HmacAuthResponseAttribute()
        {
            if (allowedApps.Count == 0)
            {
                bool fault = false;
                DataTable clientsTable = PostgreSQLClass.GetClientsDatatable(out fault);

                if (fault)
                    throw new Exception("Cannot read clients table");

                foreach (DataRow row in clientsTable.Rows)
                {
                    allowedApps.Add((string)row["client_name"], (string)row["client_key"]);
                }
            }
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {

            if (!(context.Result is UnauthorizedResult))
            {

                string responseContentBase64String = string.Empty;

                string requestUri = UriHelper.GetEncodedUrl(context.HttpContext.Request);
                requestUri = HttpUtility.UrlEncode(requestUri.ToLower());

                //Get the Request HTTP Method type
                string requestHttpMethod = context.HttpContext.Request.Method;

                //Calculate UNIX time
                DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan timeSpan = DateTime.UtcNow - epochStart;
                string requestTimeStamp = Convert.ToUInt64(timeSpan.TotalSeconds).ToString();

                //Create the random nonce for each response
                string nonce = Guid.NewGuid().ToString("N");

                //Checking if the request contains body, usually will be null wiht HTTP GET and DELETE
            
                if (context.Result != null )
                {

                    string responseContent = "";
                    //byte[] responseBodyByteArray = Encoding.ASCII.GetBytes(responseContent);
                    if (((ObjectResult)context.Result).Value is string)
                    {
                        responseContent = ((ObjectResult)context.Result).Value.ToString();
                    }
                    else
                    {
                        responseContent = JsonConvert.SerializeObject(((ObjectResult)context.Result).Value, Formatting.None);
                    }
                    
                    byte[] responseBodyByteArray = Encoding.ASCII.GetBytes(responseContent);

                    MD5 md5 = MD5.Create();
                    byte[] responseContentHash = md5.ComputeHash(responseBodyByteArray);
                    responseContentBase64String = Convert.ToBase64String(responseContentHash);
                    Console.WriteLine("Full response content: " + Encoding.UTF8.GetString(responseBodyByteArray, 0, responseBodyByteArray.Length));
                    Console.WriteLine("Response hashed: " + responseContentBase64String);
                }

                context.HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues authString);
                var authHeader = authString.ToString().Replace("hmacauth ", "");
                var authArray = authHeader.Split(":");

                var APPId = authArray[0];
                var APIKey = allowedApps[APPId];

                //Creating the raw string by combining
                //APPId, request Http Method, request Uri, request TimeStamp, nonce, request Content Base64 String
                string requestStringValue = String.Format(
                    "{0}{1}{2}{3}{4}{5}", 
                    APPId, 
                    requestHttpMethod, 
                    requestUri, 
                    requestTimeStamp, 
                    nonce, 
                    responseContentBase64String);
                
                //Converting the APIKey into byte array
                var secretKeyByteArray = Convert.FromBase64String(APIKey);

                //Converting the requestStringValue into byte array
                byte[] requestValueArray = Encoding.UTF8.GetBytes(requestStringValue);

                //Generate the hmac authenticated value and set it in the Authorization header
                using (HMACSHA256 hmac = new HMACSHA256(secretKeyByteArray))
                {
                    byte[] authenticatedRequestValueArray = hmac.ComputeHash(requestValueArray);
                    string authenticatedBase64String = Convert.ToBase64String(authenticatedRequestValueArray);

                    //Setting the values in the Authorization header using custom scheme (hmacauth)
                    context.HttpContext.Response.Headers["Authorization"] = string.Format("hmacauth {0}:{1}:{2}:{3}", APPId, authenticatedBase64String, nonce, requestTimeStamp);
                    Console.WriteLine(context.HttpContext.Response.Headers["Authorization"]);
                }
            }

            //context.HttpContext.Response.Headers.Add(_name, new string[] { _value });
            base.OnResultExecuting(context);
        }

        public static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
    }
}
