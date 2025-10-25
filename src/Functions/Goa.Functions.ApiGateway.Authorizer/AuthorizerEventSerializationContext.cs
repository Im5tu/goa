using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// JSON serialization context for API Gateway authorizer events
/// </summary>
[JsonSerializable(typeof(TokenAuthorizerEvent))]
[JsonSerializable(typeof(RequestAuthorizerEvent))]
[JsonSerializable(typeof(AuthorizerResponse))]
[JsonSerializable(typeof(PolicyDocument))]
[JsonSerializable(typeof(PolicyStatement))]
[JsonSerializable(typeof(AuthorizerRequestContext))]
[JsonSerializable(typeof(AuthorizerIdentity))]
[JsonSerializable(typeof(ClientCertificate))]
[JsonSerializable(typeof(CertificateValidity))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(IList<PolicyStatement>))]
internal partial class AuthorizerEventSerializationContext : JsonSerializerContext
{
}
