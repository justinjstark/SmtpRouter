using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter.Middleware
{
    public class RerouteTo : ISmtpMiddleware
    {
        private readonly ICollection<string> _rerouteToInternetAddresses;
        private readonly ICollection<Func<string, bool>> _keepAddressPredicates;

        private readonly ILogger _logger;

        /// <summary>
        /// Reroutes an email message to the listed addresses with rules for which addresses to keep
        /// </summary>
        /// <param name="addresses">The internet addresses to reroute the message to a different mailbox</param>
        /// <param name="keepAddressPredicates">Predicates which specify which original addresses to not remove from the message</param>
        /// <param name="logger">The optional logger to use</param>
        public RerouteTo(ICollection<string> addresses, ICollection<Func<string, bool>> keepAddressPredicates,
            ILogger logger = null)
        {
            _rerouteToInternetAddresses = addresses;
            _keepAddressPredicates = keepAddressPredicates;
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            _logger?.Log(LogLevel.Information, "Rerouting message");

            try
            {
                var toKeep = message.To.Mailboxes.Where(m => _keepAddressPredicates != null && _keepAddressPredicates.Any(p => p(m.ToString()))).ToList();
                var ccKeep = message.Cc.Mailboxes.Where(m => _keepAddressPredicates != null && _keepAddressPredicates.Any(p => p(m.ToString()))).ToList();
                var bccKeep = message.Bcc.Mailboxes.Where(m => _keepAddressPredicates != null && _keepAddressPredicates.Any(p => p(m.ToString()))).ToList();

                message.To.Clear();
                message.Cc.Clear();
                message.Bcc.Clear();

                message.To.AddRange(toKeep);
                message.Cc.AddRange(ccKeep);
                message.Bcc.AddRange(bccKeep);

                message.To.AddRange(_rerouteToInternetAddresses.Select(a => new MailboxAddress(a)));
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, exception, "Unable to reroute message");
                throw; //If error, abort so external customers don't get messages
            }

            return await Task.FromResult(message).ConfigureAwait(false);
        }
    }
}
