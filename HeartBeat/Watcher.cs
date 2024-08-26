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

namespace WatcherService
{
    public class Watcher
    {

        public ConcurrentDictionary<string, App> Apps { get; private set; }
        private readonly IConfiguration _configuration;
        private DbManager dbManager = new DbManager();
        public Watcher(IConfiguration configuration)
        {
            _configuration = configuration; 
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
                    Console.WriteLine($"Loaded config for {app.AppId}");
                }

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
                    Console.WriteLine($"Alert: {app.AppId} is not responding.");
                    Console.WriteLine($"Restarting: {app.AppId}");
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
    }
}
