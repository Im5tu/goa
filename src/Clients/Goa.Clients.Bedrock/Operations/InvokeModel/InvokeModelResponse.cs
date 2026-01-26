namespace Goa.Clients.Bedrock.Operations.InvokeModel;

/// <summary>
/// Response from the Bedrock InvokeModel API.
/// </summary>
public sealed class InvokeModelResponse
{
    /// <summary>
    /// The raw response body from the model (model-specific format).
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// The MIME type of the response body.
    /// </summary>
    public string? ContentType { get; init; }
}
