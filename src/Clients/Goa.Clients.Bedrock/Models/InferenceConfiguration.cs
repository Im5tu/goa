namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// Configuration for model inference parameters.
/// </summary>
public class InferenceConfiguration
{
    /// <summary>
    /// The maximum number of tokens to generate in the response.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// The temperature for sampling. Higher values increase randomness.
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// The top-p (nucleus) sampling parameter.
    /// </summary>
    public float? TopP { get; set; }

    /// <summary>
    /// A list of stop sequences that will stop generation when encountered.
    /// </summary>
    public List<string>? StopSequences { get; set; }
}
