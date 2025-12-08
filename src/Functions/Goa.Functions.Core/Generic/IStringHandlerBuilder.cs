namespace Goa.Functions.Core.Generic;

/// <summary>
/// Builder interface for configuring string-based Lambda handlers
/// </summary>
public interface IStringHandlerBuilder : ITypedHandlerBuilder<string, string>
{
    // Inherits HandleWith from ITypedHandlerBuilder<string, string>
    // Default method handles non-CT → CT delegation
}
