﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace DeveloperClient
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
        public string ClientKey { get; set; }
        public string TargetDevice { get; set; }

        private Configuration()
        {
            EnableDebug = ConfigurationManager.AppSettings["EnableDebug"] == "True" ? true : false;
            ClientName = ConfigurationManager.AppSettings["ClientName"];
            ClientKey = ConfigurationManager.AppSettings["ClientKey"];
            TargetDevice = ConfigurationManager.AppSettings["TargetDevice"];
        }
    }

    
}
