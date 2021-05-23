using BR.StorageSync.Service.Classes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace BR.StorageSync.Service
{
    class Program
    {

        static void Main(string[] args)
        {
            var build = CreateHostBuilder(args).Build();
            build.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                    Host.CreateDefaultBuilder(args)
                        .UseWindowsService()
                        .ConfigureLogging(configureLogging =>
                        {
                            configureLogging.AddEventLog();
                        })
                        .ConfigureServices(services =>
                        {
                            services.AddHostedService<StorageSyncService>();
                        })
                        .ConfigureAppConfiguration((context, config) =>
                        {

                            var env = context.HostingEnvironment;

                            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                  .AddJsonFile($"appsettings.{env.EnvironmentName}.json",
                                                 optional: true, reloadOnChange: true);
                            config.AddEnvironmentVariables();

                            if (args != null)
                            {
                                config.AddCommandLine(args);
                            }
                        });
    }
}
