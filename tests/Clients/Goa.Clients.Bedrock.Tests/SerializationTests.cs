using System.Text.Json;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Operations.ApplyGuardrail;
using Goa.Clients.Bedrock.Operations.Converse;
using Goa.Clients.Bedrock.Operations.CountTokens;
using Goa.Clients.Bedrock.Operations.InvokeModel;
using Goa.Clients.Bedrock.Serialization;

namespace Goa.Clients.Bedrock.Tests;

public class SerializationTests
{
    [Test]
    public async Task ConverseRequest_RoundTrip_Succeeds()
    {
        // Arrange
        var request = new ConverseRequest
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
            ],
            InferenceConfig = new InferenceConfiguration
            {
                MaxTokens = 1024,
                Temperature = 0.7f,
                TopP = 0.9f,
                StopSequences = ["###"]
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, BedrockJsonContext.Default.ConverseRequest);
        var deserialized = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.ConverseRequest);

        // Assert
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.ModelId).IsEqualTo("anthropic.claude-3-sonnet-20240229-v1:0");
        await Assert.That(deserialized.Messages).Count().IsEqualTo(1);
        await Assert.That(deserialized.Messages[0].Role).IsEqualTo(ConversationRole.User);
        await Assert.That(deserialized.Messages[0].Content[0].Text).IsEqualTo("Hello, world!");
        await Assert.That(deserialized.System).Count().IsEqualTo(1);
        await Assert.That(deserialized.System![0].Text).IsEqualTo("You are a helpful assistant.");
        await Assert.That(deserialized.InferenceConfig!.MaxTokens).IsEqualTo(1024);
        await Assert.That(deserialized.InferenceConfig!.Temperature).IsEqualTo(0.7f);
        await Assert.That(deserialized.InferenceConfig!.TopP).IsEqualTo(0.9f);
        await Assert.That(deserialized.InferenceConfig!.StopSequences).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ConverseResponse_Deserialize_Succeeds()
    {
        // Arrange
        var json = """
        {
            "output": {
                "message": {
                    "role": "assistant",
                    "content": [
                        { "text": "Hello! How can I help you today?" }
                    ]
                }
            },
            "stopReason": "end_turn",
            "usage": {
                "inputTokens": 10,
                "outputTokens": 8,
                "totalTokens": 18
            },
            "metrics": {
                "latencyMs": 500
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.ConverseResponse);

        // Assert
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Output).IsNotNull();
        await Assert.That(response.Output!.Message).IsNotNull();
        await Assert.That(response.Output!.Message!.Role).IsEqualTo(ConversationRole.Assistant);
        await Assert.That(response.Output!.Message!.Content).Count().IsEqualTo(1);
        await Assert.That(response.Output!.Message!.Content[0].Text).IsEqualTo("Hello! How can I help you today?");
        await Assert.That(response.StopReason).IsEqualTo(StopReason.EndTurn);
        await Assert.That(response.Usage!.InputTokens).IsEqualTo(10);
        await Assert.That(response.Usage!.OutputTokens).IsEqualTo(8);
        await Assert.That(response.Usage!.TotalTokens).IsEqualTo(18);
        await Assert.That(response.Metrics!.LatencyMs).IsEqualTo(500);
    }

    [Test]
    public async Task Message_WithImageBlock_Serializes()
    {
        // Arrange
        var message = new Message
        {
            Role = ConversationRole.User,
            Content =
            [
                new ContentBlock { Text = "What is in this image?" },
                new ContentBlock
                {
                    Image = new ImageBlock
                    {
                        Format = "png",
                        Source = new ImageSource
                        {
                            Bytes = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
                        }
                    }
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(message, BedrockJsonContext.Default.Message);
        var deserialized = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.Message);

        // Assert
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Content).Count().IsEqualTo(2);
        await Assert.That(deserialized.Content[0].Text).IsEqualTo("What is in this image?");
        await Assert.That(deserialized.Content[1].Image).IsNotNull();
        await Assert.That(deserialized.Content[1].Image!.Format).IsEqualTo("png");
        await Assert.That(deserialized.Content[1].Image!.Source.Bytes).IsNotNull();
    }

    [Test]
    public async Task Message_WithImageFromS3_Serializes()
    {
        // Arrange
        var message = new Message
        {
            Role = ConversationRole.User,
            Content =
            [
                new ContentBlock
                {
                    Image = new ImageBlock
                    {
                        Format = "jpeg",
                        Source = new ImageSource
                        {
                            S3Location = new S3Location
                            {
                                Uri = "s3://my-bucket/images/photo.jpg",
                                BucketOwner = "123456789012"
                            }
                        }
                    }
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(message, BedrockJsonContext.Default.Message);
        var deserialized = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.Message);

        // Assert
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Content[0].Image!.Source.S3Location).IsNotNull();
        await Assert.That(deserialized.Content[0].Image!.Source.S3Location!.Uri).IsEqualTo("s3://my-bucket/images/photo.jpg");
        await Assert.That(deserialized.Content[0].Image!.Source.S3Location!.BucketOwner).IsEqualTo("123456789012");
    }

    [Test]
    public async Task Message_WithToolUse_Serializes()
    {
        // Arrange
        var toolInput = JsonDocument.Parse("""{"location": "Seattle", "unit": "celsius"}""").RootElement;
        var message = new Message
        {
            Role = ConversationRole.Assistant,
            Content =
            [
                new ContentBlock
                {
                    ToolUse = new ToolUseBlock
                    {
                        ToolUseId = "tool_123",
                        Name = "get_weather",
                        Input = toolInput
                    }
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(message, BedrockJsonContext.Default.Message);
        var deserialized = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.Message);

        // Assert
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Content).Count().IsEqualTo(1);
        await Assert.That(deserialized.Content[0].ToolUse).IsNotNull();
        await Assert.That(deserialized.Content[0].ToolUse!.ToolUseId).IsEqualTo("tool_123");
        await Assert.That(deserialized.Content[0].ToolUse!.Name).IsEqualTo("get_weather");
        await Assert.That(deserialized.Content[0].ToolUse!.Input.GetProperty("location").GetString()).IsEqualTo("Seattle");
    }

    [Test]
    public async Task Message_WithToolResult_Serializes()
    {
        // Arrange
        var message = new Message
        {
            Role = ConversationRole.User,
            Content =
            [
                new ContentBlock
                {
                    ToolResult = new ToolResultBlock
                    {
                        ToolUseId = "tool_123",
                        Content = [new ContentBlock { Text = "The current temperature in Seattle is 15C." }],
                        Status = "success"
                    }
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(message, BedrockJsonContext.Default.Message);
        var deserialized = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.Message);

        // Assert
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Content).Count().IsEqualTo(1);
        await Assert.That(deserialized.Content[0].ToolResult).IsNotNull();
        await Assert.That(deserialized.Content[0].ToolResult!.ToolUseId).IsEqualTo("tool_123");
        await Assert.That(deserialized.Content[0].ToolResult!.Status).IsEqualTo("success");
    }

    [Test]
    public async Task InvokeModelRequest_HasCorrectProperties()
    {
        // Arrange
        var request = new InvokeModelRequest
        {
            ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
            Body = """{"prompt": "Hello"}""",
            ContentType = "application/json",
            Accept = "application/json",
            GuardrailIdentifier = "my-guardrail",
            GuardrailVersion = "1",
            PerformanceConfigLatency = LatencyMode.Optimized,
            ServiceTier = ServiceTier.Default
        };

        // Assert - InvokeModelRequest properties are marked with JsonIgnore
        // so we verify the object itself has correct values
        await Assert.That(request.ModelId).IsEqualTo("anthropic.claude-3-sonnet-20240229-v1:0");
        await Assert.That(request.Body).IsEqualTo("""{"prompt": "Hello"}""");
        await Assert.That(request.ContentType).IsEqualTo("application/json");
        await Assert.That(request.Accept).IsEqualTo("application/json");
        await Assert.That(request.GuardrailIdentifier).IsEqualTo("my-guardrail");
        await Assert.That(request.GuardrailVersion).IsEqualTo("1");
        await Assert.That(request.PerformanceConfigLatency).IsEqualTo(LatencyMode.Optimized);
        await Assert.That(request.ServiceTier).IsEqualTo(ServiceTier.Default);
    }

    [Test]
    public async Task ApplyGuardrailRequest_Serializes()
    {
        // Arrange
        var request = new ApplyGuardrailRequest
        {
            GuardrailIdentifier = "my-guardrail",
            GuardrailVersion = "1",
            Source = "INPUT",
            Content =
            [
                new GuardrailContentBlock
                {
                    Text = new GuardrailTextBlock
                    {
                        Text = "Test content for guardrail",
                        Qualifiers = [GuardrailTextQualifier.Query]
                    }
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(request, BedrockJsonContext.Default.ApplyGuardrailRequest);
        var deserialized = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.ApplyGuardrailRequest);

        // Assert
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.GuardrailIdentifier).IsEqualTo("my-guardrail");
        await Assert.That(deserialized.GuardrailVersion).IsEqualTo("1");
        await Assert.That(deserialized.Source).IsEqualTo("INPUT");
        await Assert.That(deserialized.Content).Count().IsEqualTo(1);
        await Assert.That(deserialized.Content[0].Text!.Text).IsEqualTo("Test content for guardrail");
    }

    [Test]
    public async Task CountTokensRequest_Serializes()
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
                    Content = [new ContentBlock { Text = "Count these tokens please." }]
                }
            ],
            System = [new SystemContentBlock { Text = "You are a token counter." }]
        };

        // Act
        var json = JsonSerializer.Serialize(request, BedrockJsonContext.Default.CountTokensRequest);
        var deserialized = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.CountTokensRequest);

        // Assert
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.ModelId).IsEqualTo("anthropic.claude-3-sonnet-20240229-v1:0");
        await Assert.That(deserialized.Messages).Count().IsEqualTo(1);
        await Assert.That(deserialized.Messages[0].Content[0].Text).IsEqualTo("Count these tokens please.");
        await Assert.That(deserialized.System).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ConverseRequest_WithToolConfig_Serializes()
    {
        // Arrange
        var inputSchema = JsonDocument.Parse("""{"type": "object", "properties": {"query": {"type": "string"}}}""").RootElement;
        var request = new ConverseRequest
        {
            ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
            Messages =
            [
                new Message
                {
                    Role = ConversationRole.User,
                    Content = [new ContentBlock { Text = "Search for something." }]
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
                            Name = "search",
                            Description = "Search the web",
                            InputSchema = new ToolInputSchema { Json = inputSchema }
                        }
                    }
                ],
                ToolChoice = new ToolChoice
                {
                    Auto = new AutoToolChoice()
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, BedrockJsonContext.Default.ConverseRequest);

        // Assert
        await Assert.That(json).Contains("toolConfig");
        await Assert.That(json).Contains("search");
        await Assert.That(json).Contains("Search the web");
        await Assert.That(json).Contains("auto");
    }

    [Test]
    public async Task ConverseResponse_WithToolUse_Deserializes()
    {
        // Arrange
        var json = """
        {
            "output": {
                "message": {
                    "role": "assistant",
                    "content": [
                        {
                            "toolUse": {
                                "toolUseId": "tool_abc123",
                                "name": "get_weather",
                                "input": {"location": "New York", "unit": "fahrenheit"}
                            }
                        }
                    ]
                }
            },
            "stopReason": "tool_use",
            "usage": {
                "inputTokens": 50,
                "outputTokens": 25,
                "totalTokens": 75
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.ConverseResponse);

        // Assert
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.StopReason).IsEqualTo(StopReason.ToolUse);
        await Assert.That(response.Output!.Message!.Content[0].ToolUse).IsNotNull();
        await Assert.That(response.Output!.Message!.Content[0].ToolUse!.ToolUseId).IsEqualTo("tool_abc123");
        await Assert.That(response.Output!.Message!.Content[0].ToolUse!.Name).IsEqualTo("get_weather");
    }

    [Test]
    public async Task ConverseRequest_NullPropertiesOmitted_WhenSerializing()
    {
        // Arrange
        var request = new ConverseRequest
        {
            ModelId = "test-model",
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
        var json = JsonSerializer.Serialize(request, BedrockJsonContext.Default.ConverseRequest);

        // Assert - null properties should be omitted due to JsonIgnoreCondition.WhenWritingNull
        await Assert.That(json).DoesNotContain("system");
        await Assert.That(json).DoesNotContain("inferenceConfig");
        await Assert.That(json).DoesNotContain("toolConfig");
        await Assert.That(json).DoesNotContain("guardrailConfig");
    }

    [Test]
    public async Task StopReason_AllValues_SerializeCorrectly()
    {
        // Test all StopReason enum values serialize correctly with snake_case
        var values = new[]
        {
            (StopReason.EndTurn, "end_turn"),
            (StopReason.ToolUse, "tool_use"),
            (StopReason.MaxTokens, "max_tokens"),
            (StopReason.StopSequence, "stop_sequence"),
            (StopReason.GuardrailIntervened, "guardrail_intervened"),
            (StopReason.ContentFiltered, "content_filtered"),
            (StopReason.ModelContextWindowExceeded, "model_context_window_exceeded")
        };

        foreach (var (value, expectedString) in values)
        {
            var json = JsonSerializer.Serialize(value, BedrockJsonContext.Default.StopReason);
            await Assert.That(json).IsEqualTo($"\"{expectedString}\"");
        }
    }

    [Test]
    public async Task ConversationRole_SerializesCorrectly()
    {
        // Arrange
        var userRole = ConversationRole.User;
        var assistantRole = ConversationRole.Assistant;

        // Act
        var userJson = JsonSerializer.Serialize(userRole, BedrockJsonContext.Default.ConversationRole);
        var assistantJson = JsonSerializer.Serialize(assistantRole, BedrockJsonContext.Default.ConversationRole);

        // Assert - lowercase as required by Bedrock API
        await Assert.That(userJson).IsEqualTo("\"user\"");
        await Assert.That(assistantJson).IsEqualTo("\"assistant\"");
    }

    [Test]
    public async Task LatencyMode_SerializesCorrectly()
    {
        // Arrange
        var standard = LatencyMode.Standard;
        var optimized = LatencyMode.Optimized;

        // Act
        var standardJson = JsonSerializer.Serialize(standard, BedrockJsonContext.Default.LatencyMode);
        var optimizedJson = JsonSerializer.Serialize(optimized, BedrockJsonContext.Default.LatencyMode);

        // Assert - lowercase as required by Bedrock API
        await Assert.That(standardJson).IsEqualTo("\"standard\"");
        await Assert.That(optimizedJson).IsEqualTo("\"optimized\"");
    }

    [Test]
    public async Task ServiceTier_SerializesCorrectly()
    {
        // Arrange
        var defaultTier = ServiceTier.Default;
        var priorityTier = ServiceTier.Priority;
        var flexTier = ServiceTier.Flex;

        // Act
        var defaultJson = JsonSerializer.Serialize(defaultTier, BedrockJsonContext.Default.ServiceTier);
        var priorityJson = JsonSerializer.Serialize(priorityTier, BedrockJsonContext.Default.ServiceTier);
        var flexJson = JsonSerializer.Serialize(flexTier, BedrockJsonContext.Default.ServiceTier);

        // Assert - lowercase as required by Bedrock API
        await Assert.That(defaultJson).IsEqualTo("\"default\"");
        await Assert.That(priorityJson).IsEqualTo("\"priority\"");
        await Assert.That(flexJson).IsEqualTo("\"flex\"");
    }

    [Test]
    public async Task StopReason_AllValues_DeserializeCorrectly()
    {
        // Test all StopReason enum values deserialize correctly from snake_case
        var values = new[]
        {
            ("end_turn", StopReason.EndTurn),
            ("tool_use", StopReason.ToolUse),
            ("max_tokens", StopReason.MaxTokens),
            ("stop_sequence", StopReason.StopSequence),
            ("guardrail_intervened", StopReason.GuardrailIntervened),
            ("content_filtered", StopReason.ContentFiltered),
            ("model_context_window_exceeded", StopReason.ModelContextWindowExceeded)
        };

        foreach (var (jsonValue, expectedEnum) in values)
        {
            var result = JsonSerializer.Deserialize($"\"{jsonValue}\"", BedrockJsonContext.Default.StopReason);
            await Assert.That(result).IsEqualTo(expectedEnum);
        }
    }

    [Test]
    public async Task ServiceTier_AllValues_DeserializeCorrectly()
    {
        // Test all ServiceTier enum values deserialize correctly from lowercase
        var values = new[]
        {
            ("default", ServiceTier.Default),
            ("priority", ServiceTier.Priority),
            ("flex", ServiceTier.Flex)
        };

        foreach (var (jsonValue, expectedEnum) in values)
        {
            var result = JsonSerializer.Deserialize($"\"{jsonValue}\"", BedrockJsonContext.Default.ServiceTier);
            await Assert.That(result).IsEqualTo(expectedEnum);
        }
    }

    [Test]
    public async Task LatencyMode_AllValues_DeserializeCorrectly()
    {
        // Test all LatencyMode enum values deserialize correctly from lowercase
        var values = new[]
        {
            ("standard", LatencyMode.Standard),
            ("optimized", LatencyMode.Optimized)
        };

        foreach (var (jsonValue, expectedEnum) in values)
        {
            var result = JsonSerializer.Deserialize($"\"{jsonValue}\"", BedrockJsonContext.Default.LatencyMode);
            await Assert.That(result).IsEqualTo(expectedEnum);
        }
    }

    [Test]
    public async Task ConversationRole_AllValues_DeserializeCorrectly()
    {
        // Test all ConversationRole enum values deserialize correctly from lowercase
        var values = new[]
        {
            ("user", ConversationRole.User),
            ("assistant", ConversationRole.Assistant)
        };

        foreach (var (jsonValue, expectedEnum) in values)
        {
            var result = JsonSerializer.Deserialize($"\"{jsonValue}\"", BedrockJsonContext.Default.ConversationRole);
            await Assert.That(result).IsEqualTo(expectedEnum);
        }
    }
}
