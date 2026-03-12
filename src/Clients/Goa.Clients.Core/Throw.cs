using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Goa.Clients.Core;

internal static class Throw
{
    [DoesNotReturn]

    public static void IndexOutOfRange() => throw new IndexOutOfRangeException();

    [DoesNotReturn]

    public static void InvalidOperation(string message) => throw new InvalidOperationException(message);

    [DoesNotReturn]

    public static void ArgumentNull(string paramName) => throw new ArgumentNullException(paramName);

    [DoesNotReturn]

    public static T InvalidOperation<T>(string message) => throw new InvalidOperationException(message);

    [DoesNotReturn]

    public static T JsonException<T>(string message) => throw new JsonException(message);
}
