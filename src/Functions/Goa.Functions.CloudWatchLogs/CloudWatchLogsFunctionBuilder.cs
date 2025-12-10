using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;

namespace Goa.Functions.CloudWatchLogs;

internal sealed class CloudWatchLogsFunctionBuilder : LambdaBuilder, ICloudWatchLogsFunctionBuilder
{
    public CloudWatchLogsFunctionBuilder(IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient)
        : base(builder, lambdaRuntimeClient)
    {
    }

    public ICloudWatchLogsHandlerBuilder ProcessWithoutControlMessages()
    {
        return new HandlerBuilder(this, skipControlMessages: true);
    }

    public ICloudWatchLogsHandlerBuilder ProcessWithControlMessages()
    {
        return new HandlerBuilder(this, skipControlMessages: false);
    }
}
