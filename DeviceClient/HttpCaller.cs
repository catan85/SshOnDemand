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
        private HMACDelegatingHandler customDelegatingHandler = new HMACDelegatingHandler();
        private HttpClient client = null;

        public HttpCaller()
        {
            HMACDelegatingHandler.APPId = ConfigurationManager.AppSettings["ClientName"];
            HMACDelegatingHandler.APIKey = ConfigurationManager.AppSettings["ClientKey"];

            client = HttpClientFactory.Create(customDelegatingHandler);
            
        }

        public async Task<DeviceConnectionStatus> CheckRemoteConnectionRequest()
        {
            DeviceConnectionStatus state = await DeviceCheckConnectionRequest(client);

            return state;
        }


        private async Task<DeviceConnectionStatus> DeviceCheckConnectionRequest(HttpClient client)
        {
            HttpResponseMessage response = null;
            Console.WriteLine("Checking for device connection request..");
            response = await client.PostAsJsonAsync(apiBaseAddress + "DeviceCheckRemoteConnectionRequest", "dev pub ssh key");

            if (response.IsSuccessStatusCode)
            {
                bool authenticated = HMACResponseAuthentication.IsResponseAuthenticated(response);
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
            string state = await SetDeviceConnection(client, ClientConnectionState.Connected);
            Console.WriteLine(state);
        }

        public async Task ResetActiveDeviceConnection()
        {
            string state = await SetDeviceConnection(client, ClientConnectionState.Disconnected);
            Console.WriteLine(state);
        }


        private async Task<string> SetDeviceConnection(HttpClient client, ClientConnectionState connectionState)
        {
            HttpResponseMessage response = null;

            response = await client.PostAsJsonAsync(apiBaseAddress + "DeviceSetConnectionState", connectionState);

            if (response.IsSuccessStatusCode)
            {
                bool authenticated = HMACResponseAuthentication.IsResponseAuthenticated(response);
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