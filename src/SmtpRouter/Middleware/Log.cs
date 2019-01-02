using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

/*
 * This middleware is meant to be extremely flexible. You can do any of these:
 * 
 * new Log(logger)
 *   - Uses the default formatter to write the entire message including header and entire MIME body
 *   - Same as new Log(Log.DefaultLogAction(logger, LogLevel.Information))
 * 
 * new Log(logger, message => $"Received message for {string.Join(", ", message.To)}")
 *   - Writes the message using the provided formatter
 * 
 * new Log(message => { logger.Log(LogLevel.Information, $"Received message for {string.Join(", ", message.To)}"); } )
 *   - Writes the message using the provided log action
 */

namespace SmtpRouter.Middleware
{
    /// <summary>
    /// Middleware to write the current message to the log
    /// </summary>
    public class Log : ISmtpMiddleware
    {
        private const string Indent = "  ";
        private readonly Action<MimeMessage> _logAction;

        /// <summary>
        /// Creates middleware to write the current message to the log
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="logLevel">The log level</param>
        public Log(ILogger logger, LogLevel logLevel = LogLevel.Information)
        {
            _logAction = DefaultLogAction(logger, logLevel);
        }

        /// <summary>
        /// Creates middleware to write the current message using a custom logging action
        /// </summary>
        /// <param name="logAction">The logging action to perform on the message</param>
        public Log(Action<MimeMessage> logAction)
        {
            _logAction = logAction;
        }

        /// <summary>
        /// Creates middleware to write the current message using a custom formatter
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="logFormatter"></param>
        /// <param name="logLevel"></param>
        public Log(ILogger logger, Func<MimeMessage, string> logFormatter, LogLevel logLevel = LogLevel.Information)
        {
            _logAction = message => { logger.Log(logLevel, logFormatter(message)); };
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            _logAction(message);

            return await Task.FromResult(message);
        }

        public static Action<MimeMessage> DefaultLogAction(ILogger logger, LogLevel logLevel)
        {
            return message => DefaultLogAction(logger, logLevel, message);
        }

        private static void DefaultLogAction(ILogger logger, LogLevel logLevel, MimeMessage message)
        {
            logger.Log(logLevel, "Message received");

            logger.Log(logLevel, $"{Indent}Headers:");
            var headers = message.Headers;

            foreach (var header in headers)
            {
                logger.Log(logLevel, $"{Indent}{Indent}{header.Field}: {header.Value}");
            }

            logger.Log(logLevel, $"{Indent}Body:");

            LogMimeEntity(logger, logLevel, message.Body, $"{Indent}{Indent}");
        }

        private static void LogMimeEntity(ILogger logger, LogLevel logLevel, MimeEntity entity, string indent)
        {
            logger.Log(logLevel, $"{indent}Mime Type: {entity.ContentType.MimeType}");

            indent += Indent;

            if (entity is Multipart multipart)
            {
                foreach (var subentity in multipart)
                {
                    LogMimeEntity(logger, logLevel, subentity, indent);
                }
            }
            else if (entity is TextPart textPart)
            {
                var text = string.Join('\n', textPart.Text.Split("\n").Select(line => $"{indent}{line}"));
                logger.Log(logLevel, text);
            }
            else if(entity is MimePart mimePart)
            {
                logger.Log(logLevel, $"{indent}Attachment: {mimePart.FileName}");
            }
            else
            {
                logger.Log(logLevel, $"{indent}Unhandled type {entity.GetType()}");
            }
        }
    }
}
