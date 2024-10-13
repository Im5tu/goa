namespace Goa.Functions.Core.Bootstrapping;

public interface ILambdaRuntimeClient
{
    Task<Result<InvocationRequest>> GetNextInvocationAsync(CancellationToken cancellationToken = default);
    Task<Result> ReportInitializationErrorAsync(InitializationErrorPayload errorPayload, CancellationToken cancellationToken = default);
    Task<Result> ReportInvocationErrorAsync(string awsRequestId, InvocationErrorPayload errorPayload, CancellationToken cancellationToken = default);
    Task<Result> SendResponseAsync(string awsRequestId, HttpContent content, CancellationToken cancellationToken = default);
}
