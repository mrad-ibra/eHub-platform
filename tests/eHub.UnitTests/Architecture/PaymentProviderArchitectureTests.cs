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
        "Payriff"
    ];

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

    private static string FormatViolations(TestResult result)
        => result.FailingTypes?.Count > 0
            ? string.Join(", ", result.FailingTypes.Select(t => t.FullName))
            : result.FailingTypeNames is { Count: > 0 } names
                ? string.Join(", ", names)
                : "unexpected provider SDK dependency";
}
