using Goa.Clients.Sns.Models;
using Goa.Clients.Sns.Operations.Publish;

namespace Goa.Clients.Sns.Tests;

[ClassDataSource<SnsTestFixture>(Shared = SharedType.PerAssembly)]
public class SnsClientIntegrationTests
{
    private readonly SnsTestFixture _fixture;

    public SnsClientIntegrationTests(SnsTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task PublishAsync_WithValidTopicMessage_ShouldSucceed()
    {
        // Arrange
        var request = new PublishBuilder()
            .WithTopicArn(_fixture.TestTopicArn)
            .WithMessage("Hello, SNS!")
            .WithSubject("Test Message")
            .Build();

        // Act
        var result = await _fixture.SnsClient.PublishAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.MessageId).IsNotNull();
    }

    [Test]
    public async Task PublishAsync_WithMessageAttributes_ShouldSucceed()
    {
        // Arrange
        var request = new PublishBuilder()
            .WithTopicArn(_fixture.TestTopicArn)
            .WithMessage("Message with attributes")
            .AddStringAttribute("TestAttribute", "TestValue")
            .AddMessageAttribute("NumberAttribute", new SnsMessageAttributeValue
            {
                DataType = "Number",
                StringValue = "42"
            })
            .Build();

        // Act
        var result = await _fixture.SnsClient.PublishAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.MessageId).IsNotNull();
    }

    [Test]
    public async Task PublishAsync_WithEmptyMessage_ShouldFail()
    {
        // Arrange
        var request = new PublishRequest
        {
            TopicArn = _fixture.TestTopicArn,
            Message = ""
        };

        // Act
        var result = await _fixture.SnsClient.PublishAsync(request);

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("PublishRequest.Message");
    }

    [Test]
    public async Task PublishBuilder_WithValidParameters_ShouldBuildCorrectly()
    {
        // Arrange & Act
        var request = new PublishBuilder()
            .WithTopicArn(_fixture.TestTopicArn)
            .WithMessage("Test message")
            .WithSubject("Test subject")
            .AsJsonMessage()
            .AddStringAttribute("attr1", "value1")
            .Build();

        // Assert
        await Assert.That(request.TopicArn).IsEqualTo(_fixture.TestTopicArn);
        await Assert.That(request.Message).IsEqualTo("Test message");
        await Assert.That(request.Subject).IsEqualTo("Test subject");
        await Assert.That(request.MessageStructure).IsEqualTo("json");
        await Assert.That(request.MessageAttributes).HasCount().EqualTo(1);
        await Assert.That(request.MessageAttributes!["attr1"].StringValue).IsEqualTo("value1");
    }

    [Test]
    public async Task PublishBuilder_WithoutMessage_ShouldThrowException()
    {
        // Arrange
        var builder = new PublishBuilder()
            .WithTopicArn(_fixture.TestTopicArn);

        // Act & Assert
        await Assert.That(() => builder.Build())
            .Throws<InvalidOperationException>()
            .WithMessage("Message is required.");
    }

    [Test]
    public async Task PublishBuilder_WithMultipleTargets_ShouldThrowException()
    {
        // Arrange
        var builder = new PublishBuilder()
            .WithTopicArn(_fixture.TestTopicArn)
            .WithPhoneNumber("+1234567890")
            .WithMessage("Test message");

        // Act & Assert
        await Assert.That(() => builder.Build())
            .Throws<InvalidOperationException>()
            .WithMessage("Exactly one of TopicArn, TargetArn, or PhoneNumber must be specified.");
    }
}
