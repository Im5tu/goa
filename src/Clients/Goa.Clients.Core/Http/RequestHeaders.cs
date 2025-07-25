namespace Goa.Clients.Core.Http;

internal static class RequestHeaders
{
    internal static readonly string AmzDate = "X-Amz-Date";
    internal static readonly string AmzSecurityToken = "x-amz-security-token";
    internal static readonly string AmzTarget = "X-Amz-Target";
    internal static readonly string AmzApiVersion = "x-amz-api-version";
    internal static readonly string AmzContentSha256 = "X-Amz-Content-SHA256";
}