using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middleware
{
    /// <summary>
    /// Middleware to send the email using MailKit
    /// </summary>
    public class Send : ISmtpMiddleware
    {
        private readonly Func<Task<SmtpClient>> _smtpClientFactoryAsync;

        private readonly string _host;
        private readonly int _port;
        private readonly SecureSocketOptions _secureSocketOptions;

        private readonly ILogger _logger;

        /// <summary>
        /// Creates middleware to send the email
        /// </summary>
        /// <param name="smtpClientFactoryAsync">A factory to create a configured MailKit SMTP Client</param>
        /// <param name="host">The SMTP host</param>
        /// <param name="port">The SMTP port</param>
        /// <param name="secureSocketOptions">Secure socket options from MailKit</param>
        /// <param name="logger">An optional logger to use</param>
        public Send(Func<Task<SmtpClient>> smtpClientFactoryAsync, string host, int port, SecureSocketOptions secureSocketOptions, ILogger logger = null)
        {
            _smtpClientFactoryAsync = smtpClientFactoryAsync;
            _host = host;
            _port = port;
            _secureSocketOptions = secureSocketOptions;
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            _logger?.Log(LogLevel.Information, $"Sending message to {_host}:{_port}");

            try
            {
                using (var smtpClient = await _smtpClientFactoryAsync())
                {
                    await smtpClient.ConnectAsync(_host, _port, _secureSocketOptions, cancellationToken).ConfigureAwait(false);

                    await smtpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, exception, $"Unable to send message to {_host}:{_port}");
                throw;
            }

            return message;
        }
    }
}
