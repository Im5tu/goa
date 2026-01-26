using System.Text;
using System.Text.Json;
using ErrorOr;
using Goa.Clients.Bedrock.Conversation.Dynamo.Internal;
using Goa.Clients.Bedrock.Conversation.Dynamo.Serialization;
using Goa.Clients.Bedrock.Conversation.Entities;
using Goa.Clients.Bedrock.Conversation.Errors;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Dynamo;
using Goa.Clients.Dynamo.Extensions;
using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.PutItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Transactions;
using Goa.Clients.Dynamo.Operations.UpdateItem;

namespace Goa.Clients.Bedrock.Conversation.Dynamo;

/// <summary>
/// DynamoDB-backed implementation of IConversationStore.
/// </summary>
public sealed class DynamoConversationStore : IConversationStore
{
    private readonly IDynamoClient _dynamoClient;
    private readonly DynamoConversationStoreConfiguration _configuration;

    private const string IdAttribute = "Id";
    private const string CreatedAtAttribute = "CreatedAt";
    private const string UpdatedAtAttribute = "UpdatedAt";
    private const string MessageCountAttribute = "MessageCount";
    private const string TitleAttribute = "Title";
    private const string TagsAttribute = "Tags";
    private const string ModelIdAttribute = "ModelId";
    private const string CustomDataAttribute = "CustomData";
    private const string TotalInputTokensAttribute = "TotalInputTokens";
    private const string TotalOutputTokensAttribute = "TotalOutputTokens";
    private const string TotalTokensAttribute = "TotalTokens";
    private const string ConversationIdAttribute = "ConversationId";
    private const string SequenceNumberAttribute = "SequenceNumber";
    private const string RoleAttribute = "Role";
    private const string ContentAttribute = "Content";
    private const string InputTokensAttribute = "InputTokens";
    private const string OutputTokensAttribute = "OutputTokens";
    private const string TokensAttribute = "Tokens";

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoConversationStore"/> class.
    /// </summary>
    /// <param name="dynamoClient">The DynamoDB client.</param>
    /// <param name="configuration">The store configuration.</param>
    public DynamoConversationStore(IDynamoClient dynamoClient, DynamoConversationStoreConfiguration configuration)
    {
        _dynamoClient = dynamoClient ?? throw new ArgumentNullException(nameof(dynamoClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public async Task<ErrorOr<Entities.Conversation>> CreateConversationAsync(ConversationMetadata? metadata, CancellationToken ct)
    {
        var conversationId = Guid.CreateVersion7().ToString("N");
        var now = DateTimeOffset.UtcNow;

        var item = new Dictionary<string, AttributeValue>
        {
            [_configuration.PartitionKeyName] = _configuration.ConversationPkFormat(conversationId),
            [_configuration.SortKeyName] = _configuration.ConversationSkValue,
            [IdAttribute] = conversationId,
            [CreatedAtAttribute] = new AttributeValue { N = now.ToUnixTimeSeconds().ToString() },
            [UpdatedAtAttribute] = new AttributeValue { N = now.ToUnixTimeSeconds().ToString() },
            [MessageCountAttribute] = new AttributeValue { N = "0" }
        };

        if (metadata is not null)
        {
            SerializeMetadata(metadata, item);
        }

        if (_configuration.DefaultTtl.HasValue)
        {
            var expiresAt = now.Add(_configuration.DefaultTtl.Value).ToUnixTimeSeconds();
            item[_configuration.TtlAttributeName] = new AttributeValue { N = expiresAt.ToString() };
        }

        var request = new PutItemRequest
        {
            TableName = _configuration.TableName,
            Item = item,
            ConditionExpression = $"attribute_not_exists({_configuration.PartitionKeyName})"
        };

        var result = await _dynamoClient.PutItemAsync(request, ct);
        if (result.IsError)
            return result.Errors;

        return new Entities.Conversation
        {
            Id = conversationId,
            Metadata = metadata,
            CreatedAt = now,
            UpdatedAt = now,
            MessageCount = 0
        };
    }

    /// <inheritdoc />
    public async Task<ErrorOr<Entities.Conversation>> GetConversationAsync(string conversationId, CancellationToken ct)
    {
        var request = new GetItemRequest
        {
            TableName = _configuration.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [_configuration.PartitionKeyName] = _configuration.ConversationPkFormat(conversationId),
                [_configuration.SortKeyName] = _configuration.ConversationSkValue
            }
        };

        var result = await _dynamoClient.GetItemAsync(request, ct);
        if (result.IsError)
            return result.Errors;

        if (result.Value.Item is null)
            return Error.NotFound(ConversationErrorCodes.NotFound, $"Conversation with ID '{conversationId}' was not found");

        return DeserializeConversation(result.Value.Item);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<ConversationWithMessages>> GetConversationWithMessagesAsync(
        string conversationId,
        int? limit,
        string? paginationToken,
        CancellationToken ct)
    {
        var conversationResult = await GetConversationAsync(conversationId, ct);
        if (conversationResult.IsError)
            return conversationResult.Errors;

        var effectiveLimit = limit ?? _configuration.DefaultMessageLimit;

        var queryRequest = new QueryRequest
        {
            TableName = _configuration.TableName,
            KeyConditionExpression = $"{_configuration.PartitionKeyName} = :pk AND begins_with({_configuration.SortKeyName}, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = _configuration.ConversationPkFormat(conversationId),
                [":skPrefix"] = _configuration.MessageSkPrefix
            },
            Limit = effectiveLimit,
            ScanIndexForward = true
        };

        if (!string.IsNullOrEmpty(paginationToken))
        {
            var exclusiveStartKey = DecodePaginationToken(paginationToken);
            if (exclusiveStartKey.IsError)
                return exclusiveStartKey.Errors;
            queryRequest.ExclusiveStartKey = exclusiveStartKey.Value;
        }

        var queryResult = await _dynamoClient.QueryAsync(queryRequest, ct);
        if (queryResult.IsError)
            return queryResult.Errors;

        var messages = new List<ConversationMessage>();
        foreach (var item in queryResult.Value.Items)
        {
            var message = DeserializeMessage(item);
            if (message.IsError)
                return message.Errors;
            messages.Add(message.Value);
        }

        string? nextToken = null;
        if (queryResult.Value.HasMoreResults && queryResult.Value.LastEvaluatedKey is not null)
        {
            nextToken = EncodePaginationToken(queryResult.Value.LastEvaluatedKey);
        }

        return new ConversationWithMessages
        {
            Conversation = conversationResult.Value,
            Messages = messages,
            NextPaginationToken = nextToken,
            HasMoreMessages = queryResult.Value.HasMoreResults
        };
    }

    /// <inheritdoc />
    public async Task<ErrorOr<ConversationMessage>> AddMessageAsync(
        string conversationId,
        ConversationRole role,
        Message message,
        TokenUsage? tokenUsage,
        CancellationToken ct)
    {
        var messagesResult = await AddMessagesAsync(conversationId, [(role, message, tokenUsage)], ct);
        if (messagesResult.IsError)
            return messagesResult.Errors;

        return messagesResult.Value[0];
    }

    /// <inheritdoc />
    public async Task<ErrorOr<IReadOnlyList<ConversationMessage>>> AddMessagesAsync(
        string conversationId,
        IEnumerable<(ConversationRole Role, Message Message, TokenUsage? TokenUsage)> messages,
        CancellationToken ct)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return Error.Validation(ConversationErrorCodes.MessagesEmpty, "At least one message must be provided");

        // Get current conversation to determine sequence numbers and TTL
        var conversationResult = await GetConversationAsync(conversationId, ct);
        if (conversationResult.IsError)
            return conversationResult.Errors;

        var conversation = conversationResult.Value;
        var now = DateTimeOffset.UtcNow;
        var startSequence = conversation.MessageCount + 1;

        var transactItems = new List<TransactWriteItem>();
        var createdMessages = new List<ConversationMessage>();

        // Calculate total tokens for conversation update
        var totalInputTokens = 0;
        var totalOutputTokens = 0;
        var totalTokens = 0;

        for (var i = 0; i < messageList.Count; i++)
        {
            var (role, message, tokenUsage) = messageList[i];
            var sequenceNumber = startSequence + i;
            var messageId = Guid.NewGuid().ToString("N");

            var messageItem = new Dictionary<string, AttributeValue>
            {
                [_configuration.PartitionKeyName] = _configuration.ConversationPkFormat(conversationId),
                [_configuration.SortKeyName] = _configuration.MessageSkFormat(conversationId, sequenceNumber),
                [IdAttribute] = messageId,
                [ConversationIdAttribute] = conversationId,
                [SequenceNumberAttribute] = new AttributeValue { N = sequenceNumber.ToString() },
                [RoleAttribute] = role.ToString(),
                [CreatedAtAttribute] = new AttributeValue { N = now.ToUnixTimeSeconds().ToString() }
            };

            // Serialize content blocks
            var contentList = new List<AttributeValue>();
            foreach (var block in message.Content)
            {
                var serialized = ContentBlockSerializer.Serialize(block);
                if (serialized.IsError)
                    return serialized.Errors;
                contentList.Add(serialized.Value);
            }
            messageItem[ContentAttribute] = new AttributeValue { L = contentList };

            // Add token usage if present
            if (tokenUsage is not null)
            {
                messageItem[InputTokensAttribute] = new AttributeValue { N = tokenUsage.InputTokens.ToString() };
                messageItem[OutputTokensAttribute] = new AttributeValue { N = tokenUsage.OutputTokens.ToString() };
                messageItem[TokensAttribute] = new AttributeValue { N = tokenUsage.TotalTokens.ToString() };

                totalInputTokens += tokenUsage.InputTokens;
                totalOutputTokens += tokenUsage.OutputTokens;
                totalTokens += tokenUsage.TotalTokens;
            }

            // Inherit conversation TTL
            if (_configuration.DefaultTtl.HasValue)
            {
                var expiresAt = now.Add(_configuration.DefaultTtl.Value).ToUnixTimeSeconds();
                messageItem[_configuration.TtlAttributeName] = new AttributeValue { N = expiresAt.ToString() };
            }

            transactItems.Add(new TransactWriteItem
            {
                Put = new TransactPutItem
                {
                    TableName = _configuration.TableName,
                    Item = messageItem
                }
            });

            createdMessages.Add(new ConversationMessage
            {
                Id = messageId,
                ConversationId = conversationId,
                SequenceNumber = sequenceNumber,
                Role = role,
                Message = message,
                TokenUsage = tokenUsage,
                CreatedAt = now
            });
        }

        // Build update expression for conversation
        var updateParts = new List<string>
        {
            $"{MessageCountAttribute} = {MessageCountAttribute} + :msgCount",
            $"{UpdatedAtAttribute} = :updatedAt"
        };

        var updateExpressionValues = new Dictionary<string, AttributeValue>
        {
            [":msgCount"] = new AttributeValue { N = messageList.Count.ToString() },
            [":updatedAt"] = new AttributeValue { N = now.ToUnixTimeSeconds().ToString() }
        };

        if (totalTokens > 0)
        {
            updateParts.Add($"{TotalInputTokensAttribute} = if_not_exists({TotalInputTokensAttribute}, :zero) + :inputTokens");
            updateParts.Add($"{TotalOutputTokensAttribute} = if_not_exists({TotalOutputTokensAttribute}, :zero) + :outputTokens");
            updateParts.Add($"{TotalTokensAttribute} = if_not_exists({TotalTokensAttribute}, :zero) + :totalTokens");
            updateExpressionValues[":zero"] = new AttributeValue { N = "0" };
            updateExpressionValues[":inputTokens"] = new AttributeValue { N = totalInputTokens.ToString() };
            updateExpressionValues[":outputTokens"] = new AttributeValue { N = totalOutputTokens.ToString() };
            updateExpressionValues[":totalTokens"] = new AttributeValue { N = totalTokens.ToString() };
        }

        transactItems.Add(new TransactWriteItem
        {
            Update = new TransactUpdateItem
            {
                TableName = _configuration.TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    [_configuration.PartitionKeyName] = _configuration.ConversationPkFormat(conversationId),
                    [_configuration.SortKeyName] = _configuration.ConversationSkValue
                },
                UpdateExpression = $"SET {string.Join(", ", updateParts)}",
                ExpressionAttributeValues = updateExpressionValues
            }
        });

        var transactRequest = new TransactWriteRequest
        {
            TransactItems = transactItems
        };

        var transactResult = await _dynamoClient.TransactWriteItemsAsync(transactRequest, ct);
        if (transactResult.IsError)
            return transactResult.Errors;

        return createdMessages;
    }

