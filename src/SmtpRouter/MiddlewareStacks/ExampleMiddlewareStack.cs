using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MailKit.Security;
using SmtpRouter.Middleware;
using Microsoft.Extensions.Logging;

namespace SmtpRouter.MiddlewareStacks
{
    public static class ExampleMiddlewareStack
    {
        public static IList<ISmtpMiddleware> GetStack(ILogger logger = null)
        {
            return new List<ISmtpMiddleware>
            {
                new LogEmailReceived(logger),
                new AddOriginalEmailAsAttachment(logger),
                new InjectHeadersIntoMessage(logger),
                new RerouteByUsername(
                    reroutes: new Dictionary<string, ICollection<string>>
                    {
                        { "application1", new [] { "application1@mydomain.com" } },
                        { "application2", new [] { "application2@mydomain.com" } }
                    },
                    defaultReroute: new [] { "default@mydomain.com" },
                    keepAddressPredicates: new []
                    {
                        new Func<string, bool>(e => EmailHasDomain(e, "mydomain.com")),
                        new Func<string, bool>(e => EmailHasDomain(e, "anotherdomain.net"))
                    },
                    logger: logger),
                new Send(
                    host: "realsmtpserver.com",
                    port: 25,
                    secureSocketOptions: SecureSocketOptions.None,
                    logger: logger)
            };
        }

        private static bool EmailHasDomain(string email, string domain)
        {
            return Regex.IsMatch(email, $@".*@{Regex.Escape(domain)}>?\s*$", RegexOptions.IgnoreCase);
        }
    }
}
