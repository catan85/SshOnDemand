using SshOnDemandLibs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using SshOnDemandLibs;


namespace DeveloperClient
{
    class HttpCaller
    {
        private string apiBaseAddress = "https://localhost:5001/";
        private HMACDelegatingHandler customDelegatingHandler = new HMACDelegatingHandler();
        private HttpClient client = null;
        private Logger logger = new Logger(Configuration.Instance.EnableDebug);
        private HMACResponseAuthentication hmacAuthenticator = new HMACResponseAuthentication(Configuration.Instance.EnableDebug);
        public HttpCaller()
        {


            HMACDelegatingHandler.ClientId = Configuration.Instance.ClientName;
            HMACDelegatingHandler.ClientKey = Configuration.Instance.ClientKey;

            client = HttpClientFactory.Create(customDelegatingHandler);
        }


        public async Task InsertConnectionRequest()
        {
            string state = await DeveloperConnectionRequest(client);

            logger.Debug(state);
        }


        private async Task<string> DeveloperConnectionRequest(HttpClient client)
        {
            HttpResponseMessage response = null;

            string deviceName = Configuration.Instance.TargetDevice;

            response = await client.PostAsJsonAsync(apiBaseAddress + "DeveloperDeviceConnectionRequest", deviceName);

            if (response.IsSuccessStatusCode)
            {
                bool authenticated = hmacAuthenticator.IsResponseAuthenticated(response);
                if (authenticated)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    return responseString;
                }
            }
            else
            {
                await PrintFailMessage(response);
            }

            return null;
        }

        public async Task<DeviceConnectionStatus> CheckDeviceConnectionState(string sshPublicKey)
        {
            DeviceConnectionStatus state = await DeveloperCheckDeviceConnectionState(client, sshPublicKey);

            return state;
        }


        private async Task<DeviceConnectionStatus> DeveloperCheckDeviceConnectionState(HttpClient client, string sshPublicKey)
        {
            HttpResponseMessage response = null;


            DeveloperCheckDeviceConnectionArgs args = new DeveloperCheckDeviceConnectionArgs();
            args.DeveloperSshPublicKey = sshPublicKey;
            args.DeviceName = Configuration.Instance.TargetDevice;

            response = await client.PostAsJsonAsync(apiBaseAddress + "DeveloperCheckDeviceConnection", args);

            if (response.IsSuccessStatusCode)
            {
                bool authenticated = hmacAuthenticator.IsResponseAuthenticated(response);
                if (authenticated)
                {
                    string responseString = await response.Content.ReadAsStringAsync();

                    DeviceConnectionStatus currentStatus = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceConnectionStatus>(responseString);
                    logger.Debug(responseString);

                    return currentStatus;
                }
            }
            else
            {
                await PrintFailMessage(response);
            }

            return null;
        }

        private async Task PrintFailMessage(HttpResponseMessage response)
        {
            logger.Output($"Failed to call the API. HTTP Status: {response.StatusCode}, Reason {response.ReasonPhrase}");
            string responseString = await response.Content.ReadAsStringAsync();
            logger.Output(responseString);
        }
    }
}
