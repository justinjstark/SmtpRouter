using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmtpServer;
using SmtpServer.Authentication;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter
{
    public class SmtpServerWithMiddleware
    {
        private readonly SmtpServer.SmtpServer _smtpServer;

        public SmtpServerWithMiddleware(string serverName, IEnumerable<int> ports, IList<ISmtpMiddleware> smtpMiddlewares, ILogger logger = null)
        {
            var userAuthenticator = new DelegatingUserAuthenticator((s, u, p) => {
                s.Properties["Username"] = u;
                return true;
            });

            var options = new SmtpServerOptionsBuilder()
                .ServerName(serverName)
                .Port(ports.ToArray())
                .AuthenticationRequired(false)
                .AllowUnsecureAuthentication()
                .UserAuthenticator(userAuthenticator)
                .MessageStore(new MiddlewareMessageStore(smtpMiddlewares, logger))
                .Logger(new SmtpLoggerWrapper(logger))
                .Build();

            _smtpServer = new SmtpServer.SmtpServer(options);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await _smtpServer.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
