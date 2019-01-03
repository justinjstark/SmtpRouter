using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using SmtpRouter.Middleware;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SmtpRouter.MiddlewarePipelines
{
    public static class ExampleMiddlewarePipeline
    {
        public static IList<ISmtpMiddleware> GetPipeline(ILogger logger = null)
        {
            return new List<ISmtpMiddleware>
            {
                new Log(logger, formatter: (m, c, t) => $"Received message for {string.Join(", ", m.To)}"),
                new AddOriginalEmailAsAttachment(logger),
                new InjectHeadersIntoMessage(logger),
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
                    keepAddressPredicates: new []
                    {
                        new Func<string, bool>(e => EmailHasDomain(e, "mydomain.com")),
                        new Func<string, bool>(e => EmailHasDomain(e, "anotherdomain.net"))
                    },
                    logger: logger),
                new Log(logger),
                //In the real-world, you would replace Log with a Send middleware that resends the
                //message after it has been manipulated.
                new Send(
                    create: async ct => await Task.FromResult(new SmtpClient()),
                    connect: async (smtpClient, ct) =>
                    {
                        await smtpClient.ConnectAsync("realsmtpserver.com", 587, SecureSocketOptions.StartTls, ct);
                    },
                    authenticate: async (smtpClient, ct) =>
                    {
                        await smtpClient.AuthenticateAsync("username", "password", ct);
                    })
            };
        }

        private static bool EmailHasDomain(string email, string domain)
        {
            return Regex.IsMatch(email, $@".*@{Regex.Escape(domain)}>?\s*$", RegexOptions.IgnoreCase);
        }
    }
}
