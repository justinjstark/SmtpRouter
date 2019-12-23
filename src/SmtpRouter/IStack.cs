using System.Collections.Generic;

namespace SmtpRouter
{
    public interface IStack
    {
        string Name { get; }
        IList<ISmtpMiddleware> Middlewares { get; }
    }
}
