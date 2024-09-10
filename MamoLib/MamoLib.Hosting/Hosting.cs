using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MamoLib
{
    public static class Hosting
    {
        private static IHost? _host { get; set; }
        private static HostApplicationBuilder _hostBuilder { get; set; }
        static Hosting()
        {
            _hostBuilder = Host.CreateApplicationBuilder();
        }
        public static void Register<T>() where T : class
        {
            _hostBuilder.Services.AddSingleton<T>();
        }
        public static void Register(Action<IServiceCollection> action)
        {
            action.Invoke(_hostBuilder.Services);
        }
        public static void Register<T>(T instance) where T : class
        {
            _hostBuilder.Services.AddSingleton(typeof(T), instance);
        }
        public static void Build()
        {
            _host = _hostBuilder.Build();
        }

        public static T GetRequiredService<T>() where T : notnull
        {
            if (_host is null)
                throw new InvalidOperationException("Hosting not built! Run Build() first.");
            return _host.Services.GetRequiredService<T>();
        }
        public static object? GetRequiredService(Type type)
        {
            if (_host is null)
                throw new InvalidOperationException("Hosting not built! Run Build() first.");
            return _host.Services.GetRequiredService(type);
        }

    }
}
