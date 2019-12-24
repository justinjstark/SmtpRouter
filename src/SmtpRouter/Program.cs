using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmtpServer.Storage;

namespace SmtpRouter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<SmtpRouterService>();
                    services.AddTransient<IMessageStoreFactory, MiddlewareMessageStoreFactory>();
                    services.AddTransient<IMessageStore, MiddlewareMessageStore>();
                    services.AddTransient<SmtpLoggerWrapper>();
                    services.AddTransient<IStack, Stacks.Examples.RerouteAndLogStack>();
                    services.AddSmtpRouter();
                })
                .ConfigureLogging(builder =>
                {
                    builder.AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddFilter("SmtpRouter", LogLevel.Debug)
                        .AddConsole()
                        .AddEventLog();
                });
    }
}
