using Goa.Functions.Core;
using Goa.Functions.EventBridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForEventBridge()
    .ProcessOneAtATime()
    .HandleWith<ILoggerFactory>((handler, evt) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that you want to handle the events
        // TODO :: Process the EventBridge event as needed
        
        // Available properties:
        // - evt.Source (e.g., "aws.events", "mycompany.orders")
        // - evt.DetailType (e.g., "Scheduled Event", "Order Placed")
        // - evt.Detail (event-specific data as object)
        // - evt.Time (timestamp)
        // - evt.Region, evt.Account, evt.Resources

        // TODO :: If you want to fail the event from processing, call evt.MarkAsFailed();

        return Task.CompletedTask;
    })
    .RunAsync();