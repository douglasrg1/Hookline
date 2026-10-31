using System.Diagnostics.CodeAnalysis;

namespace Hookline.SharedKernel;

public class Result
{
    protected Result(Error? error)
    {
        Error = error;
    }

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    public static Result Success() => new(null);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result(error);
    }

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    public static implicit operator Result(Error error) => Failure(error);
}
