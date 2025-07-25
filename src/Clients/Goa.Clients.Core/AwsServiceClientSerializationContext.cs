using Goa.Clients.Core.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Clients.Core;

/// <summary>
/// JSON serialization context for AWS service client types, providing optimized serialization performance.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ApiError))]
public partial class AwsServiceClientSerializationContext : JsonSerializerContext
{
}