using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace DeviceClient
{
    public sealed class Configuration
    {
        private static Configuration _instance;


        public static Configuration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Configuration();
                }
                return _instance;
            }
        }


        public bool EnableDebug { get; set; }
        public string ClientName { get; set; }

        private Configuration()
        {
            EnableDebug = ConfigurationManager.AppSettings["EnableDebug"] == "True" ? true : false;
            ClientName = ConfigurationManager.AppSettings["ClientName"];
        }
    }

    
}
