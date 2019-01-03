# SmtpRouter
An SMTP server that captures and reroutes emails for test environments

# Usage
From the src directory:
```
dotnet build
dotnet run --project SmtpRouter/SmtpRouter.csproj
```

This will start an SMTP server on localhost listening on port 25. By default the router manipulates the messages and logs the final message to the console. You can see the predefined step in the [ExampleMiddlewarePipeline](https://github.com/justinjstark/SmtpRouter/blob/master/src/SmtpRouter/MiddlewarePipelines/ExampleMiddlewarePipeline.cs#L12).

# Demo
There is a demo project you can use to test the router. It is configured to send messages to localhost:25.

In another terminal window while the SmtpRouter is running, from the src directory:
```
dotnet run --project SmtpRouter.Demo.Client/SmtpRouter.Demo.Client.csproj
```

# Configuration
SmtpRouter can be configured by code. Since the steps are highly customizable and should rarely change, it does not make sense to support configuration files. See [ExampleMiddlewarePipeline](https://github.com/justinjstark/SmtpRouter/blob/master/src/SmtpRouter/MiddlewarePipelines/ExampleMiddlewarePipeline.cs#L12) for a configuration example.

While you can write your own middleware by inheriting from ISmtpMiddleware and implementing RunAsync, there are [several pieces of configurable middleware already defined](https://github.com/justinjstark/SmtpRouter/tree/master/src/SmtpRouter/Middleware).

The simplest useful example for a test environment is to reroute messages to a mailbox and resend them.
```csharp
var pipline = new List<ISmtpMiddleware>
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
TODO:
