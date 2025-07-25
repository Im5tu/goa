using System.Text.Json.Serialization;
using Goa.Clients.Sns.Models;
using Goa.Clients.Sns.Operations.Publish;

namespace Goa.Clients.Sns.Serialization;

/// <summary>
/// JSON serialization context for SNS operations, optimized for AOT compilation.
/// </summary>
[JsonSerializable(typeof(PublishRequest))]
[JsonSerializable(typeof(PublishResponse))]
[JsonSerializable(typeof(SnsMessageAttributeValue))]
[JsonSerializable(typeof(Dictionary<string, SnsMessageAttributeValue>))]
internal partial class SnsJsonContext : JsonSerializerContext
{
}