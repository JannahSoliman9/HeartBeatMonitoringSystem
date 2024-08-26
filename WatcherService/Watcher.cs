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
using Microsoft.Win32.TaskScheduler;
using murrayju.ProcessExtensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WatcherService
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
            Apps = new ConcurrentDictionary<string, App>();
            LoadConfig();
            _logger = logger;
        }

        private void LoadConfig()
        {
            var config = _configuration.GetSection("Apps").Get<List<WatcherAppsConfig>>();
            try
            {
                if (config == null)
                {
                    _logger.LogWarning("Configuration not found");
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
                    Console.WriteLine($"Loaded config for {app.AppId}");
                }
                //loading watcher's own heartbeat configuration
                //
                var WatcherConfig = _configuration.GetSection("WatcherConfig");
                _watcherId = WatcherConfig["AppId"];
                _watcherIntervalInSeconds = int.Parse(WatcherConfig["IntervalInSeconds"]);
                Console.WriteLine($"Loaded Backup Watcher config: {_watcherId}, Interval: {_watcherIntervalInSeconds} seconds");


            }
            catch (Exception ex)
            {
                Console.WriteLine($"error loading configuration {ex:message}");
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
                try
                {
                    string executablePath = app.ExecutablePath;
                    string workingDirectory = Path.GetDirectoryName(executablePath);

                    // Start the process as the current user
                    ProcessExtensions.StartProcessAsCurrentUser(executablePath, null, workingDirectory);
                    Console.WriteLine($"Successfully restarted {app.AppId} in user session.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to restart {app.AppId} in user session: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restart {app.AppId} in user session.");
            }
      
             Console.WriteLine("After Start");
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
                    if (app.AppId.StartsWith("Service", StringComparison.OrdinalIgnoreCase))
                    {
                        restartService(app.AppId);
                    }
                    else if(app.AppId.StartsWith("Backup", StringComparison.OrdinalIgnoreCase))
                    {
                        restartService(app.AppId);
                    }
                    else if(app.AppId.StartsWith("Wpf", StringComparison.OrdinalIgnoreCase))
                    {
                        RestartApp(app);
                    }
                    else
                    {
                        _logger.LogWarning($"Unknown app ID prefix for {app.AppId}. Unable to restart.");
                    }

                    app.ResetTimer();
                    _logger.LogWarning($" {app.AppId} Restarted Successfully, with timer {app.AppTimer}");
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
            UdpSender.SendMessage(message, ipAddress, 8000);
            dbManager.LogAppHeartbeat(_watcherId, DateTime.Now);
        }
        public void StartSendingHeartbeat()
        {
            _sendHeartbeatTimer = new Timer(SendHeartbeat, null, 0, _watcherIntervalInSeconds * 1000);
        }
    }
}
