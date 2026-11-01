using Hookline.SharedKernel.AspNetCore;
using Shouldly;

namespace Hookline.SharedKernel.Tests;

public class ErrorExtensionsTests
{
    public static TheoryData<ErrorType, int> StatusCodes => new()
    {
        { ErrorType.Validation, 400 },
        { ErrorType.NotFound, 404 },
        { ErrorType.Conflict, 409 },
    };

    [Theory, MemberData(nameof(StatusCodes))]
    public void Error_type_maps_to_the_http_status_code(ErrorType type, int expectedStatusCode)
    {
        var problem = new Error("some.code", "Some message.", type).ToProblem();

        problem.StatusCode.ShouldBe(expectedStatusCode);
        problem.ProblemDetails.Status.ShouldBe(expectedStatusCode);
    }

    [Fact]
    public void Problem_details_carry_the_message_and_the_code()
    {
        var problem = Error
            .Conflict("endpoint.duplicate_url", "The endpoint URL is already registered.")
            .ToProblem();

        problem.ProblemDetails.Detail.ShouldBe("The endpoint URL is already registered.");
        problem.ProblemDetails.Extensions[ErrorExtensions.CodeExtensionKey].ShouldBe("endpoint.duplicate_url");
        problem.ProblemDetails.Title.ShouldNotBeNullOrWhiteSpace();
        problem.ProblemDetails.Type.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Unknown_error_type_is_rejected()
    {
        var error = new Error("some.code", "Some message.", (ErrorType)999);

        Should.Throw<ArgumentOutOfRangeException>(() => error.ToProblem());
    }
}
