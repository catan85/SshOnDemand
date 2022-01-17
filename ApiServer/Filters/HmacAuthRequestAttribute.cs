using ApiServer.Models;
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
    public class HmacAuthRequestAttribute : Attribute, IAuthorizationFilter
    {
        private static Dictionary<string, string> clientsSharedKeys = new Dictionary<string, string>();
        private readonly UInt64 requestMaxAgeInSeconds = 300; //Means 5 min
        private readonly string authenticationScheme = "hmacauth";
   
        public HmacAuthRequestAttribute()
        {

        }

        private void UpdateSharedKeys()
        {
            using (sshondemandContext dbContext = new sshondemandContext())
            {
                clientsSharedKeys.Clear();
                foreach (var client in dbContext.Clients)
                {
                    clientsSharedKeys.Add(client.ClientName, client.ClientKey);
                }
            }
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Lettura del Authorization Header contenente la firma autenticata
            context.HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues authString);
            if (!StringValues.IsNullOrEmpty(authString) && authString.ToString().StartsWith("hmacauth "))
            {
                // si rimuove l'intestazione della firma in modo da poter ottenere solo i parametri utili alla verifica
                var authHeader = authString.ToString().Replace($"{authenticationScheme} ", "");

                var authArray = authHeader.Split(":");

                if (authArray.Length == 4)
                {
                    // Vengono estratti i parametri necessari alla verifica
                    var clientId = authArray[0];
                    var authValue = authArray[1];
                    var nonce = authArray[2];
                    var timestamp = authArray[3];

                    // Viene lanciato il metodo di verifica
                    var isValid = IsValidRequest(context.HttpContext.Request, clientId, authValue, nonce, timestamp);

                    // Passaggio del client name al controller per conoscere l'identità del device
                    context.HttpContext.Items["ClientName"] = clientId;

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

        }


        private async Task<bool> IsValidRequest(HttpRequest req, string clientId, string incomingBase64AuthenticatedValue, string nonce, string requestTimeStamp)
        {
            // estrazione dell'uri dalla chiamata ricevuta
            string uri = UriHelper.GetEncodedUrl(req);
            uri = HttpUtility.UrlEncode(uri.ToLower());

            // lettura del metodo (GET/POST/PUT/PATH/DELETE)
            string requestHttpMethod = req.Method;

            // Se il client non è tra quelli abilitati aggiorno la cache
            if (!clientsSharedKeys.ContainsKey(clientId))
            {
                UpdateSharedKeys();
            }

            // Se ancora il client non è tra la lista dei device prelevati dal db torno false
            if (!clientsSharedKeys.ContainsKey(clientId))
            {
                return false;
            }

            // Preleva dal dizionario delle sharedKey quella del client facente la richiesta
            var sharedKey = clientsSharedKeys[clientId];

            // Verifica se si tratta di una replay request
            if (IsReplayRequest(nonce, requestTimeStamp))
            {
                return false;
            }

            // Preleva il body della richiesta
            byte[] requestContentByteArray = await GetRequestBody(req);

            // Calcola l'hash con algoritmo MD5
            string contentHashString = await CalculateContentHashString(requestContentByteArray);

            // Compone lo string Value
            string requestStringValue = String.Format("{0}{1}{2}{3}{4}{5}", 
                clientId, 
                requestHttpMethod, 
                uri, 
                requestTimeStamp, 
                nonce, 
                contentHashString);

            // Calcola la versione autenticata della stringa con HMAC
            string authStringValue = CalculateHmacString(requestStringValue, sharedKey);

            // Torna true se le due stringhe autenticate (calcolata e ricevuta) sono uguali
            return incomingBase64AuthenticatedValue.Equals(authStringValue, StringComparison.Ordinal);

        }

        private async Task<byte[]> GetRequestBody(HttpRequest req)
        {
            byte[] reqBodyByteArray = null;
            req.EnableBuffering();
            req.Body.Position = 0;
            using (var ms = new MemoryStream(2048))
            {
                await req.Body.CopyToAsync(ms);
                reqBodyByteArray = ms.ToArray();
            }
            req.Body.Position = 0;
            return reqBodyByteArray;
        }

        private bool IsReplayRequest(string nonce, string requestTimeStamp)
        {
            // Verifica se nella memory cache è già stato ricevuto il nonce
           
            if (System.Runtime.Caching.MemoryCache.Default.Contains(nonce))
            {
                // In caso affermativo torna true: si tratta di una replay request
                return true;
            }

            // Calcola la data e ora del server in unix time
            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan currentTs = DateTime.UtcNow - epochStart;
            var serverUnixTime = GetCurrentUnixTime();
            var requestUnixTime = Convert.ToUInt64(requestTimeStamp);

            // Se la differenza tra l'ora del server e quella della richiesta è maggiore 
            // della massima consentita si tratta di una replay request
            if ((serverUnixTime - requestUnixTime) > requestMaxAgeInSeconds)
            {
                return true;
            }

            // Definizione scadenza
            DateTimeOffset nonceExpiry = DateTimeOffset.UtcNow.AddSeconds(requestMaxAgeInSeconds);

            // Aggiunta del nonce nella Memory Cache
            System.Runtime.Caching.MemoryCache.Default.Add(nonce, requestTimeStamp, nonceExpiry);
            return false;
        }

        private ulong GetCurrentUnixTime()
        {
            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = DateTime.UtcNow - epochStart;
            return Convert.ToUInt64(timeSpan.TotalSeconds);
        }

        private async Task<string> CalculateContentHashString(byte[] reqBodyByteArray)
        {
            string requestHashString = "";
            byte[] requestHashByteArray = await CalculateContentHash(reqBodyByteArray);
            if (requestHashByteArray != null)
            {
                requestHashString = Convert.ToBase64String(requestHashByteArray);
            }
            return requestHashString;
        }

        private static async Task<byte[]> CalculateContentHash(byte[] httpContent)
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

        private string CalculateHmacString(string value, string secret)
        {
            var secretByteArray = Convert.FromBase64String(secret);
            byte[] valueByteArray = Encoding.UTF8.GetBytes(value);

            // Crea l’oggetto hmacAlgorithm dando in ingresso la chiave privata condivisa
            HMACSHA256 hmacAlgorithm = new HMACSHA256(secretByteArray);
            // Esegue il calcolo dell’hash autenticato
            byte[] hmacByteArray = hmacAlgorithm.ComputeHash(valueByteArray);
            // converte in stringa il valore calcolato e lo torna
            return Convert.ToBase64String(hmacByteArray);
        }


    }
}

