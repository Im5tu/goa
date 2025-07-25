using Goa.Functions.ApiGateway.Core.Payloads.V2;
using Goa.Functions.ApiGateway.Payloads.V2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Text;

namespace Goa.Functions.ApiGateway.AspNetCore.Features.V2;

internal sealed class LambdaHttpRequestFeatureV2 : IHttpRequestFeature
{
    public string Protocol { get; set; } = "HTTP/1.1";
    public string Scheme { get; set; } = "https";
    public string Method { get; set; } = "GET";
    public string PathBase { get; set; } = string.Empty;
    public string Path { get; set; } = "/";
    public string QueryString { get; set; } = string.Empty;
    public string RawTarget { get; set; } = string.Empty;
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
    public Stream Body { get; set; } = Stream.Null;

    public LambdaHttpRequestFeatureV2(ProxyPayloadV2Request request)
    {
        Protocol = request.RequestContext?.Http?.Protocol ?? "HTTP/1.1";
        Method = request.RequestContext?.Http?.Method ?? "GET";
        Path = request.RawPath ?? "/";
        QueryString = string.IsNullOrEmpty(request.RawQueryString) ? string.Empty : $"?{request.RawQueryString}";
        Headers = BuildHeaderDictionary(request.Headers);
        Body = GetRequestBodyStream(request.Body, request.IsBase64Encoded);
    }

    private static IHeaderDictionary BuildHeaderDictionary(IDictionary<string, string>? headers)
    {
        var headerDictionary = new HeaderDictionary();
        if (headers != null)
        {
            foreach (var header in headers)
            {
                headerDictionary[header.Key] = header.Value;
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