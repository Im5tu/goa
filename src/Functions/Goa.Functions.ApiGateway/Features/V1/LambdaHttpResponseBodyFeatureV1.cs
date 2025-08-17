using Microsoft.AspNetCore.Http.Features;
using System.IO.Pipelines;

namespace Goa.Functions.ApiGateway.Features.V1;

internal sealed class LambdaHttpResponseBodyFeatureV1 : IHttpResponseBodyFeature
{
    public Stream Stream { get; } = new MemoryStream();
    public PipeWriter Writer { get; }

    public LambdaHttpResponseBodyFeatureV1()
    {
        Writer = PipeWriter.Create(Stream);
    }

    public void DisableBuffering() { /* No buffering needed */ }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("SendFileAsync is not supported.");
    }

    public Task CompleteAsync()
    {
        return Task.CompletedTask;
    }
}
