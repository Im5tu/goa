using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;

namespace Goa.Functions.EventBridge;

internal sealed class EventbridgeFunctionBuilder : LambdaBuilder, IEventbridgeFunctionBuilder
{
    public EventbridgeFunctionBuilder(IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient)
        : base(builder, lambdaRuntimeClient)
    {
    }

    public ISingleEventHandlerBuilder ProcessOneAtATime()
    {
        return new HandlerBuilder(this);
    }

    public IMultipleEventHandlerBuilder ProcessAsBatch()
    {
        return new HandlerBuilder(this);
    }
}