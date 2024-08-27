using HeartBeat;
using murrayju.ProcessExtensions;

namespace WatcherService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private static DbManager dbManager = new DbManager();
        private static Watcher _watcher;
        private static Timer _monitoringTimer;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {

            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _watcher = new Watcher(_configuration, _logger);
            Task.Run(() => ListenForMessages(stoppingToken), stoppingToken);
            StartMonitoringEverySecond();
           

            _watcher.StartSendingHeartbeat();   

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
        private void ListenForMessages(CancellationToken stoppingToken)
        {
            const int port = 5000; //port to listen on 
            Console.WriteLine($"Watcher listening on port {port}...");
            UdpReceiver udpReceiver = new UdpReceiver(port);
            while (true)
            {
                
                string message = udpReceiver.ReceiveMessage();
                _logger.LogWarning(message);

                string appId = ParseAppID(message);

                if (_watcher.Apps.TryGetValue(appId, out var app))
                {
                    app.ResetTimer();
                    dbManager.LogReceivedMessage(app.AppId, DateTime.Now);
                }
                else
                { 
                    _logger.LogWarning($"Cant read app ID {app.AppId}");
                }

            }
        }
       
        private static void StartMonitoringEverySecond()
        {
            _monitoringTimer = new Timer(MonitorApps, null, 0, 1000);
        }
        private static void MonitorApps(object state)
        {
            _watcher.StartMonitoring();
        }
        private static string ParseAppID(string message)
        {
            var parts = message.Split('-');
            return parts[0];
        }
    }
}
