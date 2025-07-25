using System.Text;
using System.Text.Json.Serialization;

namespace Goa.Clients.Lambda.Operations.Invoke;

/// <summary>
/// Response from the synchronous Invoke operation.
/// </summary>
public sealed class InvokeResponse
{
    /// <summary>
    /// The HTTP status code for the invocation.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The function error type if the function returned an error.
    /// </summary>
    public string? FunctionError { get; set; }

    /// <summary>
    /// The last 4KB of the execution log (base64 encoded) if LogType was Tail.
    /// </summary>
    public string? LogResult { get; set; }

    /// <summary>
    /// The version of the function that was invoked.
    /// </summary>
    public string? ExecutedVersion { get; set; }

    /// <summary>
    /// The response payload from the function.
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Whether the function execution completed successfully.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300 && string.IsNullOrEmpty(FunctionError);

    /// <summary>
    /// Creates an InvokeResponse from HTTP response data.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="payload">The response payload.</param>
    /// <param name="headers">The HTTP response headers.</param>
    /// <returns>A new InvokeResponse instance.</returns>
    public static InvokeResponse FromHttpResponse(int statusCode, string? payload, IReadOnlyDictionary<string, IEnumerable<string>>? headers)
    {
        var response = new InvokeResponse
        {
            StatusCode = statusCode,
            Payload = payload
        };

        if (headers != null)
        {
            if (headers.TryGetValue("X-Amz-Function-Error", out var functionError))
                response.FunctionError = functionError.FirstOrDefault();

            if (headers.TryGetValue("X-Amz-Log-Result", out var logResult))
            {
                response.LogResult = logResult.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(response.LogResult))
                {
                    try
                    {
                        response.LogResult = Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
                    }
                    catch
                    {
                        // meh - we did our best to convert
                    }
                }
            }

            if (headers.TryGetValue("X-Amz-Executed-Version", out var executedVersion))
                response.ExecutedVersion = executedVersion.FirstOrDefault();
        }

        return response;
    }
}
