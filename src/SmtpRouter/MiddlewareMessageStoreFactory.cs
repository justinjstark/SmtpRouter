using System;
using Microsoft.Extensions.DependencyInjection;
using SmtpServer;
using SmtpServer.Storage;

namespace SmtpRouter
{
    public class MiddlewareMessageStoreFactory : IMessageStoreFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MiddlewareMessageStoreFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMessageStore CreateInstance(ISessionContext context)
        {
            return _serviceProvider.GetRequiredService<IMessageStore>();
        }
    }
}
