using Topshelf;

namespace SmtpRouter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<SmtpRouterService>(s =>
                {
                    s.ConstructUsing(name =>
                    {
                        var logger = new ConsoleLogger();

                        return new SmtpRouterService(logger);
                    });
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.SetServiceName("SMTP Router");
                x.SetDisplayName("SMTP Router");
                x.SetDescription("An SMTP server which reroutes emails for a test environment");

                x.RunAsLocalService();
                x.StartAutomatically();
            });
        }
    }
}
