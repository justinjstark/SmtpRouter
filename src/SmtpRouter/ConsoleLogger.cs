using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SmtpRouter
{
    public class ConsoleLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var builder = new StringBuilder();
            builder.AppendLine(formatter(state, exception));

            if (exception != null)
            {
                builder.AppendLine(exception.ToString());
            }

            Console.Write(builder.ToString());
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
