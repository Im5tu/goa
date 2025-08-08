using Goa.Functions.Core;
using Goa.Functions.Dynamo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForDynamoDB()
    .ProcessAsBatch()
    .HandleWith<ILoggerFactory>((handler, batch) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that you want to handle the messages
        // TODO :: Convert each record how you want, eg: DynamoMapper.<TYPE>.FromDynamoRecord(record.Dynamodb!.NewImage!)

        // TODO :: If you want to fail one or more records from processing, call DynamoDbStreamRecord.MarkAsFailed();

        return Task.CompletedTask;
    })
    .RunAsync();
