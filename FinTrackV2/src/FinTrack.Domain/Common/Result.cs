namespace FinTrack.Domain.Common;

/// <summary>
/// Wraps the outcome of an operation.
/// Instead of throwing exceptions for expected failures (not found, duplicate email),
/// we return a Result that callers must explicitly handle.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default!, false, error);
}

/// <summary>
/// A Result that also carries a value on success.
/// On failure, Value should not be accessed.
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T _value;

    internal Result(T value, bool isSuccess, Error error) : base(isSuccess, error)
        => _value = value;

    /// <summary>Only access this when IsSuccess is true.</summary>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Cannot access Value of a failed result.");
}
