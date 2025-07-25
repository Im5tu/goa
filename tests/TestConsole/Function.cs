using Goa.Functions.ApiGateway.AspNetCore;
using Goa.Functions.ApiGateway.Core.Payloads;
using Goa.Functions.ApiGateway.Core.Payloads.V2;
using System.Diagnostics;
using TestConsole;


// use a fake lambda runtime client for testing
var runtime = new FakeRuntimeClient();
// var request = new ProxyPayloadV1Request
// {
//     Resource = "/api/resource",
//     Path = "/api/resource",
//     HttpMethod = "POST",
//     Headers = new Dictionary<string, string>
//     {
//         { "Content-Type", "application/json" },
//         // This is signed with a dummy key (secret-key) using the HMAC-SHA256 algorithm.
//         {
//             "Authorization",
//             "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLWlkLTEyMyIsImVtYWlsIjoidXNlckBleGFtcGxlLmNvbSIsInJvbGUiOiJhZG1pbiJ9.j7X7XXEHOZxnxarZjfp4UBl3JISrT9X2MDUy9Pf2VCE\n"
//         }
//     },
//     MultiValueHeaders = new Dictionary<string, IList<string>>
//     {
//         { "Accept", new List<string> { "application/json", "text/plain" } }
//     },
//     QueryStringParameters = new Dictionary<string, string>
//     {
//         { "queryKey", "queryValue" }
//     },
//     MultiValueQueryStringParameters = new Dictionary<string, IList<string>>
//     {
//         { "multiQueryKey", new List<string> { "value1", "value2" } }
//     },
//     PathParameters = new Dictionary<string, string>
//     {
//         { "id", "12345" }
//     },
//     StageVariables = new Dictionary<string, string>
//     {
//         { "stage", "production" }
//     },
//     RequestContext = new ProxyPayloadV1RequestContext
//     {
//         Path = "/api/resource",
//         AccountId = "123456789012",
//         ResourceId = "resource-id-123",
//         Stage = "production",
//         RequestId = "request-id-12345",
//         Identity = new ProxyPayloadV1RequestIdentity
//         {
//             CognitoIdentityPoolId = "cognito-pool-id",
//             AccountId = "123456789012",
//             CognitoIdentityId = "cognito-identity-id",
//             Caller = "caller-id",
//             ApiKey = "api-key",
//             ApiKeyId = "api-key-id",
//             AccessKey = "access-key-id",
//             SourceIp = "192.168.1.1",
//             UserAgent = "PostmanRuntime/7.29.0",
//             User = "test-user"
//         },
//         ApiId = "api-id-12345",
//         DomainName = "api.example.com",
//         DomainPrefix = "api",
//         RequestTime = "12/Dec/2023:19:14:00 +0000",
//         RequestTimeEpoch = 1702404840000,
//         ExtendedRequestId = "extended-id-12345",
//         ConnectionId = "connection-id-12345",
//         EventType = "MESSAGE",
//         RouteKey = "POST /api/resource",
//         IntegrationLatency = "100",
//         MessageId = "message-id-12345",
//         Authorizer = new CustomAuthorizerContext
//         {
//             Claims = new Dictionary<string, string>
//             {
//                 { "sub", "user-sub-id" },
//                 { "email", "user@example.com" }
//             },
//             Scopes = new List<string> { "scope1", "scope2" }
//         }
//     },
//     Body = "{ \"example\": \"data\" }",
//     IsBase64Encoded = false
// };
var request = new ProxyPayloadV2Request
{
    RouteKey = "POST /api/resource",
    RawPath = "/api/resource",
    RawQueryString = "queryKey=queryValue&multiQueryKey=value1&multiQueryKey=value2",
    Headers = new Dictionary<string, string>
    {
        { "Content-Type", "application/json" },
        // This is signed with a dummy key (secret-key) using the HMAC-SHA256 algorithm.
        {
            "Authorization",
            "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLWlkLTEyMyIsImVtYWlsIjoidXNlckBleGFtcGxlLmNvbSIsInJvbGUiOiJhZG1pbiJ9.j7X7XXEHOZxnxarZjfp4UBl3JISrT9X2MDUy9Pf2VCE\n"
        },
        { "Accept", "application/json,text/plain" }
    },
    Cookies = new List<string> { "sessionId=abc123; Path=/; HttpOnly" }, // Example cookie
    QueryStringParameters = new Dictionary<string, string>
    {
        { "queryKey", "queryValue" },
        { "multiQueryKey", "value1,value2" } // Flattened multi-value query keys for v2
    },
    PathParameters = new Dictionary<string, string>
    {
        { "id", "12345" }
    },
    StageVariables = new Dictionary<string, string>
    {
        { "stage", "production" }
    },
    RequestContext = new ProxyPayloadV2RequestContext
    {
        AccountId = "123456789012",
        ApiId = "api-id-12345",
        Authorizer = new ProxyPayloadV2RequestAuthorizer
        {
            Jwt = new JwtDescription
            {
                Claims = new Dictionary<string, string>
                {
                    { "sub", "user-sub-id" },
                    { "email", "user@example.com" }
                },
                Scopes = new List<string> { "scope1", "scope2" }
            }
        },
        DomainName = "api.example.com",
        DomainPrefix = "api",
        Http = new ProxyPayloadV2RequestHttpDescription
        {
            Method = "POST",
            Path = "/api/resource",
            Protocol = "HTTP/1.1",
            SourceIp = "192.168.1.1",
            UserAgent = "PostmanRuntime/7.29.0"
        },
        RequestId = "request-id-12345",
        RouteId = "route-id-12345",
        RouteKey = "POST /api/resource",
        Stage = "production",
        Time = "12/Dec/2023:19:14:00 +0000",
        TimeEpoch = 1702404840000,
        Authentication = new ProxyPayloadV2RequestAuthentication
        {
            ClientCert = new ProxyRequestClientCert
            {
                SubjectDN = "CN=test-user",
                IssuerDN = "CN=test-issuer",
                SerialNumber = "1234567890",
                Validity = new ClientCertValidity
                {
                    NotBefore = "12/Dec/2023:19:00:00 +0000",
                    NotAfter = "12/Dec/2024:19:00:00 +0000"
                }
            }
        }
    },
    Body = "{ \"example\": \"data\" }",
    IsBase64Encoded = false
};

