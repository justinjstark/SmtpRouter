using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace SmtpRouter
{
    public class MiddlewareMessageStore : IMessageStore
    {
        private readonly IStack _stack;
        private readonly ILogger<MiddlewareMessageStore> _logger;

        public MiddlewareMessageStore(IStack stack, ILogger<MiddlewareMessageStore> logger)
        {
            _stack = stack;
            _logger = logger;
        }

        public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"Using stack {_stack.Name}.");

                MimeMessage message;
                using (var stream = ((ITextMessage)transaction.Message).Content)
                {
                    message = MimeMessage.Load(stream);
                }

                AddBccEmails(message, transaction);

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var middleware in _stack.Middlewares)
                {
                    message = await middleware.RunAsync(message, context, transaction, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, "Email middleware routing failed");
                return new SmtpResponse(SmtpReplyCode.TransactionFailed, exception.Message);
            }

            return SmtpResponse.Ok;
        }

        /*
        * BCC recipients are not part of the MIME message so they are not added to
        * the MimeMessage. See: https://github.com/cosullivan/SmtpServer/issues/35
        * To solve this problem, we add all transaction emails that are not part of
        * the MIME message to the BCC.
        */
        private void AddBccEmails(MimeMessage message, IMessageTransaction transaction)
        {
            var messageEmails = FlattenInternetAddresses(message.To.Union(message.Cc))
                .Select(ma => ma.Address.ToLower());

            var transactionEmails = transaction.To
                .Select(m => m.AsAddress().ToLower())
                .Distinct();

            var bccMailboxAddresses = transactionEmails.Except(messageEmails)
                .Select(s => new MailboxAddress(s));

            message.Bcc.AddRange(bccMailboxAddresses);
        }

        private IEnumerable<MailboxAddress> FlattenInternetAddresses(IEnumerable<InternetAddress> internetAddresses, int maxDepth = 5)
        {
            return internetAddresses.SelectMany(ia => FlattenInternetAddress(ia, maxDepth));
        }

        private IEnumerable<MailboxAddress> FlattenInternetAddress(InternetAddress internetAddresses, int maxDepth, int currentDepth = 1)
        {
            if (currentDepth > maxDepth) return new List<MailboxAddress> { };

            if (internetAddresses.GetType().IsAssignableFrom(typeof(MailboxAddress)))
            {
                return new List<MailboxAddress> { (MailboxAddress)internetAddresses };
            }
            else
            {
                return FlattenInternetAddress(internetAddresses, maxDepth, currentDepth);
            }
        }
    }
}