    /// <inheritdoc />
    public async Task<ErrorOr<Entities.Conversation>> UpdateConversationAsync(
        string conversationId,
        ConversationMetadata metadata,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var updateParts = new List<string> { $"{UpdatedAtAttribute} = :updatedAt" };
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            [":updatedAt"] = new AttributeValue { N = now.ToUnixTimeSeconds().ToString() }
        };
        var expressionAttributeNames = new Dictionary<string, string>();

        if (metadata.Title is not null)
        {
            updateParts.Add($"{TitleAttribute} = :title");
            expressionAttributeValues[":title"] = metadata.Title;
        }

        if (metadata.ModelId is not null)
        {
            updateParts.Add($"{ModelIdAttribute} = :modelId");
            expressionAttributeValues[":modelId"] = metadata.ModelId;
        }

        if (metadata.Tags.Count > 0)
        {
            updateParts.Add($"{TagsAttribute} = :tags");
            expressionAttributeValues[":tags"] = new AttributeValue { SS = metadata.Tags };
        }

        if (metadata.CustomData.Count > 0)
        {
            updateParts.Add($"#customData = :customData");
            expressionAttributeNames["#customData"] = CustomDataAttribute;
            var customDataMap = new Dictionary<string, AttributeValue>();
            foreach (var kvp in metadata.CustomData)
            {
                customDataMap[kvp.Key] = kvp.Value;
            }
            expressionAttributeValues[":customData"] = new AttributeValue { M = customDataMap };
        }

