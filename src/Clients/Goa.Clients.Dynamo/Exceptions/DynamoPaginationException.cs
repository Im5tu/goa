using System.Diagnostics.CodeAnalysis;
using ErrorOr;

namespace Goa.Clients.Dynamo.Exceptions;

/// <summary>
/// Exception thrown when an auto-pagination operation encounters an error during a page request.
/// This prevents silent truncation of results in methods like QueryAllAsync, ScanAllAsync, and BatchGetAllAsync.
/// </summary>
public sealed class DynamoPaginationException : Exception
{
    /// <summary>
    /// Gets the underlying error that caused the pagination failure.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoPaginationException"/> class.
    /// </summary>
    /// <param name="error">The error that caused the pagination failure.</param>
    public DynamoPaginationException(Error error)
        : base($"A DynamoDB pagination request failed with error '{error.Code}': {error.Description}")
    {
        Error = error;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoPaginationException"/> class with an inner exception.
    /// </summary>
    /// <param name="error">The error that caused the pagination failure.</param>
    /// <param name="innerException">The inner exception.</param>
    public DynamoPaginationException(Error error, Exception innerException)
        : base($"A DynamoDB pagination request failed with error '{error.Code}': {error.Description}", innerException)
    {
        Error = error;
    }

    [DoesNotReturn]
    internal static void Throw(Error error) => throw new DynamoPaginationException(error);
}
