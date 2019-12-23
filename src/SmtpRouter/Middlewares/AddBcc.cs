using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middlewares
{
    /// <summary>
    /// Middleware to add a recipient as BCC
    /// </summary>
    public class AddBcc : IMiddleware
    {
        private readonly ICollection<string> _bccEmails;

        private readonly ILogger _logger;

        /// <summary>
        /// Creates middleware to add a recipient as BCC
        /// </summary>
        /// <param name="bccEmails">The emails to BCC (can be <![CDATA[somebody@test.com]]> or <![CDATA["Somebody" <somebody@test.com>]]>)</param>
        /// <param name="logger">An optional logger to use</param>
        public AddBcc(ICollection<string> bccEmails, ILogger logger = null)
        {
            _bccEmails = bccEmails;
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            _logger?.Log(LogLevel.Information, $"Adding BCC to {string.Join(", ", _bccEmails)}");

            try
            {
                message.Bcc.AddRange(_bccEmails.Select(bcc => new MailboxAddress(bcc)));
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, exception, $"Error adding BCC to {string.Join(", ", _bccEmails)}");
                //Don't throw, continue routing message
            }

            return await Task.FromResult(message).ConfigureAwait(false);
        }
    }
}
