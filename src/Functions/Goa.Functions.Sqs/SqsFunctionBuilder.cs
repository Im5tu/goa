using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;

namespace Goa.Functions.Sqs;

internal sealed class SqsFunctionBuilder : LambdaBuilder, ISqsFunctionBuilder
{
    public SqsFunctionBuilder(IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient)
        : base(builder, lambdaRuntimeClient)
    {
    }

    public ISingleMessageHandlerBuilder ProcessOneAtATime()
    {
        return new HandlerBuilder(this);
    }

    public IMultipleMessageHandlerBuilder ProcessAsBatch()
    {
        return new HandlerBuilder(this);
    }
}