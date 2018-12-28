using System;
using System.Text;
using MailKit.Net.Smtp;
using MimeKit;
using SmtpRouter.Demo.Client.Properties;

namespace SmtpRouter.Demo.Client
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var quit = false;
            while (!quit)
            {
                Console.WriteLine("Press <T> to send a plain text email");
                Console.WriteLine("Press <H> to send a full HTML email");
                Console.WriteLine("Press <B> to send a basic HTML email");
                Console.WriteLine("Press <Z> to send an email with plain text and HTML");
                Console.WriteLine("Press <Q> to quit");

                var key = Console.ReadKey();

                switch (key.KeyChar)
                {
                    case 't':
                    case 'T':
                        SendMessage(CreateMessage(false, true));
                        break;
                    case 'h':
                    case 'H':
                        SendMessage(CreateMessage(true, false, true));
                        break;
                    case 'b':
                    case 'B':
                        SendMessage(CreateMessage(true, false, false));
                        break;
                    case 'z':
                    case 'Z':
                        SendMessage(CreateMessage(true, true, true));
                        break;
                    case 'q':
                    case 'Q':
                        quit = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private static void SendMessage(MimeMessage message)
        {
            using (var smtpClient = new SmtpClient())
            {
                smtpClient.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                smtpClient.Connect("localhost", 587);
                smtpClient.Authenticate("App1", "");

                smtpClient.Send(message);
            }

            Console.WriteLine("\nMessage sent!\n");
        }

        private static MimeMessage CreateMessage(bool includeHtmlBody, bool includeTextBody, bool fullHtml = true)
        {
            var bodyBuilder = new BodyBuilder();
            if (includeTextBody)
            {
                bodyBuilder.TextBody = GetTextBody();
            }
            if (includeHtmlBody)
            {
                bodyBuilder.HtmlBody = fullHtml ? GetFullHtmlBody() : GetSimpleHtmlBody();
            }

            bodyBuilder.Attachments.Add("Tux.png", Resources.Tux);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("no-reply@mydomain.com"));
            message.To.Add(new MailboxAddress(Encoding.UTF8, "Somebody", "test@mydomain.com"));
            message.To.Add(new MailboxAddress("test@somebodyelsesdomain.org"));
            message.Cc.Add(new MailboxAddress("test2@mydomain.org"));
            message.Bcc.Add(new MailboxAddress("bcc@test.com"));
            message.Subject = "Test SMTP Router";
            message.Body = bodyBuilder.ToMessageBody();

            return message;
        }

        private static string GetFullHtmlBody()
        {
            return $"<html><body>{GetSimpleHtmlBody()}</body></html>";
        }

        private static string GetSimpleHtmlBody()
        {
            return "<h1>This is a title</h1><div><b>This is bold</b></div>";
        }

        private static string GetTextBody()
        {
            return "This is plain text";
        }
    }
}
