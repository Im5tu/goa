using Goa.Functions.ApiGateway.Payloads.V1;
using Goa.Functions.ApiGateway.Payloads.V2;
using Goa.Functions.Core.Bootstrapping;
using System.Text.Json;
using ProxyPayloadV1SerializationContext = Goa.Functions.ApiGateway.Payloads.V1.ProxyPayloadV1SerializationContext;
using ProxyPayloadV2SerializationContext = Goa.Functions.ApiGateway.Payloads.V2.ProxyPayloadV2SerializationContext;

namespace TestConsole;

public class FakeRuntimeClient : ILambdaRuntimeClient
{
    private readonly Queue<string> _queue = new();

    public int PendingInvocations => _queue.Count;

    public void Enqueue(ProxyPayloadV1Request request) => _queue.Enqueue(JsonSerializer.Serialize(request, ProxyPayloadV1SerializationContext.Default.ProxyPayloadV1Request));
    public void Enqueue(ProxyPayloadV2Request request) => _queue.Enqueue(JsonSerializer.Serialize(request, ProxyPayloadV2SerializationContext.Default.ProxyPayloadV2Request));

    public Task<Result<InvocationRequest>> GetNextInvocationAsync(CancellationToken cancellationToken = default)
    {
        var payload = _queue.Count > 0 ? _queue.Dequeue() : string.Empty;
        var request = new InvocationRequest(Guid.NewGuid().ToString("N"), payload, DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeMilliseconds().ToString(), "test");
        return Task.FromResult(Result<InvocationRequest>.Success(request));
    }

    public Task<Result> ReportInitializationErrorAsync(InitializationErrorPayload errorPayload,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ReportInvocationErrorAsync(string awsRequestId, InvocationErrorPayload errorPayload,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SendResponseAsync(string awsRequestId, HttpContent content, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}
