using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpRouter.Middleware.Helpers;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middleware
{
    /// <summary>
    /// Middleware to inject the message headers into the message body
    /// </summary>
    public class InjectHeadersIntoMessage : ISmtpMiddleware
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates middleware to inject the message headers into the message body
        /// </summary>
        /// <param name="logger">An optional logger to use</param>
        public InjectHeadersIntoMessage(ILogger logger = null)
        {
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            _logger?.Log(LogLevel.Information, "Injecting headers into message");

            try
            {
                //Add headers to the top of the message
                var bodyTextParts = message.BodyParts.OfType<TextPart>().ToList();
                var htmlBody = bodyTextParts.LastOrDefault(btp => btp.IsHtml);
                var textBody = bodyTextParts.LastOrDefault(btp => !btp.IsHtml);

                if (htmlBody != null)
                {
                    var bodyTagLocation = htmlBody.Text.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
                    var insertLocation = bodyTagLocation == -1 ? 0 : bodyTagLocation + 6;
                    htmlBody.Text = htmlBody.Text.Insert(insertLocation, HeaderFormatter.GetHtmlHeaders(message));
                }

                if (textBody != null)
                {
                    textBody.Text = HeaderFormatter.GetPlainTextHeaders(message) + textBody.Text;
                }
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, exception, "Error injecting headers into message");
                //Don't throw, continue routing message
            }

            return await Task.FromResult(message).ConfigureAwait(false);
        }
    }
}
