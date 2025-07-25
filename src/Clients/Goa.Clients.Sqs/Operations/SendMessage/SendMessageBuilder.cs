using Goa.Clients.Sqs.Models;

namespace Goa.Clients.Sqs.Operations.SendMessage;

/// <summary>
/// Builder for creating SendMessage requests with a fluent API.
/// </summary>
public sealed class SendMessageBuilder
{
    private string? _queueUrl;
    private string? _messageBody;
    private int? _delaySeconds;
    private Dictionary<string, MessageAttributeValue>? _messageAttributes;
    private Dictionary<string, MessageAttributeValue>? _messageSystemAttributes;
    private string? _messageDeduplicationId;
    private string? _messageGroupId;

    /// <summary>
    /// Sets the queue URL.
    /// </summary>
    /// <param name="queueUrl">The queue URL.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SendMessageBuilder WithQueueUrl(string queueUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueUrl);
        _queueUrl = queueUrl;
        return this;
    }

    /// <summary>
    /// Sets the message body.
    /// </summary>
    /// <param name="messageBody">The message body.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SendMessageBuilder WithMessageBody(string messageBody)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageBody);
        _messageBody = messageBody;
        return this;
    }

    /// <summary>
    /// Sets the delay seconds.
    /// </summary>
    /// <param name="delaySeconds">The delay in seconds.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SendMessageBuilder WithDelaySeconds(int delaySeconds)
    {
        if (delaySeconds < 0 || delaySeconds > 900)
            throw new ArgumentOutOfRangeException(nameof(delaySeconds), "DelaySeconds must be between 0 and 900.");
        
        _delaySeconds = delaySeconds;
        return this;
    }

    /// <summary>
    /// Adds a message attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SendMessageBuilder AddMessageAttribute(string name, MessageAttributeValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        _messageAttributes ??= new Dictionary<string, MessageAttributeValue>();
        _messageAttributes[name] = value;
        return this;
    }

    /// <summary>
    /// Adds a string message attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The string value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SendMessageBuilder AddStringAttribute(string name, string value)
    {
        return AddMessageAttribute(name, new MessageAttributeValue
        {
            DataType = "String",
            StringValue = value
        });
    }

    /// <summary>
    /// Adds a message system attribute.
    /// </summary>
    /// <param name="name">The system attribute name.</param>
    /// <param name="value">The system attribute value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SendMessageBuilder AddMessageSystemAttribute(string name, MessageAttributeValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        _messageSystemAttributes ??= new Dictionary<string, MessageAttributeValue>();
        _messageSystemAttributes[name] = value;
        return this;
    }

    /// <summary>
    /// Adds a string message system attribute.
    /// </summary>
    /// <param name="name">The system attribute name.</param>
    /// <param name="value">The string value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SendMessageBuilder AddStringSystemAttribute(string name, string value)
    {
        return AddMessageSystemAttribute(name, new MessageAttributeValue
        {
            DataType = "String",
            StringValue = value
        });
    }

    /// <summary>
    /// Sets the message deduplication ID for FIFO queues.
    /// </summary>
    /// <param name="messageDeduplicationId">The deduplication ID.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SendMessageBuilder WithMessageDeduplicationId(string messageDeduplicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageDeduplicationId);
        _messageDeduplicationId = messageDeduplicationId;
        return this;
    }

    /// <summary>
    /// Sets the message group ID for FIFO queues.
    /// </summary>
    /// <param name="messageGroupId">The message group ID.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public SendMessageBuilder WithMessageGroupId(string messageGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageGroupId);
        _messageGroupId = messageGroupId;
        return this;
    }

    /// <summary>
    /// Builds the SendMessage request.
    /// </summary>
    /// <returns>A configured SendMessageRequest.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
    public SendMessageRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_queueUrl))
            throw new InvalidOperationException("Queue URL is required.");

        if (string.IsNullOrWhiteSpace(_messageBody))
            throw new InvalidOperationException("Message body is required.");

        return new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = _messageBody,
            DelaySeconds = _delaySeconds,
            MessageAttributes = _messageAttributes,
            MessageSystemAttributes = _messageSystemAttributes,
            MessageDeduplicationId = _messageDeduplicationId,
            MessageGroupId = _messageGroupId
        };
    }
}