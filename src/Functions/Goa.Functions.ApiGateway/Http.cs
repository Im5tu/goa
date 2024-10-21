namespace Goa.Functions.ApiGateway;

/// <summary>
///     Builder for using ApiGateway Functions
/// </summary>
public static class Http
{
    /// <summary>
    ///     Use an API Gateway REST API
    /// </summary>
    public static IHttpBuilder UseRestApi() => UseHttpV1();

    /// <summary>
    ///     Use an API Gateway HTTP API with the V1 Payload
    /// </summary>
    public static IHttpBuilder UseHttpV1()
    {
        return new HttpBuilder();
    }

    /// <summary>
    ///     Use an API Gateway HTTP API with the V2 Payload
    /// </summary>
    public static IHttpBuilder UseHttpV2()
    {
        return new HttpBuilder();
    }
}
