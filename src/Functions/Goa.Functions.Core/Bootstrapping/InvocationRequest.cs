namespace Goa.Functions.Core.Bootstrapping;

public sealed record InvocationRequest(string RequestId, string Payload, string DeadlineMs, string FunctionArn);
