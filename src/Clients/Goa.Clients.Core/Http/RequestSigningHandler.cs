using Goa.Clients.Core.Credentials;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Goa.Clients.Core.Http;

// public static class Time
// {
//     public static DateTime Now { get; } = DateTime.UtcNow;
// }

internal sealed class RequestSigningHandler : DelegatingHandler
{
    private const string ShortDateFormat = "yyyyMMdd";
    private const string LongDateFormat = ShortDateFormat + "THHmmssZ";
    
    private readonly ICredentialProviderChain _credentialProvider;

    public RequestSigningHandler(ICredentialProviderChain credentialProvider)
    {
        _credentialProvider = credentialProvider;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        // Step 1 - Get credentials, date, region and service
        var time = DateTime.UtcNow;
        var shortDate = time.ToString(ShortDateFormat);
        var longDate = time.ToString(LongDateFormat);
        var (region, serviceName) = GetRequestParameters(request);
        var credentialsResult = await _credentialProvider.GetCredentialsAsync();
        
        if (credentialsResult.IsError)
        {
            throw new InvalidOperationException($"Failed to retrieve AWS credentials: {string.Join("; ", credentialsResult.Errors.Select(e => e.Description))}");
        }
        
        var credentials = credentialsResult.Value;
       
        // Step 2 - Append any headers that we need to sign, such as target, date etc
        var payload = "";
        var payloadHash = "";
        if (request.Content is not null)
        {
            if (request.Options.TryGetValue(HttpOptions.Payload, out var payloadOption) && !string.IsNullOrWhiteSpace(payloadOption))
            {
                payload = payloadOption;
            }
            else
            {
                payload = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            payloadHash = ComputeSHA256Hash(payload);
            
            Debug.WriteLine("Payload Hash: " + payloadHash);
            Debug.WriteLine("Payload:\n" + payload);
            Debug.WriteLine("");
        }
        
        foreach (var (name, value) in GetRequestHeaders(request, longDate, payloadHash, credentials))
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        var signedHeaders = request.Headers.Concat(request?.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()).ToDictionary(h => h.Key, h => string.Join(";", h.Value)); 
        
        // Step 3 - create a canonical request
        var canonicalRequest = CreateCanonicalRequest(
            request!.Method.ToString(),
            request.RequestUri!.AbsolutePath,
            "",
            signedHeaders,
            payload);
        
        Debug.WriteLine("Request:\n" + canonicalRequest);
        Debug.WriteLine("");

        // Step 4 - create a string to sign
        var scope = CreateCredentialScope(shortDate, region, serviceName);
        var stringToSign = CreateStringToSign(longDate, scope, canonicalRequest);

        Debug.WriteLine("Scope:\n" + scope);
        Debug.WriteLine("");
        Debug.WriteLine("String to sign:\n" + stringToSign);
        Debug.WriteLine("");

        // Step 5 - calculate the signature
        var signature = GetSignature(shortDate, stringToSign, credentials.SecretAccessKey, region, serviceName);

        Debug.WriteLine("Signature:\n" + signature);
        Debug.WriteLine("");
        
        // Step 6 - add the signature to the request
        var authHeader = $"Credential={credentials.AccessKeyId}/{scope}, SignedHeaders={string.Join(";", signedHeaders.Select(x => x.Key.ToLower()).Order())}, Signature={signature}";
        request.Headers.Authorization = new AuthenticationHeaderValue("AWS4-HMAC-SHA256", authHeader);
        
        Debug.WriteLine("Auth:\n" + authHeader);
        Debug.WriteLine("");
        
        var response = await base.SendAsync(request, cancellationToken);
        
        // If we get an authentication error, reset the credential cache and potentially retry
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _credentialProvider.Reset();
        }
        
        return response;
    }

