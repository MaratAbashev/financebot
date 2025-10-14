namespace FinBot.Domain.Utils;

public class Result<T>(bool isSuccess, T data, string? errorMessage, ErrorType errorType = default)
{
    public bool IsSuccess { get; } = isSuccess;
    public string? ErrorMessage { get; } = errorMessage;
    public T Data { get; } = data;
    public ErrorType ErrorType { get; } = errorType;

    public static Result<T> Success(T data) => new(true, data, null);

    public static Result<T> Failure(string error, ErrorType errorType = ErrorType.Exception) =>
        new(false, default!, error, errorType);
    
    public Result<TResult> SameFailure<TResult>() => Result<TResult>.Failure(ErrorMessage!, ErrorType);
    public Result SameFailure() => Result.Failure(ErrorMessage!, ErrorType);
}

public class Result(bool isSuccess, string? errorMessage, ErrorType errorType = default)
{
    public bool IsSuccess { get; } = isSuccess;
    public string? ErrorMessage { get; } = errorMessage;
    public ErrorType ErrorType { get; } = errorType;

    public static Result Success() => new(true, null);

    public static Result Failure(string error, ErrorType errorType = ErrorType.Exception) =>
        new(false, error, errorType);
}