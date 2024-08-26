using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeartBeat
{
    public class WatcherAppsConfig
    {
        public string AppId { get; set; }
        public int IntervalInSeconds { get; set; }
        public string ExecutablePath { get; set; }
    }
}