        var updateRequest = new UpdateItemRequest
        {
            TableName = _configuration.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [_configuration.PartitionKeyName] = _configuration.ConversationPkFormat(conversationId),
                [_configuration.SortKeyName] = _configuration.ConversationSkValue
            },
            UpdateExpression = $"SET {string.Join(", ", updateParts)}",
            ConditionExpression = $"attribute_exists({_configuration.PartitionKeyName})",
            ExpressionAttributeValues = expressionAttributeValues
        };

        if (expressionAttributeNames.Count > 0)
        {
            updateRequest.ExpressionAttributeNames = expressionAttributeNames;
        }

        var result = await _dynamoClient.UpdateItemAsync(updateRequest, ct);
        if (result.IsError)
            return result.Errors;

        return await GetConversationAsync(conversationId, ct);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<Deleted>> DeleteConversationAsync(string conversationId, CancellationToken ct)
    {
        // Query all items for this conversation
        var pk = _configuration.ConversationPkFormat(conversationId);
        var allItems = new List<Dictionary<string, AttributeValue>>();

        Dictionary<string, AttributeValue>? lastKey = null;
        do
        {
            var queryRequest = new QueryRequest
            {
                TableName = _configuration.TableName,
                KeyConditionExpression = $"{_configuration.PartitionKeyName} = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = pk
                }
            };

            if (lastKey is not null && lastKey.Count > 0)
            {
                queryRequest.ExclusiveStartKey = lastKey;
            }

            var queryResult = await _dynamoClient.QueryAsync(queryRequest, ct);
            if (queryResult.IsError)
                return queryResult.Errors;

            foreach (var item in queryResult.Value.Items)
            {
                allItems.Add(new Dictionary<string, AttributeValue>
                {
                    [_configuration.PartitionKeyName] = item[_configuration.PartitionKeyName]!,
                    [_configuration.SortKeyName] = item[_configuration.SortKeyName]!
                });
            }

            lastKey = queryResult.Value.LastEvaluatedKey;
        } while (lastKey is not null && lastKey.Count > 0);

        if (allItems.Count == 0)
            return Error.NotFound(ConversationErrorCodes.NotFound, $"Conversation with ID '{conversationId}' was not found");

        // Delete in batches of 25 (transaction limit)
        const int batchSize = 25;
        for (var i = 0; i < allItems.Count; i += batchSize)
        {
            var batch = allItems.Skip(i).Take(batchSize).ToList();
            var transactItems = batch.Select(key => new TransactWriteItem
            {
                Delete = new TransactDeleteItem
                {
                    TableName = _configuration.TableName,
                    Key = key
                }
            }).ToList();

            var transactRequest = new TransactWriteRequest
            {
                TransactItems = transactItems
            };

            var transactResult = await _dynamoClient.TransactWriteItemsAsync(transactRequest, ct);
            if (transactResult.IsError)
                return transactResult.Errors;
        }

        return Deleted.Instance;
    }

    private static void SerializeMetadata(ConversationMetadata metadata, Dictionary<string, AttributeValue> item)
    {
        if (metadata.Title is not null)
            item[TitleAttribute] = metadata.Title;

        if (metadata.ModelId is not null)
            item[ModelIdAttribute] = metadata.ModelId;

        if (metadata.Tags.Count > 0)
            item[TagsAttribute] = new AttributeValue { SS = metadata.Tags };

        if (metadata.CustomData.Count > 0)
        {
            var customDataMap = new Dictionary<string, AttributeValue>();
            foreach (var kvp in metadata.CustomData)
            {
                customDataMap[kvp.Key] = kvp.Value;
            }
            item[CustomDataAttribute] = new AttributeValue { M = customDataMap };
        }
    }

    private ErrorOr<Entities.Conversation> DeserializeConversation(DynamoRecord record)
    {
        if (!record.TryGetString(IdAttribute, out var id))
            return Error.Validation(ConversationErrorCodes.MissingId, "Conversation record is missing Id attribute");

        if (!record.TryGetUnixTimestampSecondsAsOffset(CreatedAtAttribute, out var createdAt))
            return Error.Validation(ConversationErrorCodes.MissingCreatedAt, "Conversation record is missing CreatedAt attribute");

        if (!record.TryGetUnixTimestampSecondsAsOffset(UpdatedAtAttribute, out var updatedAt))
            return Error.Validation(ConversationErrorCodes.MissingUpdatedAt, "Conversation record is missing UpdatedAt attribute");

        if (!record.TryGetInt(MessageCountAttribute, out var messageCount))
            messageCount = 0;

        var metadata = DeserializeMetadata(record);

        TokenUsage? totalTokenUsage = null;
        if (record.TryGetInt(TotalInputTokensAttribute, out var totalInputTokens) &&
            record.TryGetInt(TotalOutputTokensAttribute, out var totalOutputTokens) &&
            record.TryGetInt(TotalTokensAttribute, out var totalTokens))
        {
            totalTokenUsage = new TokenUsage
            {
                InputTokens = totalInputTokens,
                OutputTokens = totalOutputTokens,
                TotalTokens = totalTokens
            };
        }

        return new Entities.Conversation
        {
            Id = id,
            Metadata = metadata,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            MessageCount = messageCount,
            TotalTokenUsage = totalTokenUsage
        };
    }

    private static ConversationMetadata? DeserializeMetadata(DynamoRecord record)
    {
        var hasMetadata = false;
        var metadata = new ConversationMetadata();

        if (record.TryGetNullableString(TitleAttribute, out var title) && title is not null)
        {
            metadata.Title = title;
            hasMetadata = true;
        }

        if (record.TryGetNullableString(ModelIdAttribute, out var modelId) && modelId is not null)
        {
            metadata.ModelId = modelId;
            hasMetadata = true;
        }

        if (record.TryGetStringSet(TagsAttribute, out var tags))
        {
            metadata.Tags = tags.ToList();
            hasMetadata = true;
        }

        if (record.TryGetStringDictionary(CustomDataAttribute, out var customData) && customData.Count > 0)
        {
            metadata.CustomData = customData;
            hasMetadata = true;
        }

        return hasMetadata ? metadata : null;
    }

    private ErrorOr<ConversationMessage> DeserializeMessage(DynamoRecord record)
    {
        if (!record.TryGetString(IdAttribute, out var id))
            return Error.Validation(ConversationErrorCodes.MessageMissingId, "Message record is missing Id attribute");

        if (!record.TryGetString(ConversationIdAttribute, out var conversationId))
            return Error.Validation(ConversationErrorCodes.MessageMissingConversationId, "Message record is missing ConversationId attribute");

        if (!record.TryGetInt(SequenceNumberAttribute, out var sequenceNumber))
            return Error.Validation(ConversationErrorCodes.MessageMissingSequenceNumber, "Message record is missing SequenceNumber attribute");

        if (!record.TryGetEnum<ConversationRole>(RoleAttribute, out var role))
            return Error.Validation(ConversationErrorCodes.MessageMissingRole, "Message record is missing or has invalid Role attribute");

        if (!record.TryGetUnixTimestampSecondsAsOffset(CreatedAtAttribute, out var createdAt))
            return Error.Validation(ConversationErrorCodes.MessageMissingCreatedAt, "Message record is missing CreatedAt attribute");

        if (!record.TryGetList(ContentAttribute, out var contentList) || contentList is null)
            return Error.Validation(ConversationErrorCodes.MessageMissingContent, "Message record is missing Content attribute");

        var content = new List<ContentBlock>();
        foreach (var item in contentList)
        {
            var block = ContentBlockSerializer.Deserialize(item);
            if (block.IsError)
                return block.Errors;
            content.Add(block.Value);
        }

        TokenUsage? tokenUsage = null;
        if (record.TryGetInt(InputTokensAttribute, out var inputTokens) &&
            record.TryGetInt(OutputTokensAttribute, out var outputTokens) &&
            record.TryGetInt(TokensAttribute, out var tokens))
        {
            tokenUsage = new TokenUsage
            {
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = tokens
            };
        }

        return new ConversationMessage
        {
            Id = id,
            ConversationId = conversationId,
            SequenceNumber = sequenceNumber,
            Role = role,
            Message = new Message { Role = role, Content = content },
            TokenUsage = tokenUsage,
            CreatedAt = createdAt
        };
    }

    private static string EncodePaginationToken(Dictionary<string, AttributeValue> lastEvaluatedKey)
    {
        var json = JsonSerializer.Serialize(lastEvaluatedKey, ConversationJsonContext.Default.DictionaryStringAttributeValue);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static ErrorOr<Dictionary<string, AttributeValue>> DecodePaginationToken(string token)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var result = JsonSerializer.Deserialize(json, ConversationJsonContext.Default.DictionaryStringAttributeValue);
            if (result is null)
                return Error.Validation(ConversationErrorCodes.PaginationTokenInvalid, "Invalid pagination token format");
            return result;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return Error.Validation(ConversationErrorCodes.PaginationTokenInvalid, "Invalid pagination token format");
        }
    }
}
