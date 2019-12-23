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
    /// Middleware to reroute an email based on configurable rules
    /// </summary>
    public class Reroute : IMiddleware
    {
        private readonly ICollection<RerouteRule> _rerouteRules;
        private readonly ICollection<string> _defaultReroute;
        private readonly ICollection<Func<string, bool>> _keepAddressPredicates;

        private readonly ILogger _logger;

        /// <summary>
        /// Creates middleware to reroute an email based on configurable rules
        /// </summary>
        /// <param name="rerouteRules">Rules specifying which emails route to which addresses</param>
        /// <param name="defaultReroute">The default route if no route is matched. If null, an exception will be thrown.</param>
        /// <param name="keepAddressPredicates">Predicates which specify which original addresses to not remove from the message</param>
        /// <param name="logger">The optional logger to use</param>
        public Reroute(ICollection<RerouteRule> rerouteRules, ICollection<string> defaultReroute = null, ICollection<Func<string, bool>> keepAddressPredicates = null,
            ILogger logger = null)
        {
            _rerouteRules = rerouteRules;
            _defaultReroute = defaultReroute;
            _keepAddressPredicates = keepAddressPredicates;
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction,
            CancellationToken cancellationToken = new CancellationToken())
        {
            _logger?.Log(LogLevel.Information, "Rerouting message");

            try
            {
                var routes = _rerouteRules.Where(r => r.Rule(message, context, transaction)).ToArray();

                _logger?.Log(LogLevel.Information, $"Found {routes.Length} matching routes: {string.Join(", ", routes.Select(r => r.Name))}");

                ICollection<string> toEmails = routes.SelectMany(r => r.ToEmails)
                    .Distinct(new EmailEqualityComparer())
                    .ToArray();

                _logger?.Log(LogLevel.Information, $"Rerouting email to {string.Join(", ", toEmails)}");

                if (!toEmails.Any())
                {
                    if (_defaultReroute == null)
                    {
                        throw new Exception("No route found and no default route specified");
                    }

                    _logger?.Log(LogLevel.Information, "No route found. Using default route.");
                    toEmails = _defaultReroute;
                }

                var toKeep = message.To.Mailboxes.Where(m => _keepAddressPredicates != null && _keepAddressPredicates.Any(p => p(m.ToString()))).ToList();

                if(toKeep.Any())
                {
                    _logger?.Log(LogLevel.Information, $"Keeping To addresses {string.Join(", ", toKeep)}");
                }

                var ccKeep = message.Cc.Mailboxes.Where(m => _keepAddressPredicates != null && _keepAddressPredicates.Any(p => p(m.ToString()))).ToList();

                if(ccKeep.Any())
                {
                    _logger?.Log(LogLevel.Information, $"Keeping CC addresses {string.Join(", ", ccKeep)}");
                }

                var bccKeep = message.Bcc.Mailboxes.Where(m => _keepAddressPredicates != null && _keepAddressPredicates.Any(p => p(m.ToString()))).ToList();

                if (ccKeep.Any())
                {
                    _logger?.Log(LogLevel.Information, $"Keeping BCC addresses {string.Join(", ", bccKeep)}");
                }

                message.To.Clear();
                message.Cc.Clear();
                message.Bcc.Clear();

                message.To.AddRange(toKeep);
                message.Cc.AddRange(ccKeep);
                message.Bcc.AddRange(bccKeep);

                message.To.AddRange(toEmails.Select(r => new MailboxAddress(r)));
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, exception, "Unable to reroute message");
                throw; //If error, abort so external customers don't get messages
            }

            return await Task.FromResult(message).ConfigureAwait(false);
        }
    }

    public class RerouteRule
    {
        public string Name { get; }
        public Func<MimeMessage, ISessionContext, IMessageTransaction, bool> Rule { get; }
        public ICollection<string> ToEmails { get; }

        public RerouteRule(string name, Func<MimeMessage, ISessionContext, IMessageTransaction, bool> rule, ICollection<string> toEmails)
        {
            Name = name;
            Rule = rule;
            ToEmails = toEmails;
        }
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : default(TValue);
        }
    }

    public class EmailEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return string.Equals(ExtractEmail(x), ExtractEmail(y), StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractEmail(string emailString)
        {
            var mailboxAddress = new MailboxAddress(emailString);

            return mailboxAddress.Address;
        }

        public int GetHashCode(string obj)
        {
            return ExtractEmail(obj).GetHashCode();
        }
    }
}