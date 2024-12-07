using Microsoft.AspNetCore.Http.Features;

namespace Goa.Functions.ApiGateway.AspNetCore.Features;

#pragma warning disable CS1591
internal sealed class LambdaHttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
{
    public required string TraceIdentifier { get; set; }
}
