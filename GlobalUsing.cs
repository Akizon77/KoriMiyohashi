global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using System.Reflection;
global using KoriMiyohashi;
global using Serilog;
global using MamoLib;
global using System.IO;
global using System.Threading;

namespace KoriMiyohashi
{
    public static class AppInfo
    {
        static AppInfo()
        {
            if (bool.TryParse(MamoLib.EnvConfig.Get("DEBUG", "false"), out bool value) ? value : false)
                Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateLogger();
            else
                Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Information().CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error((Exception)e.ExceptionObject, "Unhandled Exception.");
        }

        public static string Version => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    }
}

