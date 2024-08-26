using HeartBeat;

namespace BackupWatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private static DbManager dbManager = new DbManager();
        const string ipAddress = "127.0.0.1";
        const int port = 8000;
        private static Watcher _watcher;
        private static Timer _monitoringTimer;
        private Timer _sendTimer;
        private TimeSpan _interval;
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {

            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _watcher = new Watcher(_configuration ,_logger);
            Task.Run(() => ListenForMessages(), stoppingToken);
    
            StartMonitoringEverySecond();
            _watcher.StartSendingHeartbeat();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
        private void ListenForMessages()
        {
            const int port = 8000; //port to listen on 
            Console.WriteLine($"Watcher listening on port {port}...");
            UdpReceiver udpReceiver = new UdpReceiver(port);
            while (true)
            {
                string message = udpReceiver.ReceiveMessage();
                Console.WriteLine($"Received {message}");
                _logger.LogWarning(message);
                string appId = ParseAppID(message);

                if (_watcher.Apps.TryGetValue(appId, out var app))
                {
                    app.ResetTimer();
                    dbManager.LogReceivedMessage(app.AppId, DateTime.Now);
                }
                else
                {
                    Console.WriteLine("Cant read app ID");
                    _logger.LogWarning("Cant read app ID");
                }

            }
        }

        private void ListenForMessagesFromBackup()
        {
            const int port = 8000; //port to listen on 
            Console.WriteLine($"Watcher listening on port {port}...");

            while (true)
            {
                UdpReceiver udpReceiver = new UdpReceiver(port);
                string message = udpReceiver.ReceiveMessage();
                Console.WriteLine($"Received {message}");
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
