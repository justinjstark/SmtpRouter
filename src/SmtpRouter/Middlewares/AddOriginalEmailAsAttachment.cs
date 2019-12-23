using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middlewares
{
    /// <summary>
    /// Middleware to attach the original email as an EML file
    /// </summary>
    public class AddOriginalEmailAsAttachment : IMiddleware
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates middleware to attach the original email as an EML file
        /// </summary>
        /// <param name="logger">An optional logger to use</param>
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

                //Add the attachment to the existing parent-level multipart if it exists.
                //Otherwise create a parent multipart and put the message body and attachment in it.
                if(message.Body is Multipart multipart)
                {
                    multipart.Add(attachment);
                }
                else
                {
                    message.Body = new Multipart("mixed") { message.Body, attachment };
                }
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
