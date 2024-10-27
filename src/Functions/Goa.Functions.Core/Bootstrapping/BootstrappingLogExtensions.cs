using Microsoft.Extensions.Logging;

namespace Goa.Functions.Core.Bootstrapping;

internal static partial class BootstrappingLogExtensions
{
    // LambdaBootstrapper
    [LoggerMessage(EventId = 1, Level = LogLevel.Trace, Message = "Bootstrap started")]
    internal static partial void BootstrapStarted(this ILogger logger);
    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to communicate to the .NET runtime the amount of memory configured for the Lambda function via the AWS_LAMBDA_FUNCTION_MEMORY_SIZE environment variable. Requested memory size: {MemorySize}")]
    internal static partial void BootstrapMemorySettingsAdjustmentError(this ILogger logger, Exception exception, ulong memorySize);
    [LoggerMessage(EventId = 4, Level = LogLevel.Trace, Message = "Deserializing request payload")]
    internal static partial void BootstrapInvocationRequestDeserializationStart(this ILogger logger);
    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to deserialize request payload. Null payload returned")]
    internal static partial void BootstrapInvocationRequestDeserializationFailed(this ILogger logger);

    // LambdaRuntimeClient.GetNextInvocationAsync
    [LoggerMessage(EventId = 1, Level = LogLevel.Trace, Message = "Requesting next invocation from Lambda Runtime API")]
    internal static partial void GetNextInvocationStart(this ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Trace, Message = "Received invocation")]
    internal static partial void GetNextInvocationComplete(this ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to get next invocation from Lambda Runtime API")]
    internal static partial void GetNextInvocationError(this ILogger logger, Exception exception);

    // LambdaRuntimeClient.ReportInitializationError
    [LoggerMessage(EventId = 4, Level = LogLevel.Trace, Message = "Reporting initialization error")]
    internal static partial void ReportInitializationErrorStart(this ILogger logger);
    [LoggerMessage(EventId = 5, Level = LogLevel.Trace, Message = "Reporting initialization error complete. StatusCode: {StatusCode}")]
    internal static partial void ReportInitializationErrorComplete(this ILogger logger, int statusCode);
    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to report initialization error")]
    internal static partial void ReportInitializationErrorFailed(this ILogger logger, Exception exception);

    // LambdaRuntimeClient.ReportInvocationError
    [LoggerMessage(EventId = 4, Level = LogLevel.Trace, Message = "Reporting invocation error")]
    internal static partial void ReportInvocationErrorStart(this ILogger logger);
    [LoggerMessage(EventId = 5, Level = LogLevel.Trace, Message = "Reporting invocation error complete. StatusCode: {StatusCode}")]
    internal static partial void ReportInvocationErrorComplete(this ILogger logger, int statusCode);
    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to report invocation error")]
    internal static partial void ReportInvocationErrorFailed(this ILogger logger, Exception exception);

    // LambdaRuntimeClient.SendResponse
    [LoggerMessage(EventId = 4, Level = LogLevel.Trace, Message = "Returning response")]
    internal static partial void SendResponseStart(this ILogger logger);
    [LoggerMessage(EventId = 5, Level = LogLevel.Trace, Message = "Returning response complete. StatusCode: {StatusCode}")]
    internal static partial void SendResponseComplete(this ILogger logger, int statusCode);
    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to return response")]
    internal static partial void SendResponseFailed(this ILogger logger, Exception exception);
}
