using ErrorOr;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Goa.Clients.Sns.Operations.Publish;
using Goa.Clients.Sns.Serialization;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sns;

internal sealed class SnsServiceClient : AwsServiceClient<SnsServiceClientConfiguration>, ISnsClient
{
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
            var response = await SendAsync<PublishRequest, PublishResponse>(
                HttpMethod.Post,
                "/",
                request,
                SnsJsonContext.Default.PublishRequest,
                SnsJsonContext.Default.PublishResponse,
                "AmazonSNS.Publish",
                cancellationToken);

            return ConvertApiResponse(response);
        }
        catch (Exception ex)
        {
            var target = request.TopicArn ?? request.TargetArn ?? request.PhoneNumber;
            Logger.LogError(ex, "Failed to publish message to SNS target {Target}", target);
            return Error.Failure("SNS.Publish.Failed", $"Failed to publish message to SNS target {target}");
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
