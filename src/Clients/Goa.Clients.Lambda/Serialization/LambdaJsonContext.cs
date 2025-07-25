using System.Text.Json.Serialization;
using Goa.Clients.Lambda.Operations.Invoke;
using Goa.Clients.Lambda.Operations.InvokeAsync;
using Goa.Clients.Lambda.Operations.InvokeDryRun;

namespace Goa.Clients.Lambda.Serialization;

/// <summary>
/// JSON serialization context for Lambda operations, optimized for AOT compilation.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(InvokeRequest))]
[JsonSerializable(typeof(InvokeResponse))]
[JsonSerializable(typeof(InvokeAsyncRequest))]
[JsonSerializable(typeof(InvokeAsyncResponse))]
[JsonSerializable(typeof(InvokeDryRunRequest))]
[JsonSerializable(typeof(InvokeDryRunResponse))]
[JsonSerializable(typeof(string))]
internal partial class LambdaJsonContext : JsonSerializerContext
{
}
