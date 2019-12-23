using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Storage;

namespace SmtpRouter
{
    internal static class ServiceCollectionExtensions
    {
        internal static IServiceCollection AddSmtpRouter(this IServiceCollection services)
        {
            return services.AddTransient(sp =>
            {
                var userAuthenticator = new DelegatingUserAuthenticator((s, u, p) =>
                {
                    /*
                     * Do authentication here or write your own IUserAuthenticatorFactory.
                     * We add the username to the SessionContext. It is used by in the Reroute middleware example.
                     */
                    s.Properties["SmtpUsername"] = u;
                    return true;
                });

                var options = new SmtpServerOptionsBuilder()
                .ServerName("localhost")
                .UserAuthenticator(userAuthenticator)
                .Endpoint(b =>
                {
                    b.Port(25, false);
                    b.AuthenticationRequired(false);
                    b.AllowUnsecureAuthentication(true);
                })
                .MessageStore(sp.GetRequiredService<IMessageStoreFactory>())
                .Logger(sp.GetRequiredService<SmtpLoggerWrapper>())
                .Build();

                return new SmtpServer.SmtpServer(options);
            });
        }
    }
}
