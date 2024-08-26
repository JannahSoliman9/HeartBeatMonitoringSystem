using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WatcherService
{
    public class App
    {

        public string AppId { get; set; }
        public int AppInterval { get; set; }

        public int AppTimer { get; set; }
        public string ExecutablePath { get; set; }

        private Process _process;

        private readonly object _lock = new object();

        public App(string appId, int appInterval, string executablePath)
        {
            AppId = appId;
            AppInterval = appInterval;
            AppTimer = 0;
            ExecutablePath = executablePath;
        }
        public void ResetTimer()
        {
            lock (_lock)
            {
                AppTimer = 0;
            }

        }
        public void IncrementTimer()
        {
            lock (_lock)
            {
                AppTimer++;
            }
        }

        public bool IsOverDue()
        {
            lock (_lock)
            {
                return AppTimer > AppInterval;
            }
        }
    }
}

