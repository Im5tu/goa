using System.Text.Json;
using Goa.Clients.EventBridge.Operations.PutEvents;
using TUnit.Assertions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Core;

namespace Goa.Clients.EventBridge.Tests;

[ClassDataSource<EventBridgeTestFixture>(Shared = SharedType.PerAssembly)]
public class EventBridgeClientIntegrationTests
{
    private readonly EventBridgeTestFixture _fixture;

    public EventBridgeClientIntegrationTests(EventBridgeTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task PutEventsAsync_WithSingleEvent_ShouldSucceed()
    {
        // Arrange
        var request = new PutEventsBuilder()
            .AddEvent(
                source: "test.application",
                detailType: "Test Event",
                detail: JsonSerializer.Serialize(new { message = "Hello World", timestamp = DateTime.UtcNow }),
                eventBusName: _fixture.TestEventBusName)
            .Build();

        // Act
        var result = await _fixture.EventBridgeClient.PutEventsAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.FailedEntryCount).IsEqualTo(0);
        await Assert.That(result.Value.Entries).HasCount().EqualTo(1);
        await Assert.That(result.Value.Entries[0].EventId).IsNotNull();
        await Assert.That(result.Value.Entries[0].ErrorCode).IsNull();
    }

    [Test]
    public async Task PutEventsAsync_WithMultipleEvents_ShouldSucceed()
    {
        // Arrange
        var request = new PutEventsBuilder()
            .AddEvent(
                source: "test.application",
                detailType: "Test Event 1",
                detail: JsonSerializer.Serialize(new { message = "Event 1" }),
                eventBusName: _fixture.TestEventBusName)
            .AddEvent(
                source: "test.application",
                detailType: "Test Event 2",
                detail: JsonSerializer.Serialize(new { message = "Event 2" }),
                eventBusName: _fixture.TestEventBusName)
            .Build();

        // Act
        var result = await _fixture.EventBridgeClient.PutEventsAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.FailedEntryCount).IsEqualTo(0);
        await Assert.That(result.Value.Entries).HasCount().EqualTo(2);
        await Assert.That(result.Value.Entries[0].EventId).IsNotNull();
        await Assert.That(result.Value.Entries[1].EventId).IsNotNull();
    }

    [Test]
    public async Task PutEventsAsync_WithEmptyEntries_ShouldFail()
    {
        // Arrange
        var request = new PutEventsRequest { Entries = [] };

        // Act
        var result = await _fixture.EventBridgeClient.PutEventsAsync(request);

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("PutEventsRequest.Entries");
    }

    [Test]
    public async Task PutEventsAsync_WithTooManyEntries_ShouldFail()
    {
        // Arrange
        var builder = new PutEventsBuilder();
        for (int i = 0; i < 10; i++)
        {
            builder.AddEvent(
                source: "test.application",
                detailType: $"Test Event {i}",
                detail: JsonSerializer.Serialize(new { message = $"Event {i}" }),
                eventBusName: _fixture.TestEventBusName);
        }

        // Act & Assert
        await Assert.That(() => builder.AddEvent(
                source: "test.application",
                detailType: "Test Event Fail",
                detail: JsonSerializer.Serialize(new { message = "Test Event Fail" }),
                eventBusName: _fixture.TestEventBusName))
            .Throws<InvalidOperationException>()
            .WithMessage("Maximum of 10 entries allowed per PutEvents request.");
    }

    [Test]
    public async Task PutEventsBuilder_WithValidEvents_ShouldBuildCorrectly()
    {
        // Arrange & Act
        var eventTime = DateTime.UtcNow;
        var resources = new List<string> { "arn:aws:s3:::test-bucket" };
        var request = new PutEventsBuilder()
            .AddEvent(
                source: "test.application",
                detailType: "Test Event",
                detail: JsonSerializer.Serialize(new { message = "Test" }),
                eventBusName: _fixture.TestEventBusName,
                resources: resources,
                time: eventTime)
            .WithEndpointId("test-endpoint")
            .Build();

        // Assert
        await Assert.That(request.Entries).HasCount().EqualTo(1);
        await Assert.That(request.Entries[0].Source).IsEqualTo("test.application");
        await Assert.That(request.Entries[0].DetailType).IsEqualTo("Test Event");
        await Assert.That(request.Entries[0].EventBusName).IsEqualTo(_fixture.TestEventBusName);
        await Assert.That(request.Entries[0].Resources).IsEqualTo(resources);
        await Assert.That(request.Entries[0].Time).IsEqualTo(eventTime);
        await Assert.That(request.EndpointId).IsEqualTo("test-endpoint");
    }

    [Test]
    public async Task PutEventsBuilder_WithNoEvents_ShouldThrowException()
    {
        // Arrange
        var builder = new PutEventsBuilder();

        // Act & Assert
        await Assert.That(() => builder.Build())
            .Throws<InvalidOperationException>()
            .WithMessage("At least one event entry is required.");
    }
}
