using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HeartBeat;
using System.Text.Json;
using System.IO;
using System.Windows.Threading;
using System.Net;
using UdpApp7;

namespace UdpApp7
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string ipAddress = "127.0.0.1";
        const int port = 5000;

        private DispatcherTimer _clockTimer;
        private string _appId;
        private int _intervalSeconds;
        private DbManager dbManager = new DbManager();


        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            SetupClock();
            StartSendingMessages();
        }

        private AppConfig ReadConfig(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<AppConfig>(jsonString); //put the stuff in the json file into the appconfig object that will be returned 
        }
        private void LoadConfig()
        {
            var config = ReadConfig("appSettings.json");
            if (config == null)
            {
                Console.WriteLine("error reading from json file");
            }
            else
            {
                _appId = config.AppId;
                _intervalSeconds = config.IntervalInSeconds;
            }
        }

        private void SetupClock()
        {
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1); //timer ticks every second 
            _clockTimer.Tick += (sender, e) =>
            {
                clockDisplay.Content = DateTime.Now.ToString("hh:mm:ss tt");

            };
            _clockTimer.Start();
        }
        private void StartSendingMessages()
        {
            var sendTimer = new DispatcherTimer();
            sendTimer.Interval = TimeSpan.FromSeconds(_intervalSeconds); //timer ticks every second 
            sendTimer.Tick += (sender, e) =>
            {
                Task.Run(() =>
                {
                    string message = $"{_appId}-{DateTime.Now}";
                    UdpSender.SendMessage(message, ipAddress, port);
                    dbManager.LogAppHeartbeat(_appId, DateTime.Now);
                    Console.WriteLine($"Sent: {message}");

                });
            };
            sendTimer.Start();
        }

        private void HangClick(object sender, RoutedEventArgs e)
        {
            while (true)
            {

            }
        }
    }
}
