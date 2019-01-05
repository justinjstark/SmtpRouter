﻿using System.Linq;
using MimeKit;

namespace SmtpRouter.Middlewares.Helpers
{
    public static class HeaderFormatter
    {
        /// <summary>
        /// Gets the email headers as plain text
        /// </summary>
        /// <param name="message">The MIME message</param>
        /// <returns>The email headers as plain text</returns>
        public static string GetPlainTextHeaders(MimeMessage message)
        {
            return $@"Original Headers
To: {string.Join(", ", message.To.Mailboxes.Select(m => m.Address))}
CC: {string.Join(", ", message.Cc.Mailboxes.Select(m => m.Address))}
----------------------------------------------------------------------";
        }

        /// <summary>
        /// Gets the email header as HTML
        /// </summary>
        /// <param name="message">The MIME message</param>
        /// <returns>The email headers as HTML</returns>
        public static string GetHtmlHeaders(MimeKit.MimeMessage message)
        {
            return $@"<div>
    <strong>Original Headers</strong>
</div>
<div>To: {string.Join(", ", message.To.Mailboxes.Select(m => m.Address))}</div>
<div>CC: {string.Join(", ", message.Cc.Mailboxes.Select(m => m.Address))}</div>
<hr/>";
        }
    }
}
