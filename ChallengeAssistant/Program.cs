using System.Net;
using ChallengeAssistant;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 5000);
            options.Listen(IPAddress.Loopback, 5005, configure =>
            {
                configure.UseHttps();
                configure.Protocols = HttpProtocols.Http2;
            });
        });
        
        builder.UseStartup<Startup>();
    });

var app = hostBuilder.Build();
await app.RunAsync();
    