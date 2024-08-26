using HeartBeat;
using System.Reflection;
using System.Text.Json;

namespace ServiceApp3
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        const string ipAddress = "127.0.0.1";
        const int port = 5000;
        private Timer _sendTimer;
        private string _appId;
        private int _intervalSeconds;
        private TimeSpan _interval;
        private DbManager dbManager = new DbManager();
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LoadConfig();
            _sendTimer = new Timer(SendHeartbeat, null, 0, _intervalSeconds * 1000);
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private void LoadConfig()
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            var _configuration1 = builder.Build();
            _appId = _configuration1["AppId"];
            _intervalSeconds = int.Parse(_configuration1["IntervalInSeconds"]);

        }
        private void SendHeartbeat(object state)
        {
            string message = $"{_appId}-{DateTime.Now}";
            UdpSender.SendMessage(message, ipAddress, port);
            dbManager.LogAppHeartbeat(_appId, DateTime.Now);
            _logger.LogInformation($"Sent: {message}");
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _sendTimer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(cancellationToken);
        }
    }
}