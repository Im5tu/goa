using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Generic;

/// <summary>
/// Handler builder for string-based Lambda functions
/// </summary>
internal sealed class StringHandlerBuilder : TypedHandlerBuilder<string, string>, IStringHandlerBuilder
{
    public StringHandlerBuilder(ILambdaBuilder builder)
        : base(builder, (JsonSerializerContext)StringSerializationContext.Default)
    {
    }

    /// <inheritdoc />
    protected override string GetLoggerName() => "StringHandler";
}
