namespace Goa.Clients.Core.Http;

/// <summary>
/// Represents a response from an API operation that can contain either a successful result or an error.
/// </summary>
/// <typeparam name="T">The type of the successful response value.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// Gets the successful response value, or null if the operation failed.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error information if the operation failed, or null if successful.
    /// </summary>
    public ApiError? Error { get; }

    /// <summary>
    /// Gets the HTTP response headers.
    /// </summary>
    public IReadOnlyDictionary<string, IEnumerable<string>>? Headers { get; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Initializes a new instance with a successful response value.
    /// </summary>
    /// <param name="value">The successful response value.</param>
    public ApiResponse(T? value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance with a successful response value and headers.
    /// </summary>
    /// <param name="value">The successful response value.</param>
    /// <param name="headers">The HTTP response headers.</param>
    public ApiResponse(T? value, IReadOnlyDictionary<string, IEnumerable<string>>? headers)
    {
        Value = value;
        Headers = headers;
    }

    /// <summary>
    /// Initializes a new instance with an error.
    /// </summary>
    /// <param name="error">The error that occurred.</param>
    public ApiResponse(ApiError error)
    {
        Error = error;
    }

    /// <summary>
    /// Implicitly converts a value to a successful ApiResponse.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A successful ApiResponse containing the value.</returns>
    public static implicit operator ApiResponse<T>(T value) => new(value);

    /// <summary>
    /// Implicitly converts an ApiError to a failed ApiResponse.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    /// <returns>A failed ApiResponse containing the error.</returns>
    public static implicit operator ApiResponse<T>(ApiError error) => new(error);

    /// <summary>
    /// Implicitly extracts the value from an ApiResponse.
    /// </summary>
    /// <param name="response">The ApiResponse to extract the value from.</param>
    /// <returns>The response value, or null if the operation failed.</returns>
    public static implicit operator T?(ApiResponse<T> response) => response.Value;
}
