using Shouldly;

namespace Hookline.SharedKernel.Tests;

public class ResultTests
{
    private static readonly Error SomeError = Error.Validation("endpoint.invalid_url", "The URL must use https.");

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_carries_the_error()
    {
        var result = Result.Failure(SomeError);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(SomeError);
    }

    [Fact]
    public void Failure_without_error_is_rejected()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void Error_converts_implicitly_to_a_failed_result()
    {
        Result result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
    }

    [Fact]
    public void Successful_result_of_value_exposes_the_value()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failed_result_of_value_does_not_expose_a_value()
    {
        var result = Result<int>.Failure(SomeError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_of_value_without_error_is_rejected()
    {
        Should.Throw<ArgumentNullException>(() => Result<int>.Failure(null!));
    }

    [Fact]
    public void Value_and_error_convert_implicitly_to_a_result_of_value()
    {
        Result<string> success = "created";
        Result<string> failure = SomeError;

        success.Value.ShouldBe("created");
        failure.Error.ShouldBe(SomeError);
    }
}
