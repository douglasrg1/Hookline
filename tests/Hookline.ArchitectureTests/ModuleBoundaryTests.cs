using System.Reflection;

using NetArchTest.Rules;

using Shouldly;

namespace Hookline.ArchitectureTests;

public class ModuleBoundaryTests
{
    private static readonly string[] ModuleNames = DiscoverModules();
    public static TheoryData<string> Modules => new(ModuleNames);
    private static Assembly Load(string module) => Assembly.Load($"Hookline.Modules.{module}");

    private static string[] DiscoverModules()
    {
        var modulesDirectory = Path.Combine(FindRepositoryRoot(), "src", "Modules");

        return Directory.GetDirectories(modulesDirectory)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order()
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hookline.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not find the repository root (Hookline.slnx).");
    }

    [Fact]
    public void Modules_are_discovered_from_source_folders()
    {
        ModuleNames.ShouldNotBeEmpty();
    }

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
    public void Domain_does_not_depend_on_other_layers_web_or_persistence(string module)
    {
        AssertLayerDoesNotDependOn(
            module,
            "Domain",
            $"Hookline.Modules.{module}.Application",
            $"Hookline.Modules.{module}.Infrastructure",
            $"Hookline.Modules.{module}.Presentation",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
    }

    [Theory, MemberData(nameof(Modules))]
    public void Application_does_not_depend_on_outer_layers_web_or_persistence(string module)
    {
        AssertLayerDoesNotDependOn(
            module,
            "Application",
            $"Hookline.Modules.{module}.Infrastructure",
            $"Hookline.Modules.{module}.Presentation",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
    }

    [Theory, MemberData(nameof(Modules))]
    public void Infrastructure_does_not_depend_on_presentation_or_web(string module)
    {
        AssertLayerDoesNotDependOn(
            module,
            "Infrastructure",
            $"Hookline.Modules.{module}.Presentation",
            "Microsoft.AspNetCore");
    }

    [Theory, MemberData(nameof(Modules))]
    public void Presentation_depends_only_on_application(string module)
    {
        AssertLayerDoesNotDependOn(
            module,
            "Presentation",
            $"Hookline.Modules.{module}.Domain",
            $"Hookline.Modules.{module}.Infrastructure",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
    }

    [Theory, MemberData(nameof(Modules))]
    public void Only_presentation_depends_on_the_aspnetcore_shared_kernel(string module)
    {
        var result = Types.InAssembly(Load(module))
            .That().DoNotResideInNamespace($"Hookline.Modules.{module}.Presentation")
            .ShouldNot().HaveDependencyOn("Hookline.SharedKernel.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    private static void AssertLayerDoesNotDependOn(string module, string layer, params string[] forbidden)
    {
        var result = Types.InAssembly(Load(module))
            .That().ResideInNamespace($"Hookline.Modules.{module}.{layer}")
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
