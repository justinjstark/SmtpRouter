using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SmtpRouter.Middlewares;
using Microsoft.Extensions.Logging;

namespace SmtpRouter.Stacks.Examples
{
    public class RerouteAndLogStack : IStack
    {
        private readonly ILogger _logger;

        public RerouteAndLogStack(ILogger<RerouteAndLogStack> logger)
        {
            _logger = logger;
        }

        public string Name => "Example Stack";

        public IList<IMiddleware> Middlewares =>
            new List<IMiddleware>
            {
                new Log(_logger, formatter: (m, c, t) => $"Received message for {string.Join(", ", m.To)}"),
                new AddOriginalEmailAsAttachment(_logger),
                new InjectHeadersIntoMessage(_logger),
                new Reroute(
                    rerouteRules: new[]
                    {
                        //Reroute the message based on the SMTP username. This is one method to reroute emails
                        //to different mailboxes depending on who sends them.
                        new RerouteRule(
                            name: "App1",
                            rule: (m, c, t) => c.Properties["SmtpUsername"] as string == "App1",
                            toEmails: new [] { "app1@mydomain.com" })
                    },
                    defaultReroute: new [] { "default@mydomain.com" },
                    keepAddressPredicates: new Func<string, bool>[]
                    {
                        e => EmailHasDomain(e, "mydomain.com"),
                        e => EmailHasDomain(e, "anotherdomain.net")
                    },
                    logger: _logger),
                new Log(_logger)
                //In the real-world, you would replace Log with a Send middleware that resends the
                //message after it has been manipulated.
                //new Send(
                //    create: async ct => await Task.FromResult(new SmtpClient()),
                //    connect: async (smtpClient, ct) =>
                //    {
                //        await smtpClient.ConnectAsync("realsmtpserver.com", 587, SecureSocketOptions.StartTls, ct);
                //    },
                //    authenticate: async (smtpClient, ct) =>
                //    {
                //        await smtpClient.AuthenticateAsync("username", "password", ct);
                //    },
                //    logger: logger)
            };

        private static bool EmailHasDomain(string email, string domain)
        {
            return Regex.IsMatch(email, $@".*@{Regex.Escape(domain)}>?\s*$", RegexOptions.IgnoreCase);
        }
    }
}
