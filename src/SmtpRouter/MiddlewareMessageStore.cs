using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
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

        public IMessageStore CreateInstance(ISessionContext context)
        {
            return this;
        }

        public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                MimeMessage message;
                using (var stream = ((ITextMessage)transaction.Message).Content)
                {
                    message = MimeMessage.Load(stream);
                }

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
