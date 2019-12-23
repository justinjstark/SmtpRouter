# SmtpRouter
An SMTP server that captures and reroutes emails for test environments

[![Build status](https://ci.appveyor.com/api/projects/status/cx8cjr6ejyboupgb/branch/master?svg=true)](https://ci.appveyor.com/project/justinjstark/smtprouter/branch/master)

# Usage
From the src directory:
```
dotnet build
dotnet run --project SmtpRouter/SmtpRouter.csproj
```

This will start an SMTP server on localhost listening on port 25. By default the router manipulates the messages and logs the final message to the console. You can see the predefined steps in the [RerouteAndLogStack](https://github.com/justinjstark/SmtpRouter/blob/master/src/SmtpRouter/Stacks/Examples/RerouteAndLogStack.cs).

# Demo
There is a demo project you can use to send emails to test the router. It is configured to send messages to `localhost:25`.

In another terminal window while the SmtpRouter is running, from the src directory run:
```
dotnet run --project SmtpRouter.Demo.Client/SmtpRouter.Demo.Client.csproj
```

# Configuration
SmtpRouter is highly configurable by code. See the example [RerouteAndLogStack](https://github.com/justinjstark/SmtpRouter/blob/master/src/SmtpRouter/Stacks/Examples/RerouteAndLogStack.cs) for a configuration example.

While you can write your own middleware by inheriting from ISmtpMiddleware and implementing RunAsync, there are [several configurable middlewares already defined](https://github.com/justinjstark/SmtpRouter/tree/master/src/SmtpRouter/Middlewares).

The simplest useful example for a test environment is to reroute messages to a mailbox and resend them.
```csharp
public class RerouteAndSendStack : IStack
{
    public string Name => "Reroute and Send";

    public IList<ISmtpMiddleware> Middlewares =>
        new List<ISmtpMiddleware>
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
}
```

# How It Works
SmtpRouter is a recipe of three projects: [jstedfast/MimeKit](https://github.com/jstedfast/MimeKit), [jstedfast/MailKit](https://github.com/jstedfast/MailKit), and [cosullivan/SmtpServer](https://github.com/cosullivan/SmtpServer).

SmtpServer runs an SMTP server with a custom MiddlewareMessageStore which runs a stack of configured middlewares, each of which manipulates or acts on the email. MimeKit parses and manipulates the emails. MailKit resends them.
