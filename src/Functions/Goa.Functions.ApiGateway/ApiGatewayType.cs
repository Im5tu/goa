namespace Goa.Functions.ApiGateway;

/// <summary>
///     The type of API gateway that you are integrating with
/// </summary>
public enum ApiGatewayType
{
    /// <summary>
    ///     Represents the request payload for AWS API Gateway Proxy integration (V2).
    /// </summary>
    HttpV2,
    /// <summary>
    ///     Represents the request payload for AWS API Gateway Proxy integration (V1).
    /// </summary>
    HttpV1,
    /// <summary>
    ///     Represents the request payload for AWS API Gateway Proxy integration (V1) as a REST API.
    /// </summary>
    RestApi
}
