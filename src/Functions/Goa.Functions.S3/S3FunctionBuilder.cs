using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;

namespace Goa.Functions.S3;

internal sealed class S3FunctionBuilder : LambdaBuilder, IS3FunctionBuilder
{
    public S3FunctionBuilder(IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient)
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