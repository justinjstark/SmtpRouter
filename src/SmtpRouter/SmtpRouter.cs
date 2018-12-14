using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmtpRouter
{
    public class SmtpRouter
    {
        private readonly SmtpServerWithMiddleware _smtpServerWithMiddleware;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task _smtpServerTask;

        private readonly ILogger _logger;

        public SmtpRouter(IList<ISmtpMiddleware> smtpMiddlewares, ILogger logger)
        {
            _smtpServerWithMiddleware = new SmtpServerWithMiddleware("localhost", new[] { 25, 587 }, smtpMiddlewares);

            _logger = logger;
        }

        public void Start()
        {
            _logger?.LogTrace("Starting up");
            _smtpServerTask = _smtpServerWithMiddleware.StartAsync(_cancellationTokenSource.Token);
            _logger?.LogTrace("Started");
        }

        public void Stop()
        {
            _logger?.LogTrace("Stopping");

            _cancellationTokenSource.Cancel();

            try
            {
                _smtpServerTask.Wait();
            }
            catch (Exception exception)
            {
                _logger?.LogCritical(exception, "Critical error. See inner exception.");
            }
            
            _logger?.LogTrace("Stopped");
        }
    }
}
