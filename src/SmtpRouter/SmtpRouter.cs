using System.Threading;
using System.Threading.Tasks;
using SmtpRouter.Stacks;
using SmtpServer;
using SmtpServer.Authentication;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SmtpRouter
{
    public class SmtpRouter
    {
        private readonly SmtpServer.SmtpServer _smtpServer;

        public SmtpRouter(ILogger logger = null)
        {
            var userAuthenticator = new DelegatingUserAuthenticator((s, u, p) => {
                /*
                 * Do authentication here or write your own IUserAuthenticatorFactory.
                 * We add the username to the SessionContext. It is used by in the Reroute middleware example.
                 */
                s.Properties["SmtpUsername"] = u;
                return true;
            });

            /*
             * Build your own custom Stack.
             */
            var stack = ExampleStack.GetStack(logger);

            /*
             * Configure your SMTP server.
             */
            var options = new SmtpServerOptionsBuilder()
                .ServerName("localhost")
                .Port(25, 587)
                .AuthenticationRequired(false)
                .AllowUnsecureAuthentication()
                .UserAuthenticator(userAuthenticator)
                .MessageStore(new MiddlewareMessageStore(stack, logger))
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
