using Shouldly;

namespace Hookline.SharedKernel.Tests;

public class ErrorTests
{
    [Fact]
    public void Factory_methods_set_the_error_type()
    {
        Error.Validation("code", "message").Type.ShouldBe(ErrorType.Validation);
        Error.NotFound("code", "message").Type.ShouldBe(ErrorType.NotFound);
        Error.Conflict("code", "message").Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void Errors_with_the_same_data_are_equal()
    {
        Error.NotFound("application.not_found", "Application not found.")
            .ShouldBe(Error.NotFound("application.not_found", "Application not found."));
    }
}
