using Goa.Functions.ApiGateway;
using Goa.Functions.ApiGateway.Payloads;
using Goa.Functions.ApiGateway.Payloads.V2;
using Goa.Functions.Core;
using System.Diagnostics;
using System.Text.Json.Serialization;
using TestConsole;



var runtime = new FakeRuntimeClient();

// use a fake lambda runtime client for testing
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

var app = Host.CreateDefaultBuilder()
    .UseLambdaLifecycle(runtime)
    .ForAspNetCore(app =>
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/ping", () => new Pong("PONG!"));
            endpoints.MapPost("/api/resource", () => new Pong("Hello Resource!"));
        });
    }, apiGatewayType: ApiGatewayType.HttpV2)
    .WithServices(services =>
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolver = HttpSerializerContext.Default;
        });
    });

await Task.WhenAny(app.RunAsync(), Task.Run(async () =>
{
    while (runtime.PendingInvocations > 0)
        await Task.Delay(10);
}));
Console.WriteLine($"Run: {sw.ElapsedMilliseconds:0.##}ms");

namespace TestConsole
{
    [JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(Pong))]
    public partial class HttpSerializerContext : JsonSerializerContext
    {
    }

    public record Pong(string Message);
}
