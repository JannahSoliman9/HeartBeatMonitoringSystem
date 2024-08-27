using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.ServiceProcess;
using HeartBeat;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace BackupWatcher
{
    public class Watcher
    {

        public ConcurrentDictionary<string, App> Apps { get; private set; }
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private DbManager dbManager = new DbManager();
        private string _watcherId;
        private int _watcherIntervalInSeconds;
        private Timer _sendHeartbeatTimer;
        const string ipAddress = "127.0.0.1";
        const int port = 8000;
        public Watcher(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration; 
            _logger = logger;
            Apps = new ConcurrentDictionary<string, App>();
            LoadConfig();
        }

        private void LoadConfig()
        {
            var config = _configuration.GetSection("Apps").Get<List<WatcherAppsConfig>>();
            try
            {
                if (config == null)
                {
                    Console.WriteLine("Configuration not found");
                    return;
                }
                foreach (var appConfig in config)
                {
                    if (appConfig == null)
                    {
                        Console.WriteLine("Invalid app configuration found.");
                        continue;
                    }

                    App app = new App(appConfig.AppId, appConfig.IntervalInSeconds, appConfig.ExecutablePath);
                    Apps.TryAdd(app.AppId, app);
                   _logger.LogWarning($"Loaded config for {app.AppId}");
                }
                //loading watcher's own heartbeat configuration
                //
                var WatcherConfig = _configuration.GetSection("WatcherConfig");
                _watcherId = WatcherConfig["AppId"];
                _watcherIntervalInSeconds = int.Parse(WatcherConfig["IntervalInSeconds"]);
                _logger.LogWarning($"Loaded Backup Watcher config: {_watcherId}, Interval: {_watcherIntervalInSeconds} seconds");
            

            }
            catch (Exception ex)
            {
                _logger.LogWarning($"error loading configuration {ex:message}");
            }
        }
        public void RestartApp(App app)
        {
            try
            {
                var processes = Process.GetProcessesByName(app.AppId);
                foreach (var process in processes)
                {
                    try
                    {
                        Console.WriteLine("before terminating...");
                        process.Kill();
                        process.WaitForExit();
                        Console.WriteLine($"Terminated process: {app.AppId} with ID: {process.Id}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to kill process {process.ProcessName} with ID {process.Id}: {ex.Message}");
                    }
                }
                Console.WriteLine($"Restarting {app.AppId}...");
                var startInfo = new ProcessStartInfo
                {
                    FileName = app.ExecutablePath,
                    UseShellExecute = true,  // Set to true if the executable requires shell features
                    WorkingDirectory = Path.GetDirectoryName(app.ExecutablePath),
                };
                try
                {
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
                dbManager.LogAppRecovery(app.AppId, DateTime.Now);
                Console.WriteLine("After Start");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restart {app.AppId}: {ex.Message}");
            }
        }
        public void StartMonitoring()
        {
            new Thread(MonitorApps).Start();    
        }
        private void MonitorApps()
        {
            foreach (var app in Apps.Values)
            {
                if (app.AppTimer > app.AppInterval)
                {
                    _logger.LogWarning($"Alert: {app.AppId} is not responding.");
                    _logger.LogWarning($"Restarting: {app.AppId}");
                    app.ResetTimer();
                    // RestartApp(app);
                    restartService(app.AppId);
                    Console.WriteLine($" {app.AppId} Restarted Successfully");
                    dbManager.LogAppRecovery(app.AppId, DateTime.Now);


                }
                app.IncrementTimer();
            }
        }
        void stopService(string serviceName)
        {
            using (ServiceController sc = new ServiceController(serviceName))
            {
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }

            }

        }
        void startService(string serviceName)
        {
            using (ServiceController sc = new ServiceController(serviceName))
            {
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }

            }

        }
        void restartService(string serviceName)
        {
            stopService(serviceName);
            startService(serviceName);
        }
        private void SendHeartbeat(object state)
        {
            string message = $"{_watcherId}-{DateTime.Now}";
            UdpSender.SendMessage(message, ipAddress, 5000);
            dbManager.LogAppHeartbeat(_watcherId, DateTime.Now);
        }
        public void StartSendingHeartbeat()
        {
            _sendHeartbeatTimer = new Timer(SendHeartbeat, null, 0, _watcherIntervalInSeconds * 1000);
        }
    }
}
