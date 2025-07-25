namespace Goa.Clients.Core.Credentials;

internal interface ICredentialProviderChain : ICredentialProvider
{
    /// <summary>
    /// Clears any cached credentials and forces a fresh lookup on the next call to GetCredentialsAsync.
    /// </summary>
    void Reset();
}
