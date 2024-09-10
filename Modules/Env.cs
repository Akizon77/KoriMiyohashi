using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoriMiyohashi.Modules
{
    public static class Env
    {
        public static string DB_TYPE => EnvConfig.Get("DB_TYPE", "sqlite");
        public static string DB_CONNECTION_STRING => EnvConfig.Get("DB_CONNECTION_STRING", "");
        public static string DB_FILE => EnvConfig.Get("DB_FILE", "./KoriMiyohashi.db");
        public static bool USE_PROXY => ToBool("USE_PROXY",false);
        public static string PROXY => EnvConfig.Get("PROXY", "socks5://127.0.0.1:12612");
        public static string TG_TOKEN => EnvConfig.Get("TG_TOKEN", "");
        public static long WORK_GROUP => ToLong("WORK_GROUP");

        static bool ToBool(string key,bool fallback = false)
        {
            return (bool.TryParse(EnvConfig.Get(key), out bool value)) ? value : fallback;
        }
        static long ToLong(string key, long fallback = 0)
        {
            return (long.TryParse(EnvConfig.Get(key), out long value)) ? value : fallback;
        }
    }
}
