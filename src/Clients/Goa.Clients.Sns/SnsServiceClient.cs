using System.Net.Http.Headers;
using System.Text;
using ErrorOr;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Goa.Clients.Sns.Operations.Publish;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sns;

internal sealed class SnsServiceClient : AwsServiceClient<SnsServiceClientConfiguration>, ISnsClient
{
    private static readonly ApiError DeserializationError = new("Failed to deserialize response", "DeserializationError");

    public SnsServiceClient(
        IHttpClientFactory httpClientFactory,
        SnsServiceClientConfiguration configuration,
        ILogger<SnsServiceClient> logger)
        : base(httpClientFactory, logger, configuration)
    {
    }

    public async Task<ErrorOr<PublishResponse>> PublishAsync(PublishRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
            return Error.Validation("PublishRequest.Message", "Message is required.");

        var targetCount = 0;
        if (!string.IsNullOrWhiteSpace(request.TopicArn)) targetCount++;
        if (!string.IsNullOrWhiteSpace(request.TargetArn)) targetCount++;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) targetCount++;

        if (targetCount != 1)
            return Error.Validation("PublishRequest.Target", "Exactly one of TopicArn, TargetArn, or PhoneNumber must be specified.");

        try
        {
            // Serialize request to query parameters and convert to bytes
            var queryParameters = SerializeToQueryParameters(request);
            var content = Encoding.UTF8.GetBytes(queryParameters);

            // Create request message
            var requestMessage = CreateRequestMessage(
                HttpMethod.Post,
                "/",
                content,
                new MediaTypeHeaderValue("application/x-www-form-urlencoded"));

            // Send request and get raw HTTP response
            var httpResponse = await SendAsync(requestMessage, "Publish", cancellationToken);

            // Process the response
            var apiResponse = await ProcessSnsResponseAsync<PublishResponse>(httpResponse);

            return ConvertApiResponse(apiResponse);
        }
        catch (Exception ex)
        {
            var target = request.TopicArn ?? request.TargetArn ?? request.PhoneNumber;
            Logger.LogError(ex, "Failed to publish message to SNS target {Target}", target);
            return Error.Failure("SNS.Publish.Failed", $"Failed to publish message to SNS target {target}");
        }
    }

    /// <summary>
    /// Serializes a PublishRequest to AWS SNS query parameters format.
    /// </summary>
    private static string SerializeToQueryParameters(PublishRequest request)
    {
        var parameters = new List<string>
        {
            "Action=Publish",
            "Version=2010-03-31"
        };

        if (!string.IsNullOrWhiteSpace(request.TopicArn))
            parameters.Add($"TopicArn={Uri.EscapeDataString(request.TopicArn)}");

        if (!string.IsNullOrWhiteSpace(request.TargetArn))
            parameters.Add($"TargetArn={Uri.EscapeDataString(request.TargetArn)}");

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            parameters.Add($"PhoneNumber={Uri.EscapeDataString(request.PhoneNumber)}");

        if (!string.IsNullOrWhiteSpace(request.Message))
            parameters.Add($"Message={Uri.EscapeDataString(request.Message)}");

        if (!string.IsNullOrWhiteSpace(request.Subject))
            parameters.Add($"Subject={Uri.EscapeDataString(request.Subject)}");

        if (!string.IsNullOrWhiteSpace(request.MessageStructure))
            parameters.Add($"MessageStructure={Uri.EscapeDataString(request.MessageStructure)}");

        if (!string.IsNullOrWhiteSpace(request.MessageDeduplicationId))
            parameters.Add($"MessageDeduplicationId={Uri.EscapeDataString(request.MessageDeduplicationId)}");

        if (!string.IsNullOrWhiteSpace(request.MessageGroupId))
            parameters.Add($"MessageGroupId={Uri.EscapeDataString(request.MessageGroupId)}");

        // Handle MessageAttributes if present
        if (request.MessageAttributes != null && request.MessageAttributes.Count > 0)
        {
            var index = 1;
            foreach (var kvp in request.MessageAttributes)
            {
                var prefix = $"MessageAttributes.entry.{index}";
                parameters.Add($"{prefix}.Name={Uri.EscapeDataString(kvp.Key)}");
                parameters.Add($"{prefix}.Value.DataType={Uri.EscapeDataString(kvp.Value.DataType)}");

                if (!string.IsNullOrWhiteSpace(kvp.Value.StringValue))
                    parameters.Add($"{prefix}.Value.StringValue={Uri.EscapeDataString(kvp.Value.StringValue)}");

                if (!string.IsNullOrWhiteSpace(kvp.Value.BinaryValue))
                    parameters.Add($"{prefix}.Value.BinaryValue={Uri.EscapeDataString(kvp.Value.BinaryValue)}");

                index++;
            }
        }

        return string.Join("&", parameters);
    }

    /// <summary>
    /// Processes an SNS HTTP response and converts it to an API response.
    /// </summary>
    private async Task<ApiResponse<TResponse>> ProcessSnsResponseAsync<TResponse>(HttpResponseMessage response)
        where TResponse : class, IDeserializeFromXml, new()
    {
        var headers = response.Headers.ToDictionary(h => h.Key, h => h.Value);

        if (!response.IsSuccessStatusCode)
        {
            var errorPayload = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(errorPayload))
            {
                Logger.RequestFailed("No payload present");
                return new ApiResponse<TResponse>(new ApiError("Request not successful.") { StatusCode = response.StatusCode });
            }

            var error = DeserializeSnsError(errorPayload);
            if (error is not null)
            {
                error = error with
                {
                    Payload = errorPayload,
                    StatusCode = response.StatusCode
                };
            }
            else
            {
                error = new ApiError("Failed to send request and unable to deserialize payload") { StatusCode = response.StatusCode };
            }

            // Apply AWS-specific error header processing
            error = ProcessAwsErrorHeaders(response, error);

            Logger.RequestFailed($"Type: {error?.Type ?? "Unknown"}, Message: {error?.Message ?? "Unknown"}, Code: {error?.Code ?? "Unknown"}");
            return new ApiResponse<TResponse>(error ?? DeserializationError);
        }

        var contentPayload = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(contentPayload))
        {
            return new ApiResponse<TResponse>(default(TResponse), headers);
        }

        var result = new TResponse();
        result.DeserializeFromXml(contentPayload);
        return new ApiResponse<TResponse>(result, headers);
    }

    /// <summary>
    /// Applies AWS-specific error header processing to the error object.
    /// </summary>
    private ApiError ProcessAwsErrorHeaders(HttpResponseMessage response, ApiError error)
    {
        if (response.Headers.TryGetValues(XAmznErrorMessage, out var messages))
        {
            error = error with { Message = string.Join(", ", messages) };
        }

        if (string.IsNullOrWhiteSpace(error.Type) && response.Headers.TryGetValues(XAmzErrorType, out var types))
        {
            error = error with { Type = string.Join(", ", types) };
        }

        var infoSeparator = error.Type?.LastIndexOf(':') ?? -1;
        if (infoSeparator > 0)
        {
            error = error with { Type = error.Type![..infoSeparator] };
        }

        var typeSeparator = error.Type?.LastIndexOf('#') ?? -1;
        if (typeSeparator > 0)
        {
            error = error with { Type = error.Type![(typeSeparator + 1)..] };
        }

        return error;
    }

    /// <summary>
    /// Deserializes SNS XML error response content to an ApiError.
    /// </summary>
    private ApiError? DeserializeSnsError(string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var xmlError = new XmlApiError();
            xmlError.DeserializeFromXml(content);
            return xmlError.ToApiError();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to deserialize SNS XML error response: {Content}", content);
            return null;
        }
    }

    private static ErrorOr<T> ConvertApiResponse<T>(ApiResponse<T> response)
    {
        if (response.IsSuccess)
        {
            return response.Value!;
        }

        var error = response.Error!;
        var snsError = Error.Failure(
            code: $"Goa.SNS.{error.Type ?? error.Code ?? "Unknown"}",
            description: error.Message ?? "An error occurred while processing the request.");

        return snsError;
    }
}
