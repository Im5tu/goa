using System.Text.Json;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Operations.Converse;

namespace Goa.Clients.Bedrock.Tests;

public class ConverseBuilderTests
{
    private const string TestModelId = "anthropic.claude-3-sonnet-20240229-v1:0";

    [Test]
    public async Task Build_WithSystemPrompt_SetsSystemContent()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithSystemPrompt("You are a helpful assistant.");

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.System).IsNotNull();
        await Assert.That(request.System!).Count().IsEqualTo(1);
        await Assert.That(request.System![0].Text).IsEqualTo("You are a helpful assistant.");
    }

    [Test]
    public async Task Build_WithMultipleSystemPrompts_AddsAll()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithSystemPrompt("You are a helpful assistant.")
            .WithSystemPrompt("Be concise.");

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.System).IsNotNull();
        await Assert.That(request.System!).Count().IsEqualTo(2);
        await Assert.That(request.System![0].Text).IsEqualTo("You are a helpful assistant.");
        await Assert.That(request.System![1].Text).IsEqualTo("Be concise.");
    }

    [Test]
    public async Task Build_WithUserMessage_AddsUserMessage()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .AddUserMessage("Hello, how are you?");

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.Messages).Count().IsEqualTo(1);
        await Assert.That(request.Messages[0].Role).IsEqualTo(ConversationRole.User);
        await Assert.That(request.Messages[0].Content).Count().IsEqualTo(1);
        await Assert.That(request.Messages[0].Content[0].Text).IsEqualTo("Hello, how are you?");
    }

    [Test]
    public async Task Build_WithAssistantMessage_AddsAssistantMessage()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .AddAssistantMessage("I am doing well, thank you!");

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.Messages).Count().IsEqualTo(1);
        await Assert.That(request.Messages[0].Role).IsEqualTo(ConversationRole.Assistant);
        await Assert.That(request.Messages[0].Content).Count().IsEqualTo(1);
        await Assert.That(request.Messages[0].Content[0].Text).IsEqualTo("I am doing well, thank you!");
    }

    [Test]
    public async Task Build_WithMaxTokens_SetsInferenceConfig()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithMaxTokens(1024);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.InferenceConfig).IsNotNull();
        await Assert.That(request.InferenceConfig!.MaxTokens).IsEqualTo(1024);
    }

    [Test]
    public async Task Build_WithTemperature_SetsInferenceConfig()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithTemperature(0.7f);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.InferenceConfig).IsNotNull();
        await Assert.That(request.InferenceConfig!.Temperature).IsEqualTo(0.7f);
    }

    [Test]
    public async Task Build_WithTopP_SetsInferenceConfig()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithTopP(0.9f);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.InferenceConfig).IsNotNull();
        await Assert.That(request.InferenceConfig!.TopP).IsEqualTo(0.9f);
    }

    [Test]
    public async Task Build_WithStopSequences_SetsInferenceConfig()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithStopSequences("###", "END");

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.InferenceConfig).IsNotNull();
        await Assert.That(request.InferenceConfig!.StopSequences).Count().IsEqualTo(2);
        await Assert.That(request.InferenceConfig!.StopSequences![0]).IsEqualTo("###");
        await Assert.That(request.InferenceConfig!.StopSequences![1]).IsEqualTo("END");
    }

    [Test]
    public async Task Build_WithTool_AddsToolConfig()
    {
        // Arrange
        var inputSchema = JsonDocument.Parse("""{"type": "object", "properties": {"location": {"type": "string"}}}""").RootElement;
        var builder = new ConverseBuilder(TestModelId)
            .WithTool("get_weather", "Get the current weather for a location", inputSchema);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.ToolConfig).IsNotNull();
        await Assert.That(request.ToolConfig!.Tools).Count().IsEqualTo(1);
        await Assert.That(request.ToolConfig!.Tools[0].ToolSpec.Name).IsEqualTo("get_weather");
        await Assert.That(request.ToolConfig!.Tools[0].ToolSpec.Description).IsEqualTo("Get the current weather for a location");
    }

    [Test]
    public async Task Build_WithMultipleTools_AddsAllTools()
    {
        // Arrange
        var weatherSchema = JsonDocument.Parse("""{"type": "object", "properties": {"location": {"type": "string"}}}""").RootElement;
        var searchSchema = JsonDocument.Parse("""{"type": "object", "properties": {"query": {"type": "string"}}}""").RootElement;

        var builder = new ConverseBuilder(TestModelId)
            .WithTool("get_weather", "Get the current weather", weatherSchema)
            .WithTool("search", "Search the web", searchSchema);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.ToolConfig).IsNotNull();
        await Assert.That(request.ToolConfig!.Tools).Count().IsEqualTo(2);
        await Assert.That(request.ToolConfig!.Tools[0].ToolSpec.Name).IsEqualTo("get_weather");
        await Assert.That(request.ToolConfig!.Tools[1].ToolSpec.Name).IsEqualTo("search");
    }

    [Test]
    public async Task Build_WithToolChoice_SetsToolChoice()
    {
        // Arrange
        var inputSchema = JsonDocument.Parse("""{"type": "object"}""").RootElement;
        var toolChoice = new ToolChoice { Auto = new AutoToolChoice() };

        var builder = new ConverseBuilder(TestModelId)
            .WithTool("my_tool", null, inputSchema)
            .WithToolChoice(toolChoice);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.ToolConfig).IsNotNull();
        await Assert.That(request.ToolConfig!.ToolChoice).IsNotNull();
        await Assert.That(request.ToolConfig!.ToolChoice!.Auto).IsNotNull();
    }

    [Test]
    public async Task Build_WithGuardrail_SetsGuardrailConfig()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithGuardrail("my-guardrail", "1", "enabled");

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.GuardrailConfig).IsNotNull();
        await Assert.That(request.GuardrailConfig!.GuardrailIdentifier).IsEqualTo("my-guardrail");
        await Assert.That(request.GuardrailConfig!.GuardrailVersion).IsEqualTo("1");
        await Assert.That(request.GuardrailConfig!.Trace).IsEqualTo("enabled");
    }

    [Test]
    public async Task Build_WithPerformance_SetsPerformanceConfig()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithPerformance(LatencyMode.Optimized);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.PerformanceConfig).IsNotNull();
        await Assert.That(request.PerformanceConfig!.Latency).IsEqualTo(LatencyMode.Optimized);
    }

    [Test]
    public async Task Build_WithServiceTier_SetsRequestMetadata()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithServiceTier(ServiceTier.Default);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.RequestMetadata).IsNotNull();
        await Assert.That(request.RequestMetadata!.ServiceTier).IsEqualTo(ServiceTier.Default);
    }

    [Test]
    public async Task Build_WithAdditionalModelFields_SetsAdditionalFields()
    {
        // Arrange
        var additionalFields = JsonDocument.Parse("""{"custom_param": true}""").RootElement;
        var builder = new ConverseBuilder(TestModelId)
            .WithAdditionalModelFields(additionalFields);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.AdditionalModelRequestFields).IsNotNull();
        await Assert.That(request.AdditionalModelRequestFields!.Value.GetProperty("custom_param").GetBoolean()).IsTrue();
    }

    [Test]
    public async Task Build_WithCustomMessage_AddsMessageWithContentBlocks()
    {
        // Arrange
        var contentBlocks = new List<ContentBlock>
        {
            new() { Text = "What is in this image?" },
            new()
            {
                Image = new ImageBlock
                {
                    Format = "png",
                    Source = new ImageSource { Bytes = "base64encodeddata" }
                }
            }
        };

        var builder = new ConverseBuilder(TestModelId)
            .AddMessage(ConversationRole.User, contentBlocks);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.Messages).Count().IsEqualTo(1);
        await Assert.That(request.Messages[0].Role).IsEqualTo(ConversationRole.User);
        await Assert.That(request.Messages[0].Content).Count().IsEqualTo(2);
        await Assert.That(request.Messages[0].Content[0].Text).IsEqualTo("What is in this image?");
        await Assert.That(request.Messages[0].Content[1].Image).IsNotNull();
        await Assert.That(request.Messages[0].Content[1].Image!.Format).IsEqualTo("png");
    }

    [Test]
    public async Task Build_CompleteConversation_SetsAllProperties()
    {
        // Arrange
        var inputSchema = JsonDocument.Parse("""{"type": "object", "properties": {"city": {"type": "string"}}}""").RootElement;

        var builder = new ConverseBuilder(TestModelId)
            .WithSystemPrompt("You are a helpful weather assistant.")
            .AddUserMessage("What's the weather like in Seattle?")
            .AddAssistantMessage("Let me check that for you.")
            .AddUserMessage("Thanks!")
            .WithMaxTokens(2048)
            .WithTemperature(0.5f)
            .WithTopP(0.95f)
            .WithStopSequences("###")
            .WithTool("get_weather", "Get weather for a city", inputSchema)
            .WithGuardrail("content-filter", "DRAFT")
            .WithPerformance(LatencyMode.Standard);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.ModelId).IsEqualTo(TestModelId);
        await Assert.That(request.System).Count().IsEqualTo(1);
        await Assert.That(request.System![0].Text).IsEqualTo("You are a helpful weather assistant.");
        await Assert.That(request.Messages).Count().IsEqualTo(3);
        await Assert.That(request.Messages[0].Role).IsEqualTo(ConversationRole.User);
        await Assert.That(request.Messages[1].Role).IsEqualTo(ConversationRole.Assistant);
        await Assert.That(request.Messages[2].Role).IsEqualTo(ConversationRole.User);
        await Assert.That(request.InferenceConfig!.MaxTokens).IsEqualTo(2048);
        await Assert.That(request.InferenceConfig!.Temperature).IsEqualTo(0.5f);
        await Assert.That(request.InferenceConfig!.TopP).IsEqualTo(0.95f);
        await Assert.That(request.InferenceConfig!.StopSequences).Count().IsEqualTo(1);
        await Assert.That(request.ToolConfig!.Tools).Count().IsEqualTo(1);
        await Assert.That(request.GuardrailConfig!.GuardrailIdentifier).IsEqualTo("content-filter");
        await Assert.That(request.PerformanceConfig!.Latency).IsEqualTo(LatencyMode.Standard);
    }

    [Test]
    public async Task Build_MultipleInferenceConfigCalls_MergesValues()
    {
        // Arrange
        var builder = new ConverseBuilder(TestModelId)
            .WithMaxTokens(1024)
            .WithTemperature(0.7f)
            .WithTopP(0.9f);

        // Act
        var request = builder.Build();

        // Assert
        await Assert.That(request.InferenceConfig).IsNotNull();
        await Assert.That(request.InferenceConfig!.MaxTokens).IsEqualTo(1024);
        await Assert.That(request.InferenceConfig!.Temperature).IsEqualTo(0.7f);
        await Assert.That(request.InferenceConfig!.TopP).IsEqualTo(0.9f);
    }
}
