using ErrorOr;
using Goa.Clients.Bedrock.Operations.ApplyGuardrail;
using Goa.Clients.Bedrock.Operations.Converse;
using Goa.Clients.Bedrock.Operations.CountTokens;
using Goa.Clients.Bedrock.Operations.InvokeModel;

namespace Goa.Clients.Bedrock;

/// <summary>
/// High-performance Bedrock client interface optimized for AWS Lambda usage.
/// All operations use strongly-typed request objects and return ErrorOr results.
/// </summary>
public interface IBedrockClient
{
    /// <summary>
    /// Sends a conversation request to a Bedrock model using the Converse API.
    /// </summary>
    /// <param name="request">The converse request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The converse response, or an error if the operation failed.</returns>
    Task<ErrorOr<ConverseResponse>> ConverseAsync(ConverseRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a Bedrock model with a raw JSON payload using the InvokeModel API.
    /// </summary>
    /// <param name="request">The invoke model request containing the raw JSON body.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The invoke model response containing the raw model output, or an error if the operation failed.</returns>
    Task<ErrorOr<InvokeModelResponse>> InvokeModelAsync(InvokeModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a guardrail to content using the ApplyGuardrail API.
    /// </summary>
    /// <param name="request">The apply guardrail request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The apply guardrail response, or an error if the operation failed.</returns>
    Task<ErrorOr<ApplyGuardrailResponse>> ApplyGuardrailAsync(ApplyGuardrailRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of tokens in a request using the CountTokens API.
    /// </summary>
    /// <param name="request">The count tokens request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The count tokens response, or an error if the operation failed.</returns>
    Task<ErrorOr<CountTokensResponse>> CountTokensAsync(CountTokensRequest request, CancellationToken cancellationToken = default);
}
