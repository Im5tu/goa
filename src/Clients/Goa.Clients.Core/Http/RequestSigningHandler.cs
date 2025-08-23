using Goa.Clients.Core.Credentials;
using System.Net.Http.Headers;

namespace Goa.Clients.Core.Http;

internal sealed class RequestSigningHandler : DelegatingHandler
{
    private readonly ICredentialProviderChain _credentialProvider;
    private readonly RequestSigner _requestSigner;

    public RequestSigningHandler(ICredentialProviderChain credentialProvider)
    {
        _credentialProvider = credentialProvider;
        _requestSigner = new RequestSigner(credentialProvider);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Use RequestSigner to compute the authorization header
        var authHeaderValue = await _requestSigner.GetAuthorizationHeaderAsync(request);
        request.Headers.Authorization = new AuthenticationHeaderValue(authHeaderValue.scheme, authHeaderValue.token);

        var response = await base.SendAsync(request, cancellationToken);

        // If we get an authentication error, reset the credential cache and potentially retry
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _credentialProvider.Reset();
        }

        return response;
    }
}
