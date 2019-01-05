using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmtpRouter
{
    public class SmtpRouterService
    {
        private readonly SmtpRouter _smtpRouter;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task _smtpServerTask;

        private readonly ILogger _logger;

        public SmtpRouterService(ILogger logger)
        {
            _smtpRouter = new SmtpRouter(logger);

            _logger = logger;
        }

        public void Start()
        {
            _logger?.Log(LogLevel.Trace, "Starting up");

            _smtpServerTask = _smtpRouter.StartAsync(_cancellationTokenSource.Token);

            _logger?.Log(LogLevel.Trace, "Started");
        }

        public void Stop()
        {
            _logger?.Log(LogLevel.Trace, "Stopping");

            _cancellationTokenSource.Cancel();

            try
            {
                _smtpServerTask.Wait();
            }
            catch (AggregateException exception)
            {
                var actualExceptions = exception.InnerExceptions.Where(ie => !(ie is TaskCanceledException)).ToList();

                if (actualExceptions.Any())
                {
                    foreach (var innerException in actualExceptions)
                    {
                        _logger?.Log(LogLevel.Critical, innerException, "SMTP server crashed.");
                    }
                    throw;
                }
            }
            catch(Exception exception)
            {
                _logger?.Log(LogLevel.Critical, exception, "SMTP server crashed.");
                throw;
            }
            
            _logger?.Log(LogLevel.Trace, "Stopped");
        }
    }
}
