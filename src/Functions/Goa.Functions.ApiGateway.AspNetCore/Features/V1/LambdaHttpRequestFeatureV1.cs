using Goa.Functions.ApiGateway.Payloads.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Text;

namespace Goa.Functions.ApiGateway.AspNetCore.Features.V1;

#pragma warning disable CS1591
internal sealed class LambdaHttpRequestFeatureV1 : IHttpRequestFeature
{
    public string Protocol { get; set; } = "HTTP/1.1";
    public string Scheme { get; set; } = "https";
    public string Method { get; set; }
    public string PathBase { get; set; } = string.Empty;
    public string Path { get; set; }
    public string QueryString { get; set; }
    public string RawTarget { get; set; } = string.Empty;
    public IHeaderDictionary Headers { get; set; }
    public Stream Body { get; set; }

    public LambdaHttpRequestFeatureV1(ProxyPayloadV1Request request)
    {
        Method = request.HttpMethod ?? "GET";
        Path = request.Path ?? "/";
        QueryString = BuildQueryString(request.QueryStringParameters);
        Headers = BuildHeaderDictionary(request.Headers, request.MultiValueHeaders);
        Body = GetRequestBodyStream(request.Body, request.IsBase64Encoded);
    }

    private static string BuildQueryString(IDictionary<string, string>? queryStringParameters)
    {
        if (queryStringParameters == null || queryStringParameters.Count == 0) return string.Empty;
        return "?" + string.Join("&", queryStringParameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
    }

    private static IHeaderDictionary BuildHeaderDictionary(IDictionary<string, string>? headers, IDictionary<string, IList<string>>? multiValueHeaders)
    {
        var headerDictionary = new HeaderDictionary();

        if (headers != null)
        {
            foreach (var header in headers)
            {
                headerDictionary[header.Key] = header.Value;
            }
        }

        if (multiValueHeaders != null)
        {
            foreach (var header in multiValueHeaders)
            {
                headerDictionary[header.Key] = string.Join(",", header.Value);
            }
        }

        return headerDictionary;
    }

    private static Stream GetRequestBodyStream(string? body, bool isBase64Encoded)
    {
        if (string.IsNullOrEmpty(body)) return Stream.Null;
        var content = isBase64Encoded
            ? Convert.FromBase64String(body)
            : Encoding.UTF8.GetBytes(body);
        return new MemoryStream(content);
    }
}
