using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway;

[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(DictionaryJsonConverter), typeof(RouteEndpointJsonConverter)]
)]
[JsonSerializable(typeof(IDictionary<string, object>))]
[JsonSerializable(typeof(IDictionary<string, string>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(HostString))]
[JsonSerializable(typeof(PathString))]
[JsonSerializable(typeof(RouteEndpoint))]
[JsonSerializable(typeof(Endpoint))]
internal partial class LoggingSerializationContext : JsonSerializerContext;
