using Microsoft.AspNetCore.Http.Features;

namespace Goa.Functions.ApiGateway.Features;

internal sealed class LambdaHttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
{
    public required string TraceIdentifier { get; set; }
}
