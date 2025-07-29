namespace Goa.Functions.Core;

/// <summary>
///     Specifies how initialization tasks should be executed during Lambda startup.
/// </summary>
public enum InitializationMode
{
    /// <summary>
    ///     Execute initialization tasks in parallel for faster startup.
    /// </summary>
    Parallel,
    /// <summary>
    ///     Execute initialization tasks sequentially, one after another.
    /// </summary>
    Serial
}