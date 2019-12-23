using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmtpRouter
{
    public class SmtpRouterService : BackgroundService
    {
        private readonly SmtpServer.SmtpServer _smtpServer;
        private readonly ILogger _logger;

        public SmtpRouterService(SmtpServer.SmtpServer smtpServer, ILogger<SmtpRouterService> logger)
        {
            _smtpServer = smtpServer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SMTP Server started.");

            await _smtpServer.StartAsync(stoppingToken).ConfigureAwait(false);

            _logger.LogInformation("SMTP Server stopped.");
        }
    }
}
