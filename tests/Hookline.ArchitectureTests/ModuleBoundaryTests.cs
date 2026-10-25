using System.Reflection;

using NetArchTest.Rules;

using Shouldly;

namespace Hookline.ArchitectureTests;

public class ModuleBoundaryTests
{
    private static readonly string[] ModuleNames = ["Management", "Ingestion", "Delivery"];
    public static TheoryData<string> Modules => new(ModuleNames);
    private static Assembly Load(string module) => Assembly.Load($"Hookline.Modules.{module}");

    [Theory, MemberData(nameof(Modules))]
    public void Module_does_not_reference_other_modules(string module)
    {
        var forbidden = ModuleNames.Where(m => m != module).Select(m => $"Hookline.Modules.{m}");
        var referenced = Load(module).GetReferencedAssemblies().Select(a => a.Name);

        referenced.Intersect(forbidden).ShouldBeEmpty();
    }

    [Theory, MemberData(nameof(Modules))]
    public void Module_exposes_only_its_registration_class(string module)
    {
        var publicTypes = Load(module).GetExportedTypes().Select(t => t.Name);

        publicTypes.ShouldBe(new[] { $"{module}Module" });
    }

    [Theory, MemberData(nameof(Modules))]
    public void Domain_does_not_depend_on_other_layers_or_persistence(string module)
    {
        var result = Types.InAssembly(Load(module))
            .That().ResideInNamespace($"Hookline.Modules.{module}.Domain")
            .ShouldNot().HaveDependencyOnAny(
                $"Hookline.Modules.{module}.Application",
                $"Hookline.Modules.{module}.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Theory, MemberData(nameof(Modules))]
    public void Application_does_not_depend_on_infrastructure_or_persistence(string module)
    {
        var result = Types.InAssembly(Load(module))
            .That().ResideInNamespace($"Hookline.Modules.{module}.Application")
            .ShouldNot().HaveDependencyOnAny(
                $"Hookline.Modules.{module}.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}