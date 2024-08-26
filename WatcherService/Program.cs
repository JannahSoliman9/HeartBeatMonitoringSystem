using HeartBeat;

namespace WatcherService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureAppConfiguration(conf =>
            {
                conf.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<AppConfig>(context.Configuration.GetSection("AppConfig"));
                services.AddHostedService<Worker>();
            }).UseWindowsService();
    }
    
}