﻿using System.Threading;
using System.Threading.Tasks;
using SmtpServer;

namespace SmtpRouter
{
    public interface IMiddleware
    {
        Task<MimeKit.MimeMessage> RunAsync(MimeKit.MimeMessage message, ISessionContext context,
            IMessageTransaction transaction, CancellationToken stoppingToken);
    }
}
