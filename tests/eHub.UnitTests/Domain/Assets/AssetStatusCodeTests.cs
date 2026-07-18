using eHub.Domain.Assets;
using eHub.Domain.Exceptions;

namespace eHub.UnitTests.Domain.Assets;

public sealed class AssetStatusCodeTests
{
    [Theory]
    [InlineData("DRAFT")]
    [InlineData("draft")]
    [InlineData(" Pending_Approval ")]
    public void Parse_KnownValues_Succeeds(string value)
    {
        var code = AssetStatusCode.Parse(value);

        code.Value.Should().NotBeNullOrWhiteSpace();
        AssetStatusCode.All.Should().Contain(code);
    }

    [Fact]
    public void Parse_UnknownValue_Throws()
    {
        var act = () => AssetStatusCode.Parse("UNKNOWN");

        act.Should().Throw<ValidationFailedException>();
    }

    [Fact]
    public void Equality_IsByValue()
    {
        AssetStatusCode.Draft.Should().Be(AssetStatusCode.Parse("DRAFT"));
        AssetStatusCode.Draft.Should().NotBe(AssetStatusCode.Published);
    }
}
