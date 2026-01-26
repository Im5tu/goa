using System.Text.Json;
using ErrorOr;
using Goa.Clients.Bedrock.Conversation.Errors;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Dynamo.Extensions;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Bedrock.Conversation.Dynamo.Internal;

/// <summary>
/// Serializer for ContentBlock to/from DynamoDB AttributeValue.
/// </summary>
internal static class ContentBlockSerializer
{
    private const string TypeAttribute = "type";
    private const string TextType = "text";
    private const string ImageType = "image";
    private const string DocumentType = "document";
    private const string ToolUseType = "toolUse";
    private const string ToolResultType = "toolResult";

    /// <summary>
    /// Serializes a ContentBlock to an AttributeValue map.
    /// </summary>
    public static ErrorOr<AttributeValue> Serialize(ContentBlock block)
    {
        if (block.Text is not null)
        {
            return new AttributeValue
            {
                M = new Dictionary<string, AttributeValue>
                {
                    [TypeAttribute] = TextType,
                    ["text"] = block.Text
                }
            };
        }

        if (block.Image is not null)
        {
            var validation = ValidateS3OnlySource(block.Image.Source.Bytes, block.Image.Source.S3Location, "Image");
            if (validation.IsError)
                return validation.Errors;

            var imageMap = new Dictionary<string, AttributeValue>
            {
                [TypeAttribute] = ImageType,
                ["format"] = block.Image.Format
            };

            if (block.Image.Source.S3Location is not null)
            {
                imageMap["s3Uri"] = block.Image.Source.S3Location.Uri;
                if (block.Image.Source.S3Location.BucketOwner is not null)
                    imageMap["s3BucketOwner"] = block.Image.Source.S3Location.BucketOwner;
            }

            return new AttributeValue { M = imageMap };
        }

        if (block.Document is not null)
        {
            var validation = ValidateS3OnlySource(block.Document.Source.Bytes, block.Document.Source.S3Location, "Document");
            if (validation.IsError)
                return validation.Errors;

            var docMap = new Dictionary<string, AttributeValue>
            {
                [TypeAttribute] = DocumentType,
                ["format"] = block.Document.Format,
                ["name"] = block.Document.Name
            };

            if (block.Document.Source.S3Location is not null)
            {
                docMap["s3Uri"] = block.Document.Source.S3Location.Uri;
                if (block.Document.Source.S3Location.BucketOwner is not null)
                    docMap["s3BucketOwner"] = block.Document.Source.S3Location.BucketOwner;
            }

            return new AttributeValue { M = docMap };
        }

        if (block.ToolUse is not null)
        {
            var toolUseMap = new Dictionary<string, AttributeValue>
            {
                [TypeAttribute] = ToolUseType,
                ["toolUseId"] = block.ToolUse.ToolUseId,
                ["name"] = block.ToolUse.Name,
                ["input"] = block.ToolUse.Input.GetRawText()
            };

            return new AttributeValue { M = toolUseMap };
        }

        if (block.ToolResult is not null)
        {
            var contentList = new List<AttributeValue>();
            foreach (var content in block.ToolResult.Content)
            {
                var serialized = Serialize(content);
                if (serialized.IsError)
                    return serialized.Errors;
                contentList.Add(serialized.Value);
            }

            var toolResultMap = new Dictionary<string, AttributeValue>
            {
                [TypeAttribute] = ToolResultType,
                ["toolUseId"] = block.ToolResult.ToolUseId,
                ["content"] = new AttributeValue { L = contentList }
            };

            if (block.ToolResult.Status is not null)
                toolResultMap["status"] = block.ToolResult.Status;

            return new AttributeValue { M = toolResultMap };
        }

        return Error.Validation(ConversationErrorCodes.ContentBlockEmpty, "ContentBlock must have at least one content type set");
    }

    /// <summary>
    /// Deserializes an AttributeValue map to a ContentBlock.
    /// </summary>
    public static ErrorOr<ContentBlock> Deserialize(AttributeValue attributeValue)
    {
        if (attributeValue.M is null)
            return Error.Validation(ConversationErrorCodes.ContentBlockInvalidFormat, "ContentBlock must be a map");

        var record = new DynamoRecord(attributeValue.M);

        if (!record.TryGetString(TypeAttribute, out var type))
            return Error.Validation(ConversationErrorCodes.ContentBlockMissingType, "ContentBlock must have a type attribute");

        return type switch
        {
            TextType => DeserializeTextBlock(record),
            ImageType => DeserializeImageBlock(record),
            DocumentType => DeserializeDocumentBlock(record),
            ToolUseType => DeserializeToolUseBlock(record),
            ToolResultType => DeserializeToolResultBlock(record),
            _ => Error.Validation(ConversationErrorCodes.ContentBlockUnknownType, $"Unknown content block type: {type}")
        };
    }

    private static ErrorOr<ContentBlock> DeserializeTextBlock(DynamoRecord record)
    {
        if (!record.TryGetString("text", out var text))
            return Error.Validation(ConversationErrorCodes.ContentBlockTextMissing, "Text block must have text attribute");

        return new ContentBlock { Text = text };
    }

    private static ErrorOr<ContentBlock> DeserializeImageBlock(DynamoRecord record)
    {
        if (!record.TryGetString("format", out var format))
            return Error.Validation(ConversationErrorCodes.ContentBlockImageMissingFormat, "Image block must have format attribute");

        var source = new ImageSource();

        if (record.TryGetString("s3Uri", out var s3Uri))
        {
            source.S3Location = new S3Location { Uri = s3Uri };
            if (record.TryGetNullableString("s3BucketOwner", out var bucketOwner))
                source.S3Location.BucketOwner = bucketOwner;
        }

        return new ContentBlock
        {
            Image = new ImageBlock
            {
                Format = format,
                Source = source
            }
        };
    }

    private static ErrorOr<ContentBlock> DeserializeDocumentBlock(DynamoRecord record)
    {
        if (!record.TryGetString("format", out var format))
            return Error.Validation(ConversationErrorCodes.ContentBlockDocumentMissingFormat, "Document block must have format attribute");

        if (!record.TryGetString("name", out var name))
            return Error.Validation(ConversationErrorCodes.ContentBlockDocumentMissingName, "Document block must have name attribute");

        var source = new DocumentSource();

        if (record.TryGetString("s3Uri", out var s3Uri))
        {
            source.S3Location = new S3Location { Uri = s3Uri };
            if (record.TryGetNullableString("s3BucketOwner", out var bucketOwner))
                source.S3Location.BucketOwner = bucketOwner;
        }

        return new ContentBlock
        {
            Document = new DocumentBlock
            {
                Format = format,
                Name = name,
                Source = source
            }
        };
    }

    private static ErrorOr<ContentBlock> DeserializeToolUseBlock(DynamoRecord record)
    {
        if (!record.TryGetString("toolUseId", out var toolUseId))
            return Error.Validation(ConversationErrorCodes.ContentBlockToolUseMissingId, "ToolUse block must have toolUseId attribute");

        if (!record.TryGetString("name", out var name))
            return Error.Validation(ConversationErrorCodes.ContentBlockToolUseMissingName, "ToolUse block must have name attribute");

        if (!record.TryGetString("input", out var inputJson))
            return Error.Validation(ConversationErrorCodes.ContentBlockToolUseMissingInput, "ToolUse block must have input attribute");

        JsonElement input;
        try
        {
            input = JsonDocument.Parse(inputJson).RootElement.Clone();
        }
        catch (JsonException ex)
        {
            return Error.Validation(ConversationErrorCodes.ContentBlockToolUseInvalidInput, $"ToolUse input is not valid JSON: {ex.Message}");
        }

        return new ContentBlock
        {
            ToolUse = new ToolUseBlock
            {
                ToolUseId = toolUseId,
                Name = name,
                Input = input
            }
        };
    }

    private static ErrorOr<ContentBlock> DeserializeToolResultBlock(DynamoRecord record)
    {
        if (!record.TryGetString("toolUseId", out var toolUseId))
            return Error.Validation(ConversationErrorCodes.ContentBlockToolResultMissingId, "ToolResult block must have toolUseId attribute");

        if (!record.TryGetList("content", out var contentList) || contentList is null)
            return Error.Validation(ConversationErrorCodes.ContentBlockToolResultMissingContent, "ToolResult block must have content attribute");

        var content = new List<ContentBlock>();
        foreach (var item in contentList)
        {
            var deserialized = Deserialize(item);
            if (deserialized.IsError)
                return deserialized.Errors;
            content.Add(deserialized.Value);
        }

        record.TryGetNullableString("status", out var status);

        return new ContentBlock
        {
            ToolResult = new ToolResultBlock
            {
                ToolUseId = toolUseId,
                Content = content,
                Status = status
            }
        };
    }

    private static ErrorOr<Success> ValidateS3OnlySource(string? bytes, S3Location? s3Location, string blockType)
    {
        if (bytes is not null)
        {
            var bytesNotSupportedCode = blockType == "Image"
                ? ConversationErrorCodes.ContentBlockImageBytesNotSupported
                : ConversationErrorCodes.ContentBlockDocumentBytesNotSupported;
            return Error.Validation(bytesNotSupportedCode,
                $"{blockType} blocks with inline bytes cannot be stored. Use S3 references instead.");
        }

        if (s3Location is null)
        {
            var missingSourceCode = blockType == "Image"
                ? ConversationErrorCodes.ContentBlockImageMissingSource
                : ConversationErrorCodes.ContentBlockDocumentMissingSource;
            return Error.Validation(missingSourceCode,
                $"{blockType} blocks must have an S3 source for storage.");
        }

        return Result.Success;
    }
}