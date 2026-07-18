using System.Globalization;
using eHub.Domain.Resources;

namespace eHub.UnitTests.Domain.Resources;

public sealed class ErrorResourcesTests
{
    [Fact]
    public void Get_UsesInvariantEnglishByDefault()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            ErrorResources.Get(ErrorCodes.ValidationFailed)
                .Should().Be("One or more validation errors occurred.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Get_UsesAzerbaijaniWhenUiCultureIsAz()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("az");

            ErrorResources.Get(ErrorCodes.ValidationFailed)
                .Should().Be("Bir və ya bir neçə doğrulama xətası baş verdi.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Get_FormatsArguments()
    {
        var message = ErrorResources.Get(ErrorCodes.FieldRequired, "email");

        message.Should().Be("email is required.");
    }
}
