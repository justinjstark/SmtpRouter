using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middleware
{
    public class LogEmailReceived : ISmtpMiddleware
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Writes a record to the log table when an email is received
        /// </summary>
        /// <param name="logger">The logger implentation to use</param>
        public LogEmailReceived(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            _logger.Log(LogLevel.Information, $"Message received for {message.To}");
            
            return await Task.FromResult(message).ConfigureAwait(false);
        }
    }
}