runtime.Enqueue(request);
runtime.Enqueue(request);
runtime.Enqueue(request);









var sw = Stopwatch.StartNew();

var builder = WebApplication.CreateSlimBuilder();
builder.Host.UseConsoleLifetime();
var app = builder.UseGoa(lambdaRuntimeClient: runtime).Build();

Console.WriteLine($"Built: {sw.ElapsedMilliseconds:0.##}ms");

app.MapGet("/", () => "Hello World!");
app.MapGet("/api/resource", () => "Hello Resource!");
await Task.WhenAny(app.RunAsync(), Task.Run(async () =>
{
    while (runtime.PendingInvocations > 0)
        await Task.Delay(50);
}));
Console.WriteLine($"Run: {sw.ElapsedMilliseconds:0.##}ms");

await app.Services.GetRequiredService<IHostLifetime>().StopAsync(CancellationToken.None);


// using Goa.Functions.ApiGateway.Payloads.V2;
// using Goa.Functions.Core.Logging;
// using Microsoft.Extensions.Logging;
// using System.Diagnostics;
// using System.Text.Json.Serialization;
//
// var logger = new JsonLogger("Test", LogLevel.Information);
// var http = Http.UseHttpV2()
//     .UseMiddleware(pipeline =>
//     {
//         pipeline.Use((_, next, _) =>
//         {
//             logger.LogInvocation();
//             return next();
//         });
//     })
//     .MapGet("/ping", (context, _) =>
//     {
//         context.Response.Result = HttpResult.Ok(new Pong("Hi hi"));
//         return Task.CompletedTask;
//     })
//     .MapGet("/ping/{id}", (context, _) =>
//     {
//         context.Response.Result = HttpResult.Ok(new Pong(context.Request.RouteValues!["id"]));
//         return Task.CompletedTask;
//     });
//
// var p = http.CreatePipeline().ToList();
// var pipeline = new Function(p, MySerializerContext.Default);
//
// var sw = Stopwatch.StartNew();
// var result1 = await pipeline.InvokeAsync(new ProxyPayloadV2Request
// {
//     Headers = new Dictionary<string, string>
//     {
//         ["Accept"] = "application/json"
//     },
//     RawPath = "/ping"
// }, default);
//
// var result2 = await pipeline.InvokeAsync(new ProxyPayloadV2Request
// {
//     Headers = new Dictionary<string, string>
//     {
//         ["Accept"] = "application/json"
//     },
//     RawPath = "/ping/2"
// }, default);
//
// Console.WriteLine($"Elapsed time: {sw.ElapsedMilliseconds}ms");
//
// Console.WriteLine("Result1: " + result1.StatusCode + " " + result1?.Body);
// Console.WriteLine("Result2: " + result2.StatusCode + " " + result2?.Body);
//
// await Lambda.RunAsync(http, MySerializerContext.Default);
//
// [JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
// [JsonSerializable(typeof(string))]
// [JsonSerializable(typeof(Pong))]
// public partial class MySerializerContext : JsonSerializerContext
// {
// }
//
// public static partial class FunctionLogs
// {
//     [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Middleware Invoked")]
//     public static partial void LogInvocation(this ILogger logger);
// }
//
// public record Pong(string Message);


namespace TestConsole
{

}
