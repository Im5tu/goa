using ErrorOr;

namespace Goa.Clients.Core.Credentials;

/// <summary>
/// A credential provider that chains multiple credential providers together.
/// Tries providers in reverse registration order (latest registered first).
/// Implements caching with a 15-minute TTL for performance optimization.
/// </summary>
public sealed class CredentialProviderChain : ICredentialProviderChain, IDisposable
{
    private readonly List<ICredentialProvider> _providers;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(15);

    private AwsCredentials? _cachedCredentials;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan _expirationBuffer = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialProviderChain"/> class.
    /// </summary>
    /// <param name="providers">The credential providers to chain together.</param>
    public CredentialProviderChain(IEnumerable<ICredentialProvider> providers)
    {
        _providers = providers.ToList();
    }

    /// <inheritdoc />
    public ValueTask<ErrorOr<AwsCredentials>> GetCredentialsAsync()
    {
        var now = DateTime.UtcNow; // Capture once, reuse everywhere
        
        // Fast path: Check if cached credentials are still valid without async overhead
        if (_cachedCredentials != null && now < _cacheExpiry)
        {
            // If credentials have expiration, check if they expire soon
            // Note: _cacheExpiry already has the buffer applied, so no need to add it again
            if (_cachedCredentials.Expiration.HasValue && now >= _cacheExpiry)
            {
                // Credentials expire soon, take slow path to refresh
                return new (GetCredentialsSlowPathAsync(now));
            }

            // Return cached credentials synchronously
            return new ValueTask<ErrorOr<AwsCredentials>>(_cachedCredentials);
        }

        // Slow path: Need to acquire lock and potentially fetch new credentials
        return new (GetCredentialsSlowPathAsync(now));
    }

    private async Task<ErrorOr<AwsCredentials>> GetCredentialsSlowPathAsync(DateTime now)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Double-check the cache after acquiring the lock (another thread might have updated it)
            if (_cachedCredentials != null && now < _cacheExpiry)
            {
                // If credentials have expiration, check if they expire soon
                // Note: _cacheExpiry already has the buffer applied, so no need to add it again
                if (_cachedCredentials.Expiration.HasValue && now >= _cacheExpiry)
                {
                    // Credentials expire soon, clear cache to force refresh
                    _cachedCredentials = null;
                    _cacheExpiry = DateTime.MinValue;
                }
                else
                {
                    return _cachedCredentials;
                }
            }

            var errors = new List<Error>();

            // Try providers in reverse order (latest registered first)
            for (int i = _providers.Count - 1; i >= 0; i--)
            {
                var provider = _providers[i];
                var result = await provider.GetCredentialsAsync();

                if (!result.IsError)
                {
                    // Cache the successful result
                    _cachedCredentials = result.Value;

                    // Set cache expiry to the earlier of: credential expiration or cache TTL
                    var defaultExpiry = now.Add(_cacheTtl);
                    if (result.Value.Expiration.HasValue)
                    {
                        var credentialExpiry = result.Value.Expiration.Value.Subtract(_expirationBuffer);
                        _cacheExpiry = credentialExpiry < defaultExpiry ? credentialExpiry : defaultExpiry;
                    }
                    else
                    {
                        _cacheExpiry = defaultExpiry;
                    }

                    return result.Value;
                }

                errors.AddRange(result.Errors);
            }

            return Error.NotFound("CredentialChain", $"No credentials found from any provider. Tried {_providers.Count} providers. Errors: {string.Join("; ", errors.Select(e => e.Description))}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _semaphore.Wait();
        try
        {
            // Clear cached credentials to force fresh lookup
            _cachedCredentials = null;
            _cacheExpiry = DateTime.MinValue;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore?.Wait();
        try
        {
            foreach (var provider in _providers)
            {
                if (provider is IDisposable disposable)
                    disposable.Dispose();
            }
            _providers.Clear();
        }
        finally
        {
            _semaphore?.Release();
        }

        _semaphore?.Dispose();
    }
}
