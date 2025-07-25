using System.Text.Json;
using ErrorOr;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Goa.Clients.EventBridge.Operations.PutEvents;
using Goa.Clients.EventBridge.Serialization;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.EventBridge;

internal sealed class EventBridgeServiceClient : JsonAwsServiceClient<EventBridgeServiceClientConfiguration>, IEventBridgeClient
{
    public EventBridgeServiceClient(
        IHttpClientFactory httpClientFactory,
        EventBridgeServiceClientConfiguration configuration,
        ILogger<EventBridgeServiceClient> logger)
        : base(httpClientFactory, logger, configuration, EventBridgeJsonContext.Default)
    {
    }

    public async Task<ErrorOr<PutEventsResponse>> PutEventsAsync(PutEventsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Entries.Count == 0)
            return Error.Validation("PutEventsRequest.Entries", "At least one event entry is required.");

        if (request.Entries.Count > 10)
            return Error.Validation("PutEventsRequest.Entries", "Maximum of 10 entries allowed per request.");

        try
        {
            var response = await SendAsync<PutEventsRequest, PutEventsResponse>(
                HttpMethod.Post,
            "/",
                request,
                "PutEvents",
                cancellationToken);

            return ConvertApiResponse(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to put events to EventBridge");
            return Error.Failure("EventBridge.PutEvents.Failed", "Failed to put events to EventBridge");
        }
    }

    private static ErrorOr<T> ConvertApiResponse<T>(ApiResponse<T> response)
    {
        if (response.IsSuccess)
        {
            return response.Value!;
        }

        var error = response.Error!;
        var dynamoError = Error.Failure(
            code: $"Goa.EventBridge.{error.Type ?? error.Code ?? "Unknown"}",
            description: error.Message ?? "An error occurred while processing the request.");

        return dynamoError;
    }
}
