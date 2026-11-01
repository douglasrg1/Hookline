using System.Net;
using System.Text.Json;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Integration;

internal static class ProblemAssertions
{
    // Checks a Problem Details response and returns its body, for further checks.
    public static async Task<JsonElement> ShouldBeProblemAsync(
        this HttpResponseMessage response,
        HttpStatusCode status,
        string? code = null)
    {
        response.StatusCode.ShouldBe(status);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        if (code is not null)
        {
            problem.GetProperty("code").GetString().ShouldBe(code);
        }

        return problem;
    }
}
