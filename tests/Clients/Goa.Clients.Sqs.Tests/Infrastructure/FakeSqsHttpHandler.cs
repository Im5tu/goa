using System.Net;
using System.Text;

namespace Goa.Clients.Sqs.Tests.Infrastructure;

/// <summary>
/// Primary HTTP handler that records every request sent by the SQS client and replies with a canned response,
/// so the wire format (target header and JSON body) can be asserted without LocalStack.
/// </summary>
public sealed class FakeSqsHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;
    private readonly TimeSpan _responseDelay;

    public FakeSqsHttpHandler(string responseBody = "{}", HttpStatusCode statusCode = HttpStatusCode.OK, TimeSpan? responseDelay = null)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
        _responseDelay = responseDelay ?? TimeSpan.Zero;
    }

    public List<CapturedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var target = request.Headers.TryGetValues("X-Amz-Target", out var values) ? string.Join(",", values) : null;
        Requests.Add(new CapturedRequest(target, body));

        if (_responseDelay > TimeSpan.Zero)
            await Task.Delay(_responseDelay, cancellationToken);

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/x-amz-json-1.0")
        };
    }
}
