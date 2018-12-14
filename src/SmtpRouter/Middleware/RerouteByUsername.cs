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
    public class RerouteByUsername : ISmtpMiddleware
    {
        private readonly IDictionary<string, ICollection<string>> _reroutes;
        private readonly ICollection<string> _defaultReroute;
        private readonly ICollection<Func<string, bool>> _keepAddressPredicates;

        private readonly ILogger _logger;

        /// <summary>
        /// Reroutes an email message based on the username authenticated with the SMTP server
        /// </summary>
        /// <param name="reroutes">Which SMTP usernames route to which emails</param>
        /// <param name="defaultReroute">The default route if the username does not match a route. If null, an exception will be thrown.</param>
        /// <param name="keepAddressPredicates">Predicates which specify which original addresses to not remove from the message</param>
        /// <param name="logger">The optional logger to use</param>
        public RerouteByUsername(IDictionary<string, ICollection<string>> reroutes, ICollection<string> defaultReroute = null, ICollection<Func<string, bool>> keepAddressPredicates = null,
            ILogger logger = null)
        {
            _reroutes = reroutes;
            _defaultReroute = defaultReroute;
            _keepAddressPredicates = keepAddressPredicates;
            _logger = logger;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction,
            CancellationToken cancellationToken = new CancellationToken())
        {
            _logger?.Log(LogLevel.Information, "Rerouting message by username");

            try
            {
                var username = context.Properties.GetValueOrDefault("Username") as string;

                ICollection<string> route;

                if (username == null)
                {
                    if(_defaultReroute == null)
                    {
                        throw new Exception("No SMTP username and no default route specified");
                    }
                    
                    _logger?.Log(LogLevel.Information, "No SMTP username. Using default route.");
                    route = _defaultReroute;
                }
                else
                {
                    route = _reroutes.GetValueOrDefault(username);

                    if (route == null)
                    {
                        if (_defaultReroute == null)
                        {
                            throw new Exception($"No route found for SMTP username {username} and no default route specified");
                        }

                        _logger?.Log(LogLevel.Information, $"No route found for SMTP username {username}. Using default route.");
                        route = _defaultReroute;
                    }
                }

                var toKeep = message.To.Mailboxes.Where(m => _keepAddressPredicates != null && _keepAddressPredicates.Any(p => p(m.ToString()))).ToList();
                var ccKeep = message.Cc.Mailboxes.Where(m => _keepAddressPredicates != null && _keepAddressPredicates.Any(p => p(m.ToString()))).ToList();
                var bccKeep = message.Bcc.Mailboxes.Where(m => _keepAddressPredicates != null && _keepAddressPredicates.Any(p => p(m.ToString()))).ToList();

                message.To.Clear();
                message.Cc.Clear();
                message.Bcc.Clear();

                message.To.AddRange(toKeep);
                message.Cc.AddRange(ccKeep);
                message.Bcc.AddRange(bccKeep);

                message.To.AddRange(route.Select(r => new MailboxAddress(r)));
            }
            catch (Exception exception)
            {
                _logger?.Log(LogLevel.Error, exception, "Unable to reroute message by username");
                throw; //If error, abort so external customers don't get messages
            }

            return await Task.FromResult(message).ConfigureAwait(false);
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
}
