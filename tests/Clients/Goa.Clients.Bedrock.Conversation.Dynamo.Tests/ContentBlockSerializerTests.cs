using System.Text.Json;
using Goa.Clients.Bedrock.Conversation.Dynamo.Internal;
using Goa.Clients.Bedrock.Conversation.Errors;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Bedrock.Conversation.Dynamo.Tests;

public class ContentBlockSerializerTests
{
    [Test]
    public async Task Serialize_TextBlock_ShouldSerializeCorrectly()
    {
        var block = new ContentBlock { Text = "Hello, world!" };

        var result = ContentBlockSerializer.Serialize(block);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.M).IsNotNull();
        await Assert.That(result.Value.M!["type"].S).IsEqualTo("text");
        await Assert.That(result.Value.M!["text"].S).IsEqualTo("Hello, world!");
    }

    [Test]
    public async Task Serialize_ImageBlock_WithS3_ShouldSerializeCorrectly()
    {
        var block = new ContentBlock
        {
            Image = new ImageBlock
            {
                Format = "png",
                Source = new ImageSource
                {
                    S3Location = new S3Location
                    {
                        Uri = "s3://bucket/image.png",
                        BucketOwner = "123456789012"
                    }
                }
            }
        };

        var result = ContentBlockSerializer.Serialize(block);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.M).IsNotNull();
        await Assert.That(result.Value.M!["type"].S).IsEqualTo("image");
        await Assert.That(result.Value.M!["format"].S).IsEqualTo("png");
        await Assert.That(result.Value.M!["s3Uri"].S).IsEqualTo("s3://bucket/image.png");
        await Assert.That(result.Value.M!["s3BucketOwner"].S).IsEqualTo("123456789012");
    }

    [Test]
    public async Task Serialize_ImageBlock_WithBytes_ShouldReturnError()
    {
        var block = new ContentBlock
        {
            Image = new ImageBlock
            {
                Format = "png",
                Source = new ImageSource
                {
                    Bytes = "base64data"
                }
            }
        };

        var result = ContentBlockSerializer.Serialize(block);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(ConversationErrorCodes.ContentBlockImageBytesNotSupported);
    }

    [Test]
    public async Task Serialize_DocumentBlock_WithS3_ShouldSerializeCorrectly()
    {
        var block = new ContentBlock
        {
            Document = new DocumentBlock
            {
                Format = "pdf",
                Name = "document.pdf",
                Source = new DocumentSource
                {
                    S3Location = new S3Location { Uri = "s3://bucket/document.pdf" }
                }
            }
        };

        var result = ContentBlockSerializer.Serialize(block);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.M).IsNotNull();
        await Assert.That(result.Value.M!["type"].S).IsEqualTo("document");
        await Assert.That(result.Value.M!["format"].S).IsEqualTo("pdf");
        await Assert.That(result.Value.M!["name"].S).IsEqualTo("document.pdf");
        await Assert.That(result.Value.M!["s3Uri"].S).IsEqualTo("s3://bucket/document.pdf");
    }

    [Test]
    public async Task Serialize_DocumentBlock_WithBytes_ShouldReturnError()
    {
        var block = new ContentBlock
        {
            Document = new DocumentBlock
            {
                Format = "pdf",
                Name = "document.pdf",
                Source = new DocumentSource { Bytes = "base64data" }
            }
        };

        var result = ContentBlockSerializer.Serialize(block);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(ConversationErrorCodes.ContentBlockDocumentBytesNotSupported);
    }

    [Test]
    public async Task Serialize_ToolUseBlock_ShouldSerializeCorrectly()
    {
        var inputJson = JsonDocument.Parse("{\"query\": \"test\"}").RootElement;
        var block = new ContentBlock
        {
            ToolUse = new ToolUseBlock
            {
                ToolUseId = "tool-123",
                Name = "search",
                Input = inputJson
            }
        };

        var result = ContentBlockSerializer.Serialize(block);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.M).IsNotNull();
        await Assert.That(result.Value.M!["type"].S).IsEqualTo("toolUse");
        await Assert.That(result.Value.M!["toolUseId"].S).IsEqualTo("tool-123");
        await Assert.That(result.Value.M!["name"].S).IsEqualTo("search");
        // Compare parsed JSON instead of string to avoid formatting differences
        var serializedInput = result.Value.M!["input"].S;
        var parsedInput = JsonDocument.Parse(serializedInput!).RootElement;
        await Assert.That(parsedInput.GetProperty("query").GetString()).IsEqualTo("test");
    }

    [Test]
    public async Task Serialize_ToolResultBlock_ShouldSerializeCorrectly()
    {
        var block = new ContentBlock
        {
            ToolResult = new ToolResultBlock
            {
                ToolUseId = "tool-123",
                Status = "success",
                Content = [new ContentBlock { Text = "Result text" }]
            }
        };

        var result = ContentBlockSerializer.Serialize(block);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.M).IsNotNull();
        await Assert.That(result.Value.M!["type"].S).IsEqualTo("toolResult");
        await Assert.That(result.Value.M!["toolUseId"].S).IsEqualTo("tool-123");
        await Assert.That(result.Value.M!["status"].S).IsEqualTo("success");
        await Assert.That(result.Value.M!["content"].L).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Serialize_EmptyBlock_ShouldReturnError()
    {
        var block = new ContentBlock();

        var result = ContentBlockSerializer.Serialize(block);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(ConversationErrorCodes.ContentBlockEmpty);
    }

    [Test]
    public async Task Deserialize_TextBlock_ShouldDeserializeCorrectly()
    {
        var attributeValue = AttributeValue.FromMap(new Dictionary<string, AttributeValue>
        {
            ["type"] = AttributeValue.String("text"),
            ["text"] = AttributeValue.String("Hello, world!")
        });

        var result = ContentBlockSerializer.Deserialize(attributeValue);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Text).IsEqualTo("Hello, world!");
    }

    [Test]
    public async Task Deserialize_ImageBlock_ShouldDeserializeCorrectly()
    {
        var attributeValue = AttributeValue.FromMap(new Dictionary<string, AttributeValue>
        {
            ["type"] = AttributeValue.String("image"),
            ["format"] = AttributeValue.String("png"),
            ["s3Uri"] = AttributeValue.String("s3://bucket/image.png"),
            ["s3BucketOwner"] = AttributeValue.String("123456789012")
        });

        var result = ContentBlockSerializer.Deserialize(attributeValue);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Image).IsNotNull();
        await Assert.That(result.Value.Image!.Format).IsEqualTo("png");
        await Assert.That(result.Value.Image!.Source.S3Location).IsNotNull();
        await Assert.That(result.Value.Image!.Source.S3Location!.Uri).IsEqualTo("s3://bucket/image.png");
        await Assert.That(result.Value.Image!.Source.S3Location!.BucketOwner).IsEqualTo("123456789012");
    }

    [Test]
    public async Task Deserialize_DocumentBlock_ShouldDeserializeCorrectly()
    {
        var attributeValue = AttributeValue.FromMap(new Dictionary<string, AttributeValue>
        {
            ["type"] = AttributeValue.String("document"),
            ["format"] = AttributeValue.String("pdf"),
            ["name"] = AttributeValue.String("document.pdf"),
            ["s3Uri"] = AttributeValue.String("s3://bucket/document.pdf")
        });

        var result = ContentBlockSerializer.Deserialize(attributeValue);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Document).IsNotNull();
        await Assert.That(result.Value.Document!.Format).IsEqualTo("pdf");
        await Assert.That(result.Value.Document!.Name).IsEqualTo("document.pdf");
        await Assert.That(result.Value.Document!.Source.S3Location).IsNotNull();
    }

    [Test]
    public async Task Deserialize_ToolUseBlock_ShouldDeserializeCorrectly()
    {
        var attributeValue = AttributeValue.FromMap(new Dictionary<string, AttributeValue>
        {
            ["type"] = AttributeValue.String("toolUse"),
            ["toolUseId"] = AttributeValue.String("tool-123"),
            ["name"] = AttributeValue.String("search"),
            ["input"] = AttributeValue.String("{\"query\":\"test\"}")
        });

        var result = ContentBlockSerializer.Deserialize(attributeValue);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ToolUse).IsNotNull();
        await Assert.That(result.Value.ToolUse!.ToolUseId).IsEqualTo("tool-123");
        await Assert.That(result.Value.ToolUse!.Name).IsEqualTo("search");
        await Assert.That(result.Value.ToolUse!.Input.GetProperty("query").GetString()).IsEqualTo("test");
    }

    [Test]
    public async Task Deserialize_ToolResultBlock_ShouldDeserializeCorrectly()
    {
        var attributeValue = AttributeValue.FromMap(new Dictionary<string, AttributeValue>
        {
            ["type"] = AttributeValue.String("toolResult"),
            ["toolUseId"] = AttributeValue.String("tool-123"),
            ["status"] = AttributeValue.String("success"),
            ["content"] = AttributeValue.FromList(
            [
                AttributeValue.FromMap(new Dictionary<string, AttributeValue>
                {
                    ["type"] = AttributeValue.String("text"),
                    ["text"] = AttributeValue.String("Result")
                })
            ])
        });

        var result = ContentBlockSerializer.Deserialize(attributeValue);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ToolResult).IsNotNull();
        await Assert.That(result.Value.ToolResult!.ToolUseId).IsEqualTo("tool-123");
        await Assert.That(result.Value.ToolResult!.Status).IsEqualTo("success");
        await Assert.That(result.Value.ToolResult!.Content).Count().IsEqualTo(1);
        await Assert.That(result.Value.ToolResult!.Content[0].Text).IsEqualTo("Result");
    }

    [Test]
    public async Task Deserialize_InvalidFormat_ShouldReturnError()
    {
        var attributeValue = AttributeValue.String("not a map");

        var result = ContentBlockSerializer.Deserialize(attributeValue);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(ConversationErrorCodes.ContentBlockInvalidFormat);
    }

    [Test]
    public async Task Deserialize_MissingType_ShouldReturnError()
    {
        var attributeValue = AttributeValue.FromMap(new Dictionary<string, AttributeValue>
        {
            ["text"] = AttributeValue.String("Hello")
        });

        var result = ContentBlockSerializer.Deserialize(attributeValue);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(ConversationErrorCodes.ContentBlockMissingType);
    }

    [Test]
    public async Task Deserialize_UnknownType_ShouldReturnError()
    {
        var attributeValue = AttributeValue.FromMap(new Dictionary<string, AttributeValue>
        {
            ["type"] = AttributeValue.String("unknown")
        });

        var result = ContentBlockSerializer.Deserialize(attributeValue);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(ConversationErrorCodes.ContentBlockUnknownType);
    }

    [Test]
    public async Task RoundTrip_TextBlock_ShouldPreserveData()
    {
        var original = new ContentBlock { Text = "Hello, world!" };

        var serialized = ContentBlockSerializer.Serialize(original);
        await Assert.That(serialized.IsError).IsFalse();

        var deserialized = ContentBlockSerializer.Deserialize(serialized.Value);
        await Assert.That(deserialized.IsError).IsFalse();
        await Assert.That(deserialized.Value.Text).IsEqualTo(original.Text);
    }

    [Test]
    public async Task RoundTrip_ToolUseBlock_ShouldPreserveData()
    {
        var inputJson = JsonDocument.Parse("{\"query\": \"test\", \"nested\": {\"value\": 42}}").RootElement;
        var original = new ContentBlock
        {
            ToolUse = new ToolUseBlock
            {
                ToolUseId = "tool-123",
                Name = "search",
                Input = inputJson
            }
        };

        var serialized = ContentBlockSerializer.Serialize(original);
        await Assert.That(serialized.IsError).IsFalse();

        var deserialized = ContentBlockSerializer.Deserialize(serialized.Value);
        await Assert.That(deserialized.IsError).IsFalse();
        await Assert.That(deserialized.Value.ToolUse).IsNotNull();
        await Assert.That(deserialized.Value.ToolUse!.ToolUseId).IsEqualTo("tool-123");
        await Assert.That(deserialized.Value.ToolUse!.Name).IsEqualTo("search");
        await Assert.That(deserialized.Value.ToolUse!.Input.GetProperty("nested").GetProperty("value").GetInt32()).IsEqualTo(42);
    }
}