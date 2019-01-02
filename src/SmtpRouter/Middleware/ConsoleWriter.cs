using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using SmtpServer;

namespace SmtpRouter.Middleware
{
    /// <summary>
    /// Middleware to write the current message state including header and MIME body to the console
    /// </summary>
    public class ConsoleWriter : ISmtpMiddleware
    {
        private const string Indent = "  ";

        public Task<MimeMessage> RunAsync(MimeMessage message, ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            Console.WriteLine("MESSAGE:");

            Console.WriteLine($"{Indent}Headers:");
            var headers = message.Headers;

            foreach(var header in headers)
            {
                Console.WriteLine($"{Indent}{Indent}{header.Field}: {header.Value}");
            }

            Console.WriteLine($"{Indent}Body:");

            WriteMimeEntities(message.Body, $"{Indent}{Indent}");
            
            return Task.FromResult(message);
        }

        private static void WriteMimeEntities(MimeEntity entity, string indent)
        {
            Console.WriteLine($"{indent}Mime Type: {entity.ContentType.MimeType}");

            indent += Indent;

            if (entity is Multipart multipart)
            {
                foreach (var subentity in multipart)
                {
                    WriteMimeEntities(subentity, indent);
                }
            }
            else if (entity is TextPart textPart)
            {
                var text = string.Join('\n', textPart.Text.Split("\n").Select(line => $"{indent}{line}"));
                Console.WriteLine(text);
            }
            else if(entity is MimePart mimePart)
            {
                Console.WriteLine($"{indent}Attachment: {mimePart.FileName}");
            }
            else
            {
                Console.WriteLine($"{indent}Unhandled type {entity.GetType()}");
            }
        }
    }
}
