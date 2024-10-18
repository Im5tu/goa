using System.Text.Json.Serialization;
using Goa.Functions.ApiGateway;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;

namespace TestConsole;

public class Test : ProxyPayloadV2Function
{
    protected override Task<ProxyPayloadV2Response> HandleRequestAsync(IServiceProvider services, ProxyPayloadV2Request request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
