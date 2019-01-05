using System;
using System.Linq;
using System.Text;
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

namespace SmtpRouter.Middlewares
{
    /// <summary>
    /// Middleware to write the current message to the log
    /// </summary>
    public class Log : ISmtpMiddleware
    {
        private const string Indent = "  ";
        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;
        private readonly Func<MimeMessage, ISessionContext, IMessageTransaction, string> _formatter;

        /// <summary>
        /// Creates middleware to write the current message using a custom formatter
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="formatter"></param>
        /// <param name="logLevel"></param>
        public Log(ILogger logger, LogLevel logLevel = LogLevel.Information, Func<MimeMessage, ISessionContext, IMessageTransaction, string> formatter = null)
        {
            _logger = logger;
            _logLevel = logLevel;
            _formatter = formatter ?? DefaultFormatter;
        }

        public async Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext sessionContext, IMessageTransaction messageTransaction, CancellationToken cancellationToken = new CancellationToken())
        {
            _logger.Log(_logLevel, _formatter(message, sessionContext, messageTransaction));

            return await Task.FromResult(message);
        }

        private static string DefaultFormatter(MimeMessage message, ISessionContext sessionContext, IMessageTransaction messageTransaction)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Message received");

            stringBuilder.AppendLine($"{Indent}Headers:");
            var headers = message.Headers;

            foreach (var header in headers)
            {
                stringBuilder.AppendLine($"{Indent}{Indent}{header.Field}: {header.Value}");
            }

            stringBuilder.AppendLine($"{Indent}Body:");

            FormatMimeEntity(stringBuilder, message.Body, $"{Indent}{Indent}");

            return stringBuilder.ToString();
        }

        private static void FormatMimeEntity(StringBuilder stringBuilder, MimeEntity entity, string indent)
        {
            stringBuilder.AppendLine($"{indent}Mime Type: {entity.ContentType.MimeType}");

            indent += Indent;

            if (entity is Multipart multipart)
            {
                foreach (var subentity in multipart)
                {
                    FormatMimeEntity(stringBuilder, subentity, indent);
                }
            }
            else if (entity is TextPart textPart)
            {
                var text = string.Join('\n', textPart.Text.Split("\n").Select(line => $"{indent}{line}"));
                stringBuilder.AppendLine(text);
            }
            else if(entity is MimePart mimePart)
            {
                stringBuilder.AppendLine($"{indent}Attachment: {mimePart.FileName}");
            }
            else
            {
                stringBuilder.AppendLine($"{indent}Unhandled type {entity.GetType()}");
            }
        }
    }
}
