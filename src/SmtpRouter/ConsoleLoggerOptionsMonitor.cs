using System;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace SmtpRouter
{
    public class ConsoleLoggerOptionsMonitor : IOptionsMonitor<ConsoleLoggerOptions>
    {
        public ConsoleLoggerOptions CurrentValue { get; }
            
        public ConsoleLoggerOptionsMonitor()
        {
            CurrentValue = new ConsoleLoggerOptions
            {
                IncludeScopes = true,
                DisableColors = false
            };
        }

        public ConsoleLoggerOptions Get(string name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<ConsoleLoggerOptions, string> listener)
        {
            return null;
        }
    }
}
