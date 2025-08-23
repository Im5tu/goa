using Goa.Functions.Core;
using Goa.Functions.Kinesis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForKinesis()
    .ProcessAsBatch()
    .Using(async (kinesisEvent, cancellationToken) =>
    {
        // TODO :: Replace this with your custom processing logic
        // TODO :: To process records individually, use ProcessOneAtATime() instead
        
        var loggerFactory = Host.CreateDefaultBuilder().Build().Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("KinesisHandler");
        
        foreach (var record in kinesisEvent.Records ?? [])
        {
            try
            {
                // Decode the base64-encoded data
                var data = record.Kinesis?.Data;
                if (!string.IsNullOrEmpty(data))
                {
                    var decodedBytes = Convert.FromBase64String(data);
                    var decodedData = Encoding.UTF8.GetString(decodedBytes);
                    
                    logger.LogInformation("Processing Kinesis record: PartitionKey={PartitionKey}, SequenceNumber={SequenceNumber}, Data={Data}",
                        record.Kinesis?.PartitionKey,
                        record.Kinesis?.SequenceNumber,
                        decodedData);
                    
                    // TODO :: Add your business logic here
                    // For example: deserialize JSON, transform data, call external services, etc.
                    
                    // Example JSON deserialization:
                    // var message = JsonSerializer.Deserialize<YourMessageType>(decodedData);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process Kinesis record: {EventId}", record.EventId);
                
                // TODO :: If you want to fail this specific record and trigger retry/DLQ behavior,
                // uncomment the line below. Otherwise, the error will be logged but processing continues.
                // record.MarkAsFailed();
            }
        }
    })
    .RunAsync();