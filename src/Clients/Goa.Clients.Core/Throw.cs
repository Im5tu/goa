using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Goa.Clients.Core;

/// <summary>
/// Shared throw helpers annotated with <see cref="DoesNotReturnAttribute"/> so the JIT
/// keeps throw-site code out of the inlined hot path.
/// </summary>
public static class Throw
{
    /// <summary>Throws <see cref="IndexOutOfRangeException"/>.</summary>
    [DoesNotReturn]
    public static void IndexOutOfRange() => throw new IndexOutOfRangeException();

    /// <summary>Throws <see cref="InvalidOperationException"/> with the specified message.</summary>
    [DoesNotReturn]
    public static void InvalidOperation(string message) => throw new InvalidOperationException(message);

    /// <summary>Throws <see cref="ArgumentNullException"/> for the specified parameter.</summary>
    [DoesNotReturn]
    public static void ArgumentNull(string paramName) => throw new ArgumentNullException(paramName);

    /// <summary>Throws <see cref="InvalidOperationException"/>. Returns <typeparamref name="T"/> for use in switch expression arms.</summary>
    [DoesNotReturn]
    public static T InvalidOperation<T>(string message) => throw new InvalidOperationException(message);

    /// <summary>Throws <see cref="JsonException"/>. Returns <typeparamref name="T"/> for use in switch expression arms.</summary>
    [DoesNotReturn]
    public static T JsonException<T>(string message) => throw new JsonException(message);
}
