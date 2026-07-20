using eHub.Application;
using eHub.Domain;
using FluentAssertions;
using NetArchTest.Rules;

namespace eHub.UnitTests.Architecture;

public sealed class PaymentProviderArchitectureTests
{
    private static readonly string[] ForbiddenProviderDependencies =
    [
        "Stripe",
        "Payriff",
        "KapitalBank"
    ];

    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void Domain_Must_Not_Depend_On_Stripe()
    {
        var result = Types.InAssembly(typeof(DomainAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Stripe")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatViolations(result));
    }

    [Fact]
    public void Domain_Must_Not_Depend_On_Payriff()
    {
        var result = Types.InAssembly(typeof(DomainAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Payriff")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatViolations(result));
    }

    [Fact]
    public void Application_Must_Not_Depend_On_Stripe()
    {
        var result = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Stripe")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatViolations(result));
    }

    [Fact]
    public void Application_Must_Not_Depend_On_Payriff()
    {
        var result = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Payriff")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatViolations(result));
    }

    [Fact]
    public void Application_Must_Not_Depend_On_Any_Payment_Provider_Sdk()
    {
        var result = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ForbiddenProviderDependencies)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatViolations(result));
    }

    [Fact]
    public void Domain_csproj_Must_Not_Reference_Provider_Sdk_Packages()
    {
        AssertNoProviderPackageReferences("src/eHub.Domain/eHub.Domain.csproj");
    }

    [Fact]
    public void Application_csproj_Must_Not_Reference_Provider_Sdk_Packages()
    {
        AssertNoProviderPackageReferences("src/eHub.Application/eHub.Application.csproj");
    }

    private static void AssertNoProviderPackageReferences(string relativeProjectPath)
    {
        var path = Path.Combine(RepoRoot, relativeProjectPath);
        var content = File.ReadAllText(path);

        foreach (var forbidden in new[] { "Stripe.net", "Payriff", "KapitalBank" })
        {
            content.Should().NotContain(
                forbidden,
                because: $"{relativeProjectPath} must not reference provider SDK package {forbidden}");
        }
    }

    private static string FormatViolations(TestResult result)
        => result.FailingTypes?.Count > 0
            ? string.Join(", ", result.FailingTypes.Select(t => t.FullName))
            : result.FailingTypeNames is { Count: > 0 } names
                ? string.Join(", ", names)
                : "unexpected provider SDK dependency";

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "eHub.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
