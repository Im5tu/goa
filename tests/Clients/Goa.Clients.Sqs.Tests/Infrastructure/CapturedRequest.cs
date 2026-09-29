namespace Goa.Clients.Sqs.Tests.Infrastructure;

/// <summary>
/// A request recorded by <see cref="FakeSqsHttpHandler"/>.
/// </summary>
/// <param name="Target">The value of the X-Amz-Target header, if present.</param>
/// <param name="Body">The request body.</param>
public sealed record CapturedRequest(string? Target, string Body);
