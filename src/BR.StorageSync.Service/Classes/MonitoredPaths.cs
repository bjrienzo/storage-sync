using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BR.StorageSync.Service.Classes
{
    public class MonitoredPath
    {
        public string Path { get; set; }
        public MonitoredPathSettings Settings { get; set; }
    }

    public class MonitoredPathSettings
    {
        public string Filter { get; private set; }
        public string DestinationContainer { get; set; }
        public string ConnectionString { get; set; }
    }

}
