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

    public ISingleLogEventHandlerBuilder ProcessOneAtATime()
    {
        return new HandlerBuilder(this, skipControlMessages: true);
    }

    public IMultipleLogEventHandlerBuilder ProcessAsBatch()
    {
        return new HandlerBuilder(this, skipControlMessages: false);
    }
}
