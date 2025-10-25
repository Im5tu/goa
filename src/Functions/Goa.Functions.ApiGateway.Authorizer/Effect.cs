using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents the effect of a policy statement (Allow or Deny)
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Effect
{
    /// <summary>
    /// Allow the action on the resource
    /// </summary>
    Allow,

    /// <summary>
    /// Deny the action on the resource
    /// </summary>
    Deny
}
