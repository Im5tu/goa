using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Goa.Clients.Core;

internal static class Throw
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void IndexOutOfRange() => throw new IndexOutOfRangeException();

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InvalidOperation(string message) => throw new InvalidOperationException(message);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ArgumentNull(string paramName) => throw new ArgumentNullException(paramName);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T InvalidOperation<T>(string message) => throw new InvalidOperationException(message);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T JsonException<T>(string message) => throw new JsonException(message);
}
