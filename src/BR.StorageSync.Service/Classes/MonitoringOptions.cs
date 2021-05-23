using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BR.StorageSync.Service.Classes
{
    public class MonitoringOptions
    {
        public const string Section = "MonitoringOptions";

        public List<MonitoredPath> Paths { get; set; }

    }
}
