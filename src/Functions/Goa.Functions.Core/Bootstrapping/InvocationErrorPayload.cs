namespace Goa.Functions.Core.Bootstrapping;

/// <summary>
/// Represents the payload for reporting invocation errors to the AWS Lambda Runtime API.
/// </summary>
/// <param name="ErrorType">The type of the error (e.g., "UnhandledException").</param>
/// <param name="ErrorMessage">The message detailing the error encountered during invocation.</param>
/// <param name="StackTrace">The stack trace of the error as an array of strings.</param>
#pragma warning disable CS9113 // Parameter type is never used. This is by design with the AWS Lambda Runtime as we need to send details.
public sealed class InvocationErrorPayload(string ErrorType, string ErrorMessage, string[] StackTrace);
