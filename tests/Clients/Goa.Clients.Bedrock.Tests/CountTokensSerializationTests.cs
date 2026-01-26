using System.Text.Json;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Operations.Converse;
using Goa.Clients.Bedrock.Operations.CountTokens;
using Goa.Clients.Bedrock.Serialization;

namespace Goa.Clients.Bedrock.Tests;

public class CountTokensSerializationTests
{
    [Test]
    public async Task CountTokensRequest_ShouldSerializeCorrectly()
    {
        // Arrange
        var request = new CountTokensRequest
        {
            ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
            Messages =
            [
                new Message
                {
                    Role = ConversationRole.User,
                    Content = [new ContentBlock { Text = "Hello, world!" }]
                }
            ],
            System =
            [
                new SystemContentBlock { Text = "You are a helpful assistant." }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(request, BedrockJsonContext.Default.CountTokensRequest);
        var deserialized = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.CountTokensRequest);

        // Assert
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.ModelId).IsEqualTo("anthropic.claude-3-sonnet-20240229-v1:0");
        await Assert.That(deserialized.Messages).Count().IsEqualTo(1);
        await Assert.That(deserialized.Messages[0].Role).IsEqualTo(ConversationRole.User);
        await Assert.That(deserialized.Messages[0].Content[0].Text).IsEqualTo("Hello, world!");
        await Assert.That(deserialized.System).Count().IsEqualTo(1);
        await Assert.That(deserialized.System![0].Text).IsEqualTo("You are a helpful assistant.");
    }

    [Test]
    public async Task CountTokensRequest_WithToolConfig_ShouldSerializeCorrectly()
    {
        // Arrange
        var inputSchema = JsonDocument.Parse("""{"type": "object", "properties": {"location": {"type": "string"}}}""").RootElement;
        var request = new CountTokensRequest
        {
            ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
            Messages =
            [
                new Message
                {
                    Role = ConversationRole.User,
                    Content = [new ContentBlock { Text = "What is the weather?" }]
                }
            ],
            ToolConfig = new ToolConfiguration
            {
                Tools =
                [
                    new Tool
                    {
                        ToolSpec = new ToolSpec
                        {
                            Name = "get_weather",
                            Description = "Get current weather",
                            InputSchema = new ToolInputSchema { Json = inputSchema }
                        }
                    }
                ]
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, BedrockJsonContext.Default.CountTokensRequest);

        // Assert
        await Assert.That(json).Contains("toolConfig");
        await Assert.That(json).Contains("get_weather");
    }

    [Test]
    public async Task CountTokensResponse_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "inputTokens": 42
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.CountTokensResponse);

        // Assert
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.InputTokens).IsEqualTo(42);
    }

    [Test]
    public async Task CountTokensResponse_WithZeroTokens_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "inputTokens": 0
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.CountTokensResponse);

        // Assert
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.InputTokens).IsEqualTo(0);
    }

    [Test]
    public async Task CountTokensRequest_MinimalRequest_ShouldSerializeCorrectly()
    {
        // Arrange
        var request = new CountTokensRequest
        {
            ModelId = "amazon.titan-text-express-v1",
            Messages =
            [
                new Message
                {
                    Role = ConversationRole.User,
                    Content = [new ContentBlock { Text = "Hi" }]
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(request, BedrockJsonContext.Default.CountTokensRequest);

        // Assert
        await Assert.That(json).Contains("modelId");
        await Assert.That(json).Contains("messages");
        await Assert.That(json).DoesNotContain("system"); // Should be null and excluded
        await Assert.That(json).DoesNotContain("toolConfig"); // Should be null and excluded
    }
}
