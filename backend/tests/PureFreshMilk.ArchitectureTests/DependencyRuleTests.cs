using System.Reflection;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;

namespace PureFreshMilk.ArchitectureTests;

/// <summary>
/// Enforces the Onion dependency rule: dependencies point inwards only.
/// <code>
/// Api ──► Application ──► Domain
///  └────► Infrastructure ──► Application, Domain
/// </code>
/// Changing an allowed reference here is an architectural decision — record it in
/// docs/architecture/decisions.
/// </summary>
public sealed class DependencyRuleTests
{
    private const string Domain = "PureFreshMilk.Domain";
    private const string Application = "PureFreshMilk.Application";
    private const string Infrastructure = "PureFreshMilk.Infrastructure";
    private const string Api = "PureFreshMilk.Api";

    private static readonly string[] FrameworkPrefixes =
        ["Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Npgsql"];

    public static TheoryData<string, string[]> AllowedProjectReferences => new()
    {
        { Domain, [] },
        { Application, [Domain] },
        { Infrastructure, [Application, Domain] },
        { Api, [Application, Infrastructure] },
    };

    [Theory]
    [MemberData(nameof(AllowedProjectReferences))]
    public void Project_references_point_inwards_only(string project, string[] allowed)
    {
        // The .csproj files write references with Windows separators, and Path only treats '\' as a
        // separator on Windows, so normalise before taking the file name.
        var actual = ReadProjectFile(project)
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(
                ((string)reference.Attribute("Include")!).Replace('\\', '/')))
            .Order(StringComparer.Ordinal);

        Assert.Equal(allowed.Order(StringComparer.Ordinal), actual);
    }

    [Fact]
    public void Domain_has_no_package_references()
    {
        Assert.Empty(ReadProjectFile(Domain).Descendants("PackageReference"));
    }

    [Fact]
    public void Domain_assembly_references_only_the_base_class_library()
    {
        var references = ReferencedAssemblyNames(Domain);

        Assert.All(references, name => Assert.True(
            name.StartsWith("System", StringComparison.Ordinal) || name is "netstandard" or "mscorlib",
            $"{Domain} must not reference {name}."));
    }

    [Theory]
    [InlineData(Application, new[] { Infrastructure, Api })]
    [InlineData(Infrastructure, new[] { Api })]
    public void Assemblies_do_not_reference_outer_layers(string project, string[] outerLayers)
    {
        var references = ReferencedAssemblyNames(project);

        Assert.All(outerLayers, layer => Assert.DoesNotContain(layer, references));
    }

    [Fact]
    public void Application_assembly_does_not_reference_web_or_persistence_frameworks()
    {
        var references = ReferencedAssemblyNames(Application);

        Assert.All(references, name => Assert.False(
            FrameworkPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)),
            $"{Application} must not reference {name}."));
    }

    [Fact]
    public void Infrastructure_does_not_expose_its_DbContext()
    {
        var publicContexts = Assembly.Load(Infrastructure)
            .GetExportedTypes()
            .Where(type => typeof(DbContext).IsAssignableFrom(type));

        Assert.Empty(publicContexts);
    }

    private static string[] ReferencedAssemblyNames(string project) =>
        Assembly.Load(project).GetReferencedAssemblies().Select(name => name.Name!).ToArray();

    private static XDocument ReadProjectFile(string project) =>
        XDocument.Load(Path.Combine(FindBackendDirectory(), "src", project, $"{project}.csproj"));

    private static string FindBackendDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PureFreshMilk.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find PureFreshMilk.slnx above the test output directory.");
    }
}
