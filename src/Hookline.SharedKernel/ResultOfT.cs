namespace Hookline.SharedKernel;

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value)
        : base(null)
    {
        _value = value;
    }

    private Result(Error error)
        : base(error)
    {
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public static Result<TValue> Success(TValue value) => new(value);

    public static new Result<TValue> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<TValue>(error);
    }

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
