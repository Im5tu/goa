using Goa.Clients.EventBridge.Models;

namespace Goa.Clients.EventBridge.Operations.PutEvents;

/// <summary>
/// Builder for creating PutEvents requests with a fluent API.
/// </summary>
public sealed class PutEventsBuilder
{
    private readonly List<EventEntry> _entries = [];
    private string? _endpointId;

    /// <summary>
    /// Adds an event entry to the request.
    /// </summary>
    /// <param name="source">The source of the event.</param>
    /// <param name="detailType">The detail type of the event.</param>
    /// <param name="detail">The event detail as a JSON string.</param>
    /// <param name="eventBusName">The event bus name (optional).</param>
    /// <param name="resources">Resources associated with the event (optional).</param>
    /// <param name="time">Event timestamp (optional, defaults to current time).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PutEventsBuilder AddEvent(
        string source,
        string detailType,
        string detail,
        string? eventBusName = null,
        List<string>? resources = null,
        DateTime? time = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(detailType);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        if (_entries.Count >= 10)
            throw new InvalidOperationException("Maximum of 10 entries allowed per PutEvents request.");

        _entries.Add(new EventEntry
        {
            Source = source,
            DetailType = detailType,
            Detail = detail,
            EventBusName = eventBusName,
            Resources = resources,
            Time = time
        });

        return this;
    }

    /// <summary>
    /// Sets the endpoint ID for multi-region endpoints.
    /// </summary>
    /// <param name="endpointId">The endpoint ID.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PutEventsBuilder WithEndpointId(string endpointId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        _endpointId = endpointId;
        return this;
    }

    /// <summary>
    /// Builds the PutEvents request.
    /// </summary>
    /// <returns>A configured PutEventsRequest.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no entries have been added.</exception>
    public PutEventsRequest Build()
    {
        if (_entries.Count == 0)
            throw new InvalidOperationException("At least one event entry is required.");

        return new PutEventsRequest
        {
            Entries = [.. _entries],
            EndpointId = _endpointId
        };
    }
}