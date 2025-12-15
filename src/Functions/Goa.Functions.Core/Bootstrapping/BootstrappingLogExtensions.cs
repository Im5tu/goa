using Microsoft.Extensions.Logging;

namespace Goa.Functions.Core.Bootstrapping;

internal static partial class BootstrappingLogExtensions
{
    // LambdaBootstrapper: EventIds 100-199
    [LoggerMessage(EventId = 100, Level = LogLevel.Trace, Message = "Bootstrap started")]
    internal static partial void BootstrapStarted(this ILogger logger);
    [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "Failed to communicate to the .NET runtime the amount of memory configured for the Lambda function via the AWS_LAMBDA_FUNCTION_MEMORY_SIZE environment variable. Requested memory size: {MemorySize}")]
    internal static partial void BootstrapMemorySettingsAdjustmentError(this ILogger logger, Exception exception, ulong memorySize);
    [LoggerMessage(EventId = 102, Level = LogLevel.Trace, Message = "Deserializing request payload")]
    internal static partial void BootstrapInvocationRequestDeserializationStart(this ILogger logger);
    [LoggerMessage(EventId = 103, Level = LogLevel.Error, Message = "Failed to deserialize request payload. Null payload returned")]
    internal static partial void BootstrapInvocationRequestDeserializationFailed(this ILogger logger);
    [LoggerMessage(EventId = 104, Level = LogLevel.Warning, Message = "Failed to get next invocation: {ErrorMessage}")]
    internal static partial void BootstrapGetNextInvocationFailed(this ILogger logger, string? errorMessage);
    [LoggerMessage(EventId = 105, Level = LogLevel.Error, Message = "Invocation processing failed")]
    internal static partial void BootstrapInvocationProcessingFailed(this ILogger logger, Exception exception);
    [LoggerMessage(EventId = 106, Level = LogLevel.Error, Message = "Function factory initialization failed")]
    internal static partial void BootstrapFunctionFactoryInitializationFailed(this ILogger logger, Exception exception);

    // LambdaRuntimeClient.GetNextInvocationAsync: EventIds 200-299
    [LoggerMessage(EventId = 200, Level = LogLevel.Trace, Message = "Requesting next invocation from Lambda Runtime API")]
    internal static partial void GetNextInvocationStart(this ILogger logger);
    [LoggerMessage(EventId = 201, Level = LogLevel.Trace, Message = "Received invocation")]
    internal static partial void GetNextInvocationComplete(this ILogger logger);
    [LoggerMessage(EventId = 202, Level = LogLevel.Error, Message = "Failed to get next invocation from Lambda Runtime API")]
    internal static partial void GetNextInvocationError(this ILogger logger, Exception exception);
    [LoggerMessage(EventId = 203, Level = LogLevel.Warning, Message = "Get next invocation was cancelled")]
    internal static partial void GetNextInvocationCancelled(this ILogger logger, Exception exception);

    // LambdaRuntimeClient.ReportInitializationError: EventIds 300-399
    [LoggerMessage(EventId = 300, Level = LogLevel.Trace, Message = "Reporting initialization error")]
    internal static partial void ReportInitializationErrorStart(this ILogger logger);
    [LoggerMessage(EventId = 301, Level = LogLevel.Trace, Message = "Reporting initialization error complete. StatusCode: {StatusCode}")]
    internal static partial void ReportInitializationErrorComplete(this ILogger logger, int statusCode);
    [LoggerMessage(EventId = 302, Level = LogLevel.Error, Message = "Failed to report initialization error")]
    internal static partial void ReportInitializationErrorFailed(this ILogger logger, Exception exception);

    // LambdaRuntimeClient.ReportInvocationError: EventIds 400-499
    [LoggerMessage(EventId = 400, Level = LogLevel.Trace, Message = "Reporting invocation error")]
    internal static partial void ReportInvocationErrorStart(this ILogger logger);
    [LoggerMessage(EventId = 401, Level = LogLevel.Trace, Message = "Reporting invocation error complete. StatusCode: {StatusCode}")]
    internal static partial void ReportInvocationErrorComplete(this ILogger logger, int statusCode);
    [LoggerMessage(EventId = 402, Level = LogLevel.Error, Message = "Failed to report invocation error")]
    internal static partial void ReportInvocationErrorFailed(this ILogger logger, Exception exception);

    // LambdaRuntimeClient.SendResponse: EventIds 500-599
    [LoggerMessage(EventId = 500, Level = LogLevel.Trace, Message = "Returning response")]
    internal static partial void SendResponseStart(this ILogger logger);
    [LoggerMessage(EventId = 501, Level = LogLevel.Trace, Message = "Returning response complete. StatusCode: {StatusCode}")]
    internal static partial void SendResponseComplete(this ILogger logger, int statusCode);
    [LoggerMessage(EventId = 502, Level = LogLevel.Error, Message = "Failed to return response")]
    internal static partial void SendResponseFailed(this ILogger logger, Exception exception);
}
