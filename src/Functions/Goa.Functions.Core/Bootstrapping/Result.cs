namespace Goa.Functions.Core.Bootstrapping;

public class Result
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }

    protected Result(bool success, string errorMessage)
    {
        IsSuccess = success;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new Result(true, string.Empty);

    public static Result Failure(string errorMessage) => new Result(false, errorMessage);
}

public sealed class Result<T> : Result
{
    public T? Data { get; }

    private Result(T data) : base(true, string.Empty)
    {
        Data = data;
    }

    private Result(string errorMessage) : base(false, errorMessage) { }

    public static Result<T> Success(T data) => new Result<T>(data);

    public new static Result<T> Failure(string errorMessage) => new Result<T>(errorMessage);
}
