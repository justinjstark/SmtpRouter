using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middlewares
{
    /// <summary>
    /// Middleware to send the email using MailKit
    /// </summary>
    public class Send : IMiddleware
    {
        private readonly Func<CancellationToken, Task<SmtpClient>> _create;
        private readonly Func<SmtpClient, CancellationToken, Task> _connect;
        private readonly Func<SmtpClient, CancellationToken, Task> _authenticate;

        private readonly ILogger _logger;

        /// <summary>
        /// Creates middleware to send the email
        /// </summary>
        /// <param name="create">An asynchronous method to create the MailKit SMTP client</param>
        /// <param name="connect">An asynchronous method to connect to the server with the provided MailKit SMTP client</param>
        /// <param name="authenticate">An asynchronous method to authenticate with the provided MailKit SMTP client</param>
        /// <param name="logger">An optional logger to use</param>
        public Send(Func<CancellationToken, Task<SmtpClient>> create, Func<SmtpClient, CancellationToken, Task> connect, Func<SmtpClient, CancellationToken, Task> authenticate = null, ILogger logger = null)
        {
            _create = create;
            _connect = connect;
            _authenticate = authenticate;
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            _logger?.Log(LogLevel.Information, "Sending message");

            try
            {
                using (var smtpClient = await _create(cancellationToken))
                {
                    await _connect(smtpClient, cancellationToken);

                    if (_authenticate != null)
                    {
                        await _authenticate(smtpClient, cancellationToken);
                    }
                    
                    await smtpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, exception, "Unable to send message");
                throw;
            }

            return message;
        }
    }
}
