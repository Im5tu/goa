using Goa.Functions.ApiGateway.Core.Payloads.V1;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using System.Text;
using ProxyPayloadV1SerializationContext = Goa.Functions.ApiGateway.Core.Payloads.V1.ProxyPayloadV1SerializationContext;

namespace Goa.Functions.ApiGateway.AspNetCore.Features.V1;

internal sealed class LambdaHTTPV1Server : LambdaServer<ProxyPayloadV1Request, ProxyPayloadV1Response>
{
    public LambdaHTTPV1Server(ILambdaRuntimeClient?  lambdaRuntimeClient = null)
        : base(new LambdaBootstrapper<ProxyPayloadV1Request, ProxyPayloadV1Response>(ProxyPayloadV1SerializationContext.Default, lambdaRuntimeClient: lambdaRuntimeClient))
    {
    }

    protected override IFeatureCollection GetPerRequestFeatureCollection(ProxyPayloadV1Request request, InvocationRequest invocationRequest, CancellationToken cancellationToken)
    {
        var features = new FeatureCollection();

        var requestFeature = new LambdaHttpRequestFeatureV1(request);
        features.Set<IHttpRequestFeature>(requestFeature);
        features.Set<IHttpRequestBodyDetectionFeature>(new LambdaHttpRequestBodyDetectionFeature(requestFeature));
        features.Set<IHttpRequestIdentifierFeature>(new LambdaHttpRequestIdentifierFeature { TraceIdentifier = request.RequestContext?.RequestId ?? invocationRequest.RequestId });
        features.Set<IHttpRequestLifetimeFeature>(new LambdaHttpRequestLifetimeFeature(cancellationToken));

        features.Set<IHttpAuthenticationFeature>(new LambdaHttpAuthenticationFeatureV1(request));

        features.Set<IHttpResponseFeature>(new LambdaHttpResponseFeature());
        features.Set<IHttpResponseBodyFeature>(new LambdaHttpResponseBodyFeatureV1());

        return features;
    }

    protected override ProxyPayloadV1Response ProcessResponse(IFeatureCollection features)
    {
        if (features == null)
            throw new ArgumentNullException(nameof(features));

        var responseFeature = features.Get<IHttpResponseFeature>();
        var responseBodyFeature = features.Get<IHttpResponseBodyFeature>();

        if (responseFeature == null || responseBodyFeature == null)
            throw new InvalidOperationException("Required response features are missing from the feature collection.");

        var responseBody = ReadResponseBody(responseBodyFeature.Stream);

        // Convert headers to single-value and multi-value dictionaries
        var (headers, multiValueHeaders) = ConvertHeaders(responseFeature.Headers);

        return new ProxyPayloadV1Response
        {
            StatusCode = responseFeature.StatusCode,
            Headers = headers,
            MultiValueHeaders = multiValueHeaders,
            Body = responseBody.IsBase64Encoded ? Convert.ToBase64String(responseBody.Content) : Encoding.UTF8.GetString(responseBody.Content),
            IsBase64Encoded = responseBody.IsBase64Encoded
        };
    }

    private static (IDictionary<string, string>, IDictionary<string, IList<string>>) ConvertHeaders(IHeaderDictionary headerDictionary)
    {
        var singleValueHeaders = new Dictionary<string, string>();
        var multiValueHeaders = new Dictionary<string, IList<string>>();

        foreach (var header in headerDictionary)
        {
            var values = header.Value;
            if (values.Count == 1)
            {
                singleValueHeaders[header.Key] = values[0] ?? string.Empty;
            }
            else if (values.Count > 1)
            {
                multiValueHeaders[header.Key] = values.ToList()!;
            }
        }

        return (singleValueHeaders, multiValueHeaders);
    }
}
