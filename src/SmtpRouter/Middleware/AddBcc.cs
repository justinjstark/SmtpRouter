using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middleware
{
    public class AddBcc : ISmtpMiddleware
    {
        private readonly ICollection<InternetAddress> _bccInternetAddresses;

        private readonly ILogger _logger;

        /// <summary>
        /// Reroutes an email message
        /// </summary>
        /// <param name="bccInternetAddresses">The internet addresses to add to the message BCC</param>
        /// <param name="logger">An optional logger to use</param>
        public AddBcc(ICollection<InternetAddress> bccInternetAddresses, ILogger logger = null)
        {
            _bccInternetAddresses = bccInternetAddresses;
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            _logger?.Log(LogLevel.Information, $"Adding BCC to {_bccInternetAddresses}");

            try
            {
                message.Bcc.AddRange(_bccInternetAddresses);
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, exception, $"Error adding BCC to {_bccInternetAddresses}");
                //Don't throw, continue routing message
            }

            return await Task.FromResult(message).ConfigureAwait(false);
        }
    }
}
