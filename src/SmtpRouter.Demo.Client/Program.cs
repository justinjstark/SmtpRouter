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
                Console.WriteLine("Press <H> to send an HTML email");
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
                        SendMessage(CreateMessage(true, false));
                        break;
                    case 'z':
                    case 'Z':
                        SendMessage(CreateMessage(true, true));
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

        private static MimeMessage CreateMessage(bool includeHtmlBody, bool includeTextBody)
        {
            var bodyBuilder = new BodyBuilder();
            if (includeTextBody)
            {
                bodyBuilder.TextBody = GetTextBody();
            }
            if (includeHtmlBody)
            {
                bodyBuilder.HtmlBody = GetFullHtmlBody();
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
            return @"<!DOCTYPE html>
<html>
<head>
<title>Testing SMTP Router</title>
</head>
<body>
<h1>Testing SMTP Router</h1>
<p>This is a test email sent by the SMTP Router demo.</p>
<p><a href=""https://github.com/justinjstark/SmtpRouter"">https://github.com/justinjstark/SmtpRouter</a></p>
</body></html>";
        }

        private static string GetTextBody()
        {
            return "This is plain text";
        }
    }
}
