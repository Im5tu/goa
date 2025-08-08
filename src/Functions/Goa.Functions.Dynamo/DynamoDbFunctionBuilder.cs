using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;

namespace Goa.Functions.Dynamo;

internal sealed class DynamoDbFunctionBuilder : LambdaBuilder, IDynamoDbFunctionBuilder
{
    public DynamoDbFunctionBuilder(IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient)
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