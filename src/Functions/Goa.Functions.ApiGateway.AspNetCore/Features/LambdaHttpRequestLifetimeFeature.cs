using Microsoft.AspNetCore.Http.Features;

namespace Goa.Functions.ApiGateway.AspNetCore.Features;

#pragma warning disable CS1591
internal sealed class LambdaHttpRequestLifetimeFeature : IHttpRequestLifetimeFeature, IDisposable
{
    private readonly CancellationTokenSource _cts;
    public CancellationToken RequestAborted { get; set; }

    public LambdaHttpRequestLifetimeFeature(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        RequestAborted = _cts.Token;
    }

    public void Abort()
    {
        _cts.Cancel();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
