﻿using SshOnDemandLibs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;


namespace DeviceClient
{
    class HttpCaller
    {
        private string apiBaseAddress = "https://localhost:5001/";
        private HMACDelegatingHandler customDelegatingHandler = new HMACDelegatingHandler(true);
        private HttpClient client = null;
        private HMACResponseAuthentication hmacResponseAuthenticator = new HMACResponseAuthentication(Configuration.Instance.EnableDebug, true);
        public HttpCaller()
        {
            HMACDelegatingHandler.ClientId = Configuration.Instance.ClientName;

            client = HttpClientFactory.Create(customDelegatingHandler);
        }

        public async Task<DeviceConnectionStatus> CheckRemoteConnectionRequest(string publicKey)
        {
            DeviceConnectionStatus state = await DeviceCheckConnectionRequest(client, publicKey);

            return state;
        }


        private async Task<DeviceConnectionStatus> DeviceCheckConnectionRequest(HttpClient client, string publicKey)
        {
            HttpResponseMessage response = null;
            Console.WriteLine("Checking for device connection request..");
            response = await client.PostAsJsonAsync(apiBaseAddress + "DeviceCheckRemoteConnectionRequest",  publicKey);

            if (response.IsSuccessStatusCode)
            {
                bool authenticated = hmacResponseAuthenticator.IsResponseAuthenticated(response);
                if (authenticated)
                {
                    string responseString = await response.Content.ReadAsStringAsync();

                    DeviceConnectionStatus currentStatus = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceConnectionStatus>(responseString);
                    Console.WriteLine(responseString);

                    return currentStatus;
                }
            }
            else
            {
                await PrintFailMessage(response);
            }

            return null;
        }

        public async Task SetActiveDeviceConnection()
        {
            string state = await SetDeviceConnection(client, EnumClientConnectionState.Connected);
            Console.WriteLine(state);
        }

        public async Task SetClosedSshConnectionState()
        {
            string state = await SetDeviceConnection(client, EnumClientConnectionState.ClosedSsh);
            Console.WriteLine(state);
        }


        private async Task<string> SetDeviceConnection(HttpClient client, EnumClientConnectionState connectionState)
        {
            HttpResponseMessage response = null;

            response = await client.PostAsJsonAsync(apiBaseAddress + "DeviceSetConnectionState", connectionState);

            if (response.IsSuccessStatusCode)
            {
                bool authenticated = hmacResponseAuthenticator.IsResponseAuthenticated(response);
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


        private async Task PrintFailMessage(HttpResponseMessage response)
        {
            Console.WriteLine("Failed to call the API. HTTP Status: {0}, Reason {1}", response.StatusCode, response.ReasonPhrase);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
        }

    }
}
