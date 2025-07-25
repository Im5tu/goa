using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Goa.Functions.ApiGateway.AspNetCore.Features;

internal sealed class LambdaHttpRequestBodyDetectionFeature(IHttpRequestFeature request) : IHttpRequestBodyDetectionFeature
{
    public bool CanHaveBody { get; } = (request.Method == HttpMethods.Put || request.Method == HttpMethods.Patch || request.Method == HttpMethods.Post) && request.Body != Stream.Null;
}
