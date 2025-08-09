using Goa.Clients.Sns.Models;

namespace Goa.Clients.Sns.Operations.Publish;

/// <summary>
/// Builder for creating Publish requests with a fluent API.
/// </summary>
public sealed class PublishBuilder
{
    private string? _topicArn;
    private string? _targetArn;
    private string? _phoneNumber;
    private string? _message;
    private string? _subject;
    private string? _messageStructure;
    private Dictionary<string, SnsMessageAttributeValue>? _messageAttributes;
    private string? _messageDeduplicationId;
    private string? _messageGroupId;

    /// <summary>
    /// Sets the topic ARN to publish to.
    /// </summary>
    /// <param name="topicArn">The topic ARN.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder WithTopicArn(string topicArn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicArn);
        _topicArn = topicArn;
        return this;
    }

    /// <summary>
    /// Sets the target ARN (endpoint) to publish to.
    /// </summary>
    /// <param name="targetArn">The target ARN.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder WithTargetArn(string targetArn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetArn);
        _targetArn = targetArn;
        return this;
    }

    /// <summary>
    /// Sets the phone number for SMS delivery.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder WithPhoneNumber(string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        _phoneNumber = phoneNumber;
        return this;
    }

    /// <summary>
    /// Sets the message content.
    /// </summary>
    /// <param name="message">The message content.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder WithMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _message = message;
        return this;
    }

    /// <summary>
    /// Sets the subject for email endpoints.
    /// </summary>
    /// <param name="subject">The subject line.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder WithSubject(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        _subject = subject;
        return this;
    }

    /// <summary>
    /// Sets the message structure to JSON.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder AsJsonMessage()
    {
        _messageStructure = "json";
        return this;
    }

    /// <summary>
    /// Adds a message attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder AddMessageAttribute(string name, SnsMessageAttributeValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        _messageAttributes ??= new Dictionary<string, SnsMessageAttributeValue>();
        _messageAttributes[name] = value;
        return this;
    }

    /// <summary>
    /// Adds a string message attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The string value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder AddStringAttribute(string name, string value)
    {
        return AddMessageAttribute(name, new SnsMessageAttributeValue
        {
            DataType = "String",
            StringValue = value
        });
    }

    /// <summary>
    /// Sets the message deduplication ID for FIFO topics.
    /// </summary>
    /// <param name="messageDeduplicationId">The deduplication ID.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder WithMessageDeduplicationId(string messageDeduplicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageDeduplicationId);
        _messageDeduplicationId = messageDeduplicationId;
        return this;
    }

    /// <summary>
    /// Sets the message group ID for FIFO topics.
    /// </summary>
    /// <param name="messageGroupId">The message group ID.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PublishBuilder WithMessageGroupId(string messageGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageGroupId);
        _messageGroupId = messageGroupId;
        return this;
    }

    /// <summary>
    /// Builds the Publish request.
    /// </summary>
    /// <returns>A configured PublishRequest.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing or invalid.</exception>
    public PublishRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_message))
            throw new InvalidOperationException("Message is required.");

        var targetCount = 0;
        if (!string.IsNullOrWhiteSpace(_topicArn)) targetCount++;
        if (!string.IsNullOrWhiteSpace(_targetArn)) targetCount++;
        if (!string.IsNullOrWhiteSpace(_phoneNumber)) targetCount++;

        if (targetCount != 1)
            throw new InvalidOperationException("Exactly one of TopicArn, TargetArn, or PhoneNumber must be specified.");

        return new PublishRequest
        {
            TopicArn = _topicArn,
            TargetArn = _targetArn,
            PhoneNumber = _phoneNumber,
            Message = _message,
            Subject = _subject,
            MessageStructure = _messageStructure,
            MessageAttributes = _messageAttributes,
            MessageDeduplicationId = _messageDeduplicationId,
            MessageGroupId = _messageGroupId
        };
    }
}