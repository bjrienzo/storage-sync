using BR.StorageSync.Service.Classes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BR.StorageSync.Service
{
    class StorageSyncService : IHostedService
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ILogger<MonitorInstance> _monitorLogger;
        private readonly MonitoringOptions _monitoringOptions;
        private static ConcurrentBag<MonitorInstance> _monitors = new ConcurrentBag<MonitorInstance>(); //Future use


        public StorageSyncService(IConfiguration configuration, ILogger<StorageSyncService> logger, ILogger<MonitorInstance> monitorLogger)
        {
            _configuration = configuration;
            _monitoringOptions = _configuration.GetSection(MonitoringOptions.Section).Get<MonitoringOptions>();
            _logger = logger;
            _monitorLogger = monitorLogger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Storage Service Starting");

            Parallel.ForEach(_monitoringOptions.Paths, async monitoredPath => {
                _logger.LogTrace($"Configuring Montoring for #{monitoredPath.Path}#");
                if (!Directory.Exists(monitoredPath.Path))
                {
                    _logger.LogWarning($"Specified Path Does Not Exist #{monitoredPath.Path}#");
                    return;
                }

                var monitor = new MonitorInstance(monitoredPath, _monitorLogger);
                if (await monitor.Initialize() == 0)
                {
                    _monitors.Add(monitor);
                }
                else
                {
                    _logger.LogError($"Monitor Failed to Initialize #{monitoredPath.Path}#");
                    return;
                }
                
            });

            return Task.FromResult(0);

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
