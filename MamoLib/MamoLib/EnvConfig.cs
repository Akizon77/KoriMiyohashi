using System.Collections;
using System.Collections.ObjectModel;

namespace MamoLib
{
    public static class EnvConfig
    {
        private static Dictionary<string, string> Envs = new Dictionary<string, string>();

        static EnvConfig()
        {
            LoadEnvironmentVariables(Envs);
            LoadEnvFileVariables(Envs, ".env");
            Dictionary = new ReadOnlyDictionary<string, string>(Envs);
        }

        /// <summary>
        /// Environments variable
        /// </summary>
        public static IReadOnlyDictionary<string, string> Dictionary { get; private set; }

        public static string Get(string key, string fallback = "")
        {
            if (Envs.TryGetValue(key, out string? value))
            {
                return value;
            }
            else
            {
                return fallback;
            }
        }

        private static void LoadEnvironmentVariables(Dictionary<string, string> configVariables)
        {
            IDictionary environmentVariables = System.Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry de in environmentVariables)
            {
                if (de.Key == null) continue;
                configVariables[de.Key.ToString()!] = de.Value is null ? "" : de.Value.ToString()!;
            }
        }

        private static void LoadEnvFileVariables(Dictionary<string, string> configVariables, string filePath)
        {
            if (!File.Exists(filePath)) return;
            foreach (var line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) && line.StartsWith("#")) continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (!configVariables.ContainsKey(key))
                    configVariables[key] = value;
            }
        }
    }
}