using Hookline.SharedKernel;

using NetArchTest.Rules;

using Shouldly;

namespace Hookline.ArchitectureTests;

public class SharedKernelTests
{
    [Fact]
    public void Shared_kernel_does_not_depend_on_modules_web_or_persistence()
    {
        var result = Types.InAssembly(typeof(Result).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Hookline.Modules",
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
