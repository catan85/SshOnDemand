using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SshOnDemandLibs
{
    public class HMACDelegatingHandler : DelegatingHandler
    {
        public static string ClientId = "";
        public static string ClientKey = "";
        private static bool useTpm = false;


        public HMACDelegatingHandler(bool useTpmSigning)
        {
            useTpm = useTpmSigning;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            
            // Prelevo l'uri della richiesta
            string uri = HttpUtility.UrlEncode(request.RequestUri.AbsoluteUri.ToLower());

            // Prelevo il metodo della richiesta HTTP (GET, POST, DELETE, PUT, PATCH)
            string requestHttpMethod = request.Method.Method;

            // Genero una stringa contenente il timestamp corrente in formato unix time
            string requestTimeStamp = GetCurrentUnixTime();

            // Genero un nonce random
            string nonce = Guid.NewGuid().ToString("N");

            // Calcolo l'hash MD5 del contenuto della richiesta HTTP
            // qualora la richiesta non avesse contenuto l'hash risulta vuoto (ad esempio con metodi GET o DELETE)
            string contentHash = await CalculateContentHash(request);

            // Creo la signature della richiesta HTTP concatenando i parametri:
            // ClientId, metodo http, uri, timestamp, nonce, hash del contenuto della richiesta
            string requestSignature = String.Format("{0}{1}{2}{3}{4}{5}", 
                ClientId, 
                requestHttpMethod, 
                uri, 
                requestTimeStamp, 
                nonce, 
                contentHash);

            // viene cifrata la signature utilizzando l'algoritmo HMAC

            string cyphredSignature = "";
            
            if (useTpm)
            {
                cyphredSignature = CalculateHmacStringWithTpm(requestSignature);
            }
            else
            {
                cyphredSignature = CalculateHmacString(requestSignature, ClientKey);
            }
        
            // si crea il valore del parametro hmac che inseriremo nell'authentication header
            // concatenando ClientId, signature cifrata, nonce, timestamp
            string hmacAuthParameterValue = string.Format("{0}:{1}:{2}:{3}",
                ClientId,
                cyphredSignature,
                nonce,
                requestTimeStamp);

            // Viene settato il parametro "hmacauth" dell'authentication header
            request.Headers.Authorization = new AuthenticationHeaderValue("hmacauth", hmacAuthParameterValue);

            // Viene inoltrata la richiesta
            response = await base.SendAsync(request, cancellationToken);
            return response;
        }


        private string GetCurrentUnixTime()
        {
            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = DateTime.UtcNow - epochStart;
            return Convert.ToUInt64(timeSpan.TotalSeconds).ToString();
        }

        private async Task<string> CalculateContentHash(HttpRequestMessage request)
        {
            string contentHash = string.Empty;
            if (request.Content != null)
            {
                byte[] content = await request.Content.ReadAsByteArrayAsync();
                // Crea l'oggetto MD5 che permette di utilizzare la funzione hash 
                MD5 md5 = MD5.Create();
                // Utilizza l'oggetto lanciando la computazione dell'hash del contenuto
                byte[] requestContentHash = md5.ComputeHash(content);
                contentHash = Convert.ToBase64String(requestContentHash);
            }
            return contentHash;
        }

        private string CalculateHmacString(string signature, string secret)
        {
            var secretByteArray = Convert.FromBase64String(secret);
            byte[] signatureByteArray = Encoding.UTF8.GetBytes(signature);

            // Crea l’oggetto hmacAlgorithm dando in ingresso la chiave privata condivisa
            HMACSHA256 hmacAlgorithm = new HMACSHA256(secretByteArray);
            // Esegue il calcolo dell’hash cifrato
            byte[] hmacByteArray = hmacAlgorithm.ComputeHash(signatureByteArray);
            // converte in stringa il valore calcolato e lo torna
            return Convert.ToBase64String(hmacByteArray);
        }

        private string CalculateHmacStringWithTpm(string signature)
        {
            byte[] signatureByteArray = Encoding.UTF8.GetBytes(signature);
            byte[] hmacByteArray = TpmHelper.SignHmac(signatureByteArray);

            // converte in stringa il valore calcolato e lo torna
            return Convert.ToBase64String(hmacByteArray);
        }

    }

}