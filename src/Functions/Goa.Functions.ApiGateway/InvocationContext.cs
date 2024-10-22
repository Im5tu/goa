namespace Goa.Functions.ApiGateway;

#pragma warning disable CS1591, CS3021
public sealed class InvocationContext
{
    public required Request Request { get; init; }
    public HttpResult? Response { get; set; }
}

public sealed class Request
{
}
