using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;

namespace Goa.Functions.Kinesis;

internal sealed class KinesisFunctionBuilder : LambdaBuilder, IKinesisFunctionBuilder
{
    public KinesisFunctionBuilder(IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient)
        : base(builder, lambdaRuntimeClient)
    {
    }

    public ISingleRecordHandlerBuilder ProcessOneAtATime()
    {
        return new HandlerBuilder(this);
    }

    public IMultipleRecordHandlerBuilder ProcessAsBatch()
    {
        return new HandlerBuilder(this);
    }
}