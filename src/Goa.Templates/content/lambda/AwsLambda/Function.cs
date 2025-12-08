using Goa.Functions.Core;
using Goa.Functions.Core.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForString()
    .HandleWith<ILoggerFactory>((handler, input) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that you want to handle the request
        // TODO :: Process the input string and return a response

        return Task.FromResult($"Processed: {input}");
    })
    .RunAsync();