    private static IEnumerable<(string header, string value)> GetRequestHeaders(HttpRequestMessage request, string longDate, string payloadHash, AwsCredentials credentials)
    {
        request.Headers.Host = request.RequestUri!.Host;
        
        yield return (RequestHeaders.AmzDate, longDate);
        yield return (RequestHeaders.AmzContentSha256, payloadHash);

        if (request.Options.TryGetValue(HttpOptions.Target, out var target) && !string.IsNullOrWhiteSpace(target))
        {
            yield return (RequestHeaders.AmzTarget, target);
        }
        
        if (request.Options.TryGetValue(HttpOptions.ApiVersion, out var apiVersion) && !string.IsNullOrWhiteSpace(apiVersion))
        {
            yield return (RequestHeaders.AmzApiVersion, apiVersion);
        }
        
        if (!string.IsNullOrWhiteSpace(credentials.SessionToken))
        {
            yield return (RequestHeaders.AmzSecurityToken, credentials.SessionToken);
        }
    }
    private static (string region, string serviceName) GetRequestParameters(HttpRequestMessage request)
    {
        var region = Environment.GetEnvironmentVariable("AWS_REGION");
        if (request.Options.TryGetValue(HttpOptions.Region, out var regionOption) && !string.IsNullOrWhiteSpace(regionOption))
        {
            region = regionOption;
        }
        
        if (string.IsNullOrWhiteSpace(region))
        {
            throw new InvalidOperationException("Region is required");
        }
        
        if (!request.Options.TryGetValue(HttpOptions.Service, out var service) || string.IsNullOrWhiteSpace(service))
        {
            throw new InvalidOperationException("Service name is required");
        }

        return (region, service);
    }
    private static string GetSignature(string dateStamp, string stringToSign, string secretAccessKey, string region, string service)
    {
        // Method verified with the GetSignature benchmarks -> v9 used
        ReadOnlySpan<byte> prefix = "AWS4"u8;
        ReadOnlySpan<byte> request = "aws4_request"u8;
        int aws4KeyLength = prefix.Length + Encoding.UTF8.GetByteCount(secretAccessKey);
        Span<byte> v2 = stackalloc byte[aws4KeyLength];
        prefix.CopyTo(v2);
        Encoding.UTF8.GetBytes(secretAccessKey, v2.Slice(prefix.Length));

        Span<byte> current = stackalloc byte[32];
        HmacSHA256(current, v2, dateStamp);
        HmacSHA256(current, current, region);
        HmacSHA256(current, current, service);
        HmacSHA256(current, current, request);
        HmacSHA256(current, current, stringToSign);

        return ToHexString(current);
    }
    private static string CreateCanonicalRequest(string httpRequestMethod, string canonicalUri, string canonicalQueryString, Dictionary<string, string> headers, string payload)
    {
        var canonicalHeaders = string.Join("\n", headers.OrderBy(h => h.Key).Select(h => $"{h.Key.ToLower()}:{h.Value.Trim()}")) + "\n";
        var signedHeaders = string.Join(";", headers.OrderBy(h => h.Key).Select(h => h.Key.ToLower()));
        var payloadHash = ComputeSHA256Hash(payload);
        return $"{httpRequestMethod.ToUpper()}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
    }
    private static string CreateStringToSign(string dateTime, string credentialScope, string canonicalRequest)
    {
        // Method verified with the CreateStringToSign benchmarks -> v1 used
        var prefix = "AWS4-HMAC-SHA256";
        var hashedRequest = ComputeSHA256Hash(canonicalRequest);
        var length = prefix.Length + 1 + dateTime.Length + 1 + credentialScope.Length + 1 + hashedRequest.Length;
        return string.Create(length, (prefix, dateTime, credentialScope, HashedRequest: hashedRequest), (chars, state) =>
        {
            var (version, longDate, scope, request) = state;
            var i = 0;
            foreach (var c in version)
            {
                chars[i++] = c;
            }
            chars[i++] = '\n';
            foreach (var c in longDate)
            {
                chars[i++] = c;
            }
            chars[i++] = '\n';
            foreach (var c in scope)
            {
                chars[i++] = c;
            }
            chars[i++] = '\n';
            foreach (var c in request)
            {
                chars[i++] = c;
            }
        });
        //return $"AWS4-HMAC-SHA256\n{dateTime}\n{credentialScope}\n{ComputeSHA256Hash(canonicalRequest)}";
    }
    private static string CreateCredentialScope(string date, string region, string service)
    {
        // Method verified with the CreateCredentialScope benchmarks -> v2 used
        ReadOnlySpan<char> charDate = date;
        ReadOnlySpan<char> charRegion = region;
        ReadOnlySpan<char> charService = service;
        ReadOnlySpan<char> charSuffix = "/aws4_request";
        var length = charDate.Length + 1 + charRegion.Length + 1 + charService.Length + charSuffix.Length;
        
        Span<char> chars = length <= 512 ? stackalloc char[length] : new char[length];
        
        charDate.CopyTo(chars);
        var temp = chars.Slice(charDate.Length);
        temp[0] = '/';
        temp = temp.Slice(1);
        
        charRegion.CopyTo(temp);
        temp = temp.Slice(charRegion.Length);
        temp[0] = '/';
        temp = temp.Slice(1);
        
        charService.CopyTo(temp);
        temp = temp.Slice(charService.Length);
       
        charSuffix.CopyTo(temp);

        return new string(chars);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HmacSHA256(in Span<byte> destination, in Span<byte> key, string data)
    {
        // Method verified with the GetSignature benchmarks -> v9 used
        Span<byte> dataBytes = stackalloc byte[data.Length];
        Encoding.UTF8.GetBytes(data, dataBytes);
        HMACSHA256.HashData(key, dataBytes, destination);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HmacSHA256(in Span<byte> destination, in ReadOnlySpan<byte> key, in ReadOnlySpan<byte> data)
    {
        // Method verified with the GetSignature benchmarks -> v9 used
        HMACSHA256.HashData(key, data, destination);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ComputeSHA256Hash(string input)
    {
        var inputCount = Encoding.UTF8.GetByteCount(input);
        
        // SHA256 hash is always 32 bytes long
        Span<byte> destination = stackalloc byte[32];
        // Allocate on the stack if the input is less than or equal to 512 bytes, as this is quicker
        Span<byte> source = inputCount <= 512 ? stackalloc byte[inputCount] : new byte[inputCount];
        Encoding.UTF8.GetBytes(input, source);
        SHA256.HashData(source, destination);

        return ToHexString(destination);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ToHexString(in ReadOnlySpan<byte> input)
    {
        // Verify with the ToHexString benchmarks before changing
        // Desired length is twice the input length as a byte is 2 hex characters, so use a left shift by 1 to multiply by 2
        var desiredLength = input.Length << 1;
        Span<char> c = input.Length <= 512 ? stackalloc char[desiredLength] : new char[desiredLength];
        int b;
        for (int i = 0; i < input.Length; i++)
        {
            b = input[i];
            // https://stackoverflow.com/a/14333437
            // formula to convert to uppercase hex is: (char)(55 + b + (((b-10)>>31)&-7))
            c[i << 1] = (char)(87 + b / 16 + (((b / 16 - 10) >> 31) & -39));
            c[(i << 1) + 1] = (char)(87 + b % 16 + (((b % 16 - 10) >> 31) & -39));
        }
        return new string(c);
    }
}