using System.Text.Json;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Operations.Converse;

/// <summary>
/// Fluent builder for constructing Bedrock Converse requests.
/// </summary>
/// <param name="modelId">The identifier of the model to use.</param>
public class ConverseBuilder(string modelId)
{
    private readonly ConverseRequest _request = new()
    {
        ModelId = modelId
    };

    /// <summary>
    /// Sets the system prompt for the conversation.
    /// </summary>
    /// <param name="prompt">The system prompt text.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithSystemPrompt(string prompt)
    {
        _request.System ??= new();
        _request.System.Add(new SystemContentBlock { Text = prompt });
        return this;
    }

    /// <summary>
    /// Adds a user message to the conversation.
    /// </summary>
    /// <param name="content">The text content of the user message.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder AddUserMessage(string content)
    {
        _request.Messages.Add(new Message
        {
            Role = ConversationRole.User,
            Content = new List<ContentBlock> { new() { Text = content } }
        });
        return this;
    }

    /// <summary>
    /// Adds an assistant message to the conversation.
    /// </summary>
    /// <param name="content">The text content of the assistant message.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder AddAssistantMessage(string content)
    {
        _request.Messages.Add(new Message
        {
            Role = ConversationRole.Assistant,
            Content = new List<ContentBlock> { new() { Text = content } }
        });
        return this;
    }

    /// <summary>
    /// Adds a message with the specified role to the conversation.
    /// </summary>
    /// <param name="role">The role of the message sender.</param>
    /// <param name="content">The content blocks of the message.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder AddMessage(ConversationRole role, List<ContentBlock> content)
    {
        _request.Messages.Add(new Message
        {
            Role = role,
            Content = content
        });
        return this;
    }

    /// <summary>
    /// Sets the maximum number of tokens to generate.
    /// </summary>
    /// <param name="maxTokens">The maximum number of tokens.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithMaxTokens(int maxTokens)
    {
        _request.InferenceConfig ??= new();
        _request.InferenceConfig.MaxTokens = maxTokens;
        return this;
    }

    /// <summary>
    /// Sets the temperature for sampling.
    /// </summary>
    /// <param name="temperature">The temperature value (0.0 to 1.0).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithTemperature(float temperature)
    {
        _request.InferenceConfig ??= new();
        _request.InferenceConfig.Temperature = temperature;
        return this;
    }

    /// <summary>
    /// Sets the top-p (nucleus) sampling parameter.
    /// </summary>
    /// <param name="topP">The top-p value (0.0 to 1.0).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithTopP(float topP)
    {
        _request.InferenceConfig ??= new();
        _request.InferenceConfig.TopP = topP;
        return this;
    }

    /// <summary>
    /// Sets the stop sequences that will stop generation.
    /// </summary>
    /// <param name="stopSequences">The stop sequences.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithStopSequences(params string[] stopSequences)
    {
        _request.InferenceConfig ??= new();
        _request.InferenceConfig.StopSequences = stopSequences.ToList();
        return this;
    }

    /// <summary>
    /// Adds a tool that the model can use.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <param name="description">A description of what the tool does.</param>
    /// <param name="inputSchema">The JSON schema defining the tool's input parameters.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithTool(string name, string? description, JsonElement inputSchema)
    {
        _request.ToolConfig ??= new();
        _request.ToolConfig.Tools.Add(new Tool
        {
            ToolSpec = new ToolSpec
            {
                Name = name,
                Description = description,
                InputSchema = new ToolInputSchema { Json = inputSchema }
            }
        });
        return this;
    }

    /// <summary>
    /// Sets the tool choice configuration.
    /// </summary>
    /// <param name="toolChoice">The tool choice configuration.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithToolChoice(ToolChoice toolChoice)
    {
        _request.ToolConfig ??= new();
        _request.ToolConfig.ToolChoice = toolChoice;
        return this;
    }

    /// <summary>
    /// Sets the guardrail configuration.
    /// </summary>
    /// <param name="guardrailIdentifier">The guardrail identifier.</param>
    /// <param name="guardrailVersion">The guardrail version.</param>
    /// <param name="trace">Optional trace configuration.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithGuardrail(string guardrailIdentifier, string guardrailVersion, string? trace = null)
    {
        _request.GuardrailConfig = new GuardrailConfiguration
        {
            GuardrailIdentifier = guardrailIdentifier,
            GuardrailVersion = guardrailVersion,
            Trace = trace
        };
        return this;
    }

    /// <summary>
    /// Sets the performance configuration.
    /// </summary>
    /// <param name="latencyMode">The latency mode.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithPerformance(LatencyMode latencyMode)
    {
        _request.PerformanceConfig = new PerformanceConfiguration
        {
            Latency = latencyMode
        };
        return this;
    }

    /// <summary>
    /// Sets the service tier for request processing.
    /// </summary>
    /// <param name="serviceTier">The service tier.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithServiceTier(ServiceTier serviceTier)
    {
        _request.RequestMetadata = new RequestMetadata
        {
            ServiceTier = serviceTier
        };
        return this;
    }

    /// <summary>
    /// Sets additional model-specific request fields.
    /// </summary>
    /// <param name="additionalFields">The additional fields as a JSON element.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConverseBuilder WithAdditionalModelFields(JsonElement additionalFields)
    {
        _request.AdditionalModelRequestFields = additionalFields;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured ConverseRequest.
    /// </summary>
    /// <returns>The configured ConverseRequest instance.</returns>
    public ConverseRequest Build()
    {
        return _request;
    }
}
