using Microsoft.AspNetCore.Http.Features;

namespace Goa.Functions.ApiGateway.AspNetCore.Features;

internal sealed class LambdaHttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
{
    public required string TraceIdentifier { get; set; }
}
