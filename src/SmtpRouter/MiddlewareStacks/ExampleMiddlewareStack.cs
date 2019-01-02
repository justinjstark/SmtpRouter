﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
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
                new Log(logger, message => $"Received message for {string.Join(", ", message.To)}"),
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
                new Log(logger)
                //In the real-world, you would replace ConsoleWriter with a Send middleware that resends the
                //message after it has been manipulated.
                //new Send(
                //    smtpClientFactory: () => {
                //        return new SmtpClient
                //        {
                //            ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true,
                //            Timeout = 10 * 1000
                //        };
                //    },
                //    host: "realsmtpserver.com",
                //    port: 25,
                //    secureSocketOptions: SecureSocketOptions.None,
                //    logger: logger)
            };
        }

        private static bool EmailHasDomain(string email, string domain)
        {
            return Regex.IsMatch(email, $@".*@{Regex.Escape(domain)}>?\s*$", RegexOptions.IgnoreCase);
        }
    }
}
