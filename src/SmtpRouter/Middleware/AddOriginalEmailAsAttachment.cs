using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middleware
{
    public class AddOriginalEmailAsAttachment : ISmtpMiddleware
    {
        private readonly ILogger _logger;

        public AddOriginalEmailAsAttachment(ILogger logger = null)
        {
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            _logger?.Log(LogLevel.Information, "Adding original email as attachment");

            try
            {
                var stream = new MemoryStream();

                message.WriteTo(stream, cancellationToken);

                var attachment = new MimePart("text", "plain")
                {
                    Content = new MimeContent(stream),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = "OriginalEmail.eml"
                };

                message.Body = new Multipart("mixed") {message.Body, attachment};
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, exception, "Error adding original email as attachment");
                //Don't throw, continue routing message
            }

            return await Task.FromResult(message).ConfigureAwait(false);
        }
    }
}
