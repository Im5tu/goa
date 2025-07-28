using Goa.Functions.ApiGateway.Payloads.V2;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using System.Text;

namespace Goa.Functions.ApiGateway.Features.V2;

internal sealed class LambdaHTTPV2Server : LambdaServer<ProxyPayloadV2Request, ProxyPayloadV2Response>
{
    public LambdaHTTPV2Server(ILambdaRuntimeClient?  lambdaRuntimeClient = null)
        : base(new LambdaBootstrapper<ProxyPayloadV2Request, ProxyPayloadV2Response>(Payloads.V2.ProxyPayloadV2SerializationContext.Default, lambdaRuntimeClient: lambdaRuntimeClient))
    {
    }

    protected override IFeatureCollection GetPerRequestFeatureCollection(ProxyPayloadV2Request request, InvocationRequest invocationRequest, CancellationToken cancellationToken)
    {
        var features = new FeatureCollection();

        var requestFeature = new LambdaHttpRequestFeatureV2(request);
        features.Set<IHttpRequestFeature>(requestFeature);
        features.Set<IHttpRequestBodyDetectionFeature>(new LambdaHttpRequestBodyDetectionFeature(requestFeature));
        features.Set<IHttpRequestIdentifierFeature>(new LambdaHttpRequestIdentifierFeature { TraceIdentifier = request.RequestContext?.RequestId ?? invocationRequest.RequestId });
        features.Set<IHttpRequestLifetimeFeature>(new LambdaHttpRequestLifetimeFeature(cancellationToken));

        features.Set<IHttpAuthenticationFeature>(new LambdaHttpAuthenticationFeatureV2(request));

        features.Set<IHttpResponseFeature>(new LambdaHttpResponseFeature());
        features.Set<IHttpResponseBodyFeature>(new LambdaHttpResponseBodyFeatureV2());

        return features;
    }

    protected override ProxyPayloadV2Response ProcessResponse(IFeatureCollection features)
    {
        if (features == null)
            throw new ArgumentNullException(nameof(features));

        var responseFeature = features.Get<IHttpResponseFeature>();
        var responseBodyFeature = features.Get<IHttpResponseBodyFeature>();

        if (responseFeature == null || responseBodyFeature == null)
            throw new InvalidOperationException("Required response features are missing from the feature collection.");

        var responseBody = ReadResponseBody(responseBodyFeature.Stream);

        // Convert headers to a single-value dictionary and extract cookies
        var (headers, cookies) = ConvertHeadersAndCookies(responseFeature.Headers);

        return new ProxyPayloadV2Response
        {
            StatusCode = responseFeature.StatusCode,
            Headers = headers,
            Cookies = cookies,
            Body = responseBody.IsBase64Encoded
                ? Convert.ToBase64String(responseBody.Content)
                : Encoding.UTF8.GetString(responseBody.Content),
            IsBase64Encoded = responseBody.IsBase64Encoded
        };
    }

    private (IReadOnlyDictionary<string, string>, IEnumerable<string>) ConvertHeadersAndCookies(IHeaderDictionary headerDictionary)
    {
        var headers = new Dictionary<string, string>();
        var cookies = new List<string>();

        foreach (var header in headerDictionary)
        {
            if (header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            {
                cookies.AddRange(header.Value);
            }
            else
            {
                headers[header.Key] = header.Value.ToString();
            }
        }

        return (headers, cookies);
    }
}
