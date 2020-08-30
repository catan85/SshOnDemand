using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer
{
    public class AppSettings
    {
        public static string OS { get; set; }
        public static string ApiKey { get; set; }

        public static string DbHost { get; set; }

        public static int DbPort { get; set; }

        public static string DbName { get; set; }

        public static string DbUser { get; set; }

        public static string DbPass { get; set; }

    }
}
