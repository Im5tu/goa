using Goa.Functions.Core;

namespace Goa.Functions.ApiGateway;

/// <summary>
///     Use this function if you have a REST API or a HTTP API using the V1 payload. This class provides Dependency Injection (DI), logging, and configuration.
/// </summary>
/// <remarks>https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-develop-integrations-lambda.html</remarks>
public abstract class ProxyPayloadV1Function : FunctionBase<ProxyPayloadV1Request, ProxyPayloadV1Response>
{
}
