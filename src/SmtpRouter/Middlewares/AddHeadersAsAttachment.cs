using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpRouter.Middlewares.Helpers;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middlewares
{
    /// <summary>
    /// Middleware to add message headers as an attachment
    /// </summary>
    public class AddHeadersAsAttachment : IMiddleware
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates middleware to add message headers as an attachment
        /// </summary>
        /// <param name="logger">An optional logger to use</param>
        public AddHeadersAsAttachment(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            _logger?.Log(LogLevel.Information, "Adding headers as an attachment");

            try
            {
                var stream = new MemoryStream();
                var streamWriter = new StreamWriter(stream);
                streamWriter.Write(HeaderFormatter.GetPlainTextHeaders(message));
                streamWriter.Flush();
                stream.Position = 0;

                var attachment = new MimePart("text", "plain")
                {
                    Content = new MimeContent(stream),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = "OriginalHeaders.txt"
                };

                //Add the attachment to the existing parent-level multipart if it exists.
                //Otherwise create a parent multipart and put the message body and attachment in it.
                if (message.Body is Multipart multipart)
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
                _logger?.Log(LogLevel.Error, exception, "Error adding headers as an attachment");
                //Don't throw, continue routing message
            }

            return await Task.FromResult(message).ConfigureAwait(false);
        }
    }
}
