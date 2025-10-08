namespace StudentUnionBot.Core.Results;

/// <summary>
/// Представляє результат операції без повернення значення
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("Success result cannot have an error");

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Error = error ?? string.Empty;
    }

    public static Result Ok() => new(true, string.Empty);

    public static Result Fail(string error) => new(false, error);

    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    public static Result<T> Fail<T>(string error) => Result<T>.Fail(error);
}

/// <summary>
/// Представляє результат операції з поверненням значення
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string error)
        : base(isSuccess, error)
    {
        if (isSuccess && value == null)
            throw new InvalidOperationException("Success result must have a value");

        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, string.Empty);

    public static new Result<T> Fail(string error) => new(false, default, error);

    public static Result<T> Fail(IEnumerable<string> errors)
        => new(false, default, string.Join("; ", errors));
}
