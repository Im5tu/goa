using Goa.Functions.Core;

namespace Goa.Functions.ApiGateway;

/// <summary>
///     Use this function if you have a HTTP API using the V2 payload. This class provides Dependency Injection (DI), logging, and configuration.
/// </summary>
/// <remarks>https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-develop-integrations-lambda.html</remarks>
public abstract class ProxyPayloadV2Function : FunctionBase<ProxyPayloadV2Request, ProxyPayloadV2Response>
{
}
