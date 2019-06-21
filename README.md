# SmtpRouter
An SMTP server that captures and reroutes emails for test environments

[![Build status](https://ci.appveyor.com/api/projects/status/cx8cjr6ejyboupgb/branch/master?svg=true)](https://ci.appveyor.com/project/justinjstark/smtprouter/branch/master)

# Usage
From the src directory:
```
dotnet build
dotnet run --project SmtpRouter/SmtpRouter.csproj
```

This will start an SMTP server on localhost listening on port 25. By default the router manipulates the messages and logs the final message to the console. You can see the predefined step in the [ExampleStack](https://github.com/justinjstark/SmtpRouter/blob/master/src/SmtpRouter/Stacks/ExampleStack.cs#L12).

# Demo
There is a demo project you can use to send emails to test the router. It is configured to send messages to localhost:25.

In another terminal window while the SmtpRouter is running, from the src directory:
```
dotnet run --project SmtpRouter.Demo.Client/SmtpRouter.Demo.Client.csproj
```

# Configuration
SmtpRouter can be configured by code. Since the steps are highly customizable and should rarely change, it does not make sense to support configuration files. See [ExampleStack](https://github.com/justinjstark/SmtpRouter/blob/master/src/SmtpRouter/Stacks/ExampleStack.cs#L12) for a configuration example.

While you can write your own middleware by inheriting from ISmtpMiddleware and implementing RunAsync, there are [several configurable middlewares already defined](https://github.com/justinjstark/SmtpRouter/tree/master/src/SmtpRouter/Middlewares).

The simplest useful example for a test environment is to reroute messages to a mailbox and resend them.
```csharp
var stack = new List<ISmtpMiddleware>
{
    new RerouteTo(addresses: new [] { "mymailbox@mydomain.com" }),
    new Send(
        create: async ct => await Task.FromResult(new SmtpClient()),
        connect: async (smtpClient, ct) =>
        {
            await smtpClient.ConnectAsync("mysmtpserver.com", 587, SecureSocketOptions.StartTls, ct);
        },
        authenticate: async (smtpClient, ct) =>
        {
            await smtpClient.AuthenticateAsync("username", "password", ct);
        })
}
```

# How It Works
SmtpRouter is a recipe of three projects: [jstedfast/MimeKit](https://github.com/jstedfast/MimeKit), [jstedfast/MailKit](https://github.com/jstedfast/MailKit), and [cosullivan/SmtpServer](https://github.com/cosullivan/SmtpServer).

SmtpServer allows us to run an SMTP server. SmtpRouter leverages the ability to add a custom MessageStore to intercept emails. The MiddlewareMessageStore simply runs a stack of configured middlewares, each of which manipulates or acts on the email. MimeKit is used to to parse and manipulate the emails. Mailkit is used to resend them.

# Logging
By default SmtpRouter uses ConsoleLogger which logs all messages to the console. Each predefined middleware can be configured with a logger implementing Microsoft.Extensions.Logging.ILogger. A custom logger can be written to log messages to a database or filesystem and different loggers can be used with different middlewares.
