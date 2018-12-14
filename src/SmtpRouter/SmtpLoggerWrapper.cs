using Microsoft.Extensions.Logging;

namespace SmtpRouter
{
    public class SmtpLoggerWrapper : SmtpServer.ILogger
    {
        private readonly ILogger _logger;

        public SmtpLoggerWrapper(ILogger logger)
        {
            _logger = logger;
        }

        public void LogVerbose(string format, params object[] args)
        {
            _logger.Log(LogLevel.Information, format);
        }
    }
}
