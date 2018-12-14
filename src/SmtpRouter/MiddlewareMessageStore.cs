using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter
{
    public class MiddlewareMessageStore : IMessageStore, IMessageStoreFactory
    {
        private readonly ILogger _logger;
        private readonly IList<ISmtpMiddleware> _smtpMiddlewares;

        public MiddlewareMessageStore(IList<ISmtpMiddleware> smtpMiddlewares, ILogger logger)
        {
            _smtpMiddlewares = smtpMiddlewares;
            _logger = logger;
        }

        private static MimeKit.MimeMessage LoadMimeKitMessage(string s)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            streamWriter.Write(s);
            streamWriter.Flush();
            stream.Position = 0;

            return MimeKit.MimeMessage.Load(stream);
        }

        public IMessageStore CreateInstance(ISessionContext context)
        {
            return this;
        }

        public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                var stream = ((ITextMessage)transaction.Message).Content;

                var message = MimeKit.MimeMessage.Load(stream);

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var middleware in _smtpMiddlewares)
                {
                    message = await middleware.RunAsync(message, context, transaction, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, "Email middleware routing failed");
                return new SmtpResponse(SmtpReplyCode.TransactionFailed, exception.Message);
            }

            return SmtpResponse.Ok;
        }
    }
}
