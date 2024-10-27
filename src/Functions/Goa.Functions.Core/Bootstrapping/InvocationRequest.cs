namespace Goa.Functions.Core.Bootstrapping;

/// <summary>
///     Represents a Lambda invocation request containing the request details such as ID, payload, deadline, and function ARN.
/// </summary>
/// <param name="RequestId">The unique request ID for the Lambda invocation.</param>
/// <param name="Payload">The payload associated with the Lambda invocation.</param>
/// <param name="DeadlineMs">The deadline for the invocation in Unix time (milliseconds).</param>
/// <param name="FunctionArn">The ARN of the invoked Lambda function.</param>
public sealed record InvocationRequest(string RequestId, string Payload, string DeadlineMs, string FunctionArn);
