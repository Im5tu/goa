namespace Goa.Functions.Core.Bootstrapping;

/// <summary>
/// Represents the result of an operation, indicating success or failure.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Protected constructor for initializing the result of an operation.
    /// </summary>
    /// <param name="success">Indicates whether the operation was successful.</param>
    /// <param name="errorMessage">The error message if the operation failed, otherwise empty.</param>
    protected Result(bool success, string errorMessage)
    {
        IsSuccess = success;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new Result(true, string.Empty);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public static Result Failure(string errorMessage) => new Result(false, errorMessage);
}

/// <summary>
/// Represents the result of an operation that returns a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the data returned by the operation.</typeparam>
public sealed class Result<T> : Result
{
    /// <summary>
    /// Gets the data returned by the operation if it was successful.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Private constructor for a successful result with data.
    /// </summary>
    /// <param name="data">The data returned by the operation.</param>
    private Result(T data) : base(true, string.Empty)
    {
        Data = data;
    }

    /// <summary>
    /// Private constructor for a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    private Result(string errorMessage) : base(false, errorMessage) { }

    /// <summary>
    /// Creates a successful result with the specified data.
    /// </summary>
    /// <param name="data">The data returned by the operation.</param>
    public static Result<T> Success(T data) => new Result<T>(data);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public new static Result<T> Failure(string errorMessage) => new Result<T>(errorMessage);
}

