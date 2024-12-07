using Microsoft.AspNetCore.Http.Features;
using System.IO.Pipelines;

namespace Goa.Functions.ApiGateway.AspNetCore.Features.V2;

internal sealed class LambdaHttpResponseBodyFeatureV2 : IHttpResponseBodyFeature
{
    public Stream Stream { get; } = new MemoryStream();
    public PipeWriter Writer { get; } = PipeWriter.Create(Stream.Null);

    public void DisableBuffering() { }

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("SendFileAsync is not supported.");
    }

    public Task CompleteAsync() => Task.CompletedTask;
}