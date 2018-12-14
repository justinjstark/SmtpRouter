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
    public class Send : ISmtpMiddleware
    {
        private readonly string _host;
        private readonly int _port;
        private readonly SecureSocketOptions _secureSocketOptions;

        private readonly ILogger _logger;

        public Send(string host, int port, SecureSocketOptions secureSocketOptions, ILogger logger = null)
        {
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
                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
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
