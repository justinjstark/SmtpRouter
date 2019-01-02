using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middleware
{
    /// <summary>
    /// Middleware to log when a message is received
    /// </summary>
    public class LogEmailReceived : ISmtpMiddleware
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Logs when a message is received
        /// </summary>
        /// <param name="logger">An optional logger to use</param>
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
