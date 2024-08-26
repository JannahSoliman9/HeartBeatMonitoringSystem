using HeartBeat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting;
using ServiceApp2;
using System.IO;
namespace ServiceApp2
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