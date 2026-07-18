using eHub.Infrastructure.Identity;

namespace eHub.UnitTests.Infrastructure.Identity;

public sealed class Sha256RefreshTokenHasherTests
{
    private readonly Sha256RefreshTokenHasher _hasher = new();

    [Fact]
    public void GenerateRawToken_ReturnsUniqueValues()
    {
        var first = _hasher.GenerateRawToken(64);
        var second = _hasher.GenerateRawToken(64);

        first.Should().NotBeNullOrWhiteSpace();
        second.Should().NotBeNullOrWhiteSpace();
        first.Should().NotBe(second);
    }

    [Fact]
    public void Hash_IsDeterministicAndNotRawToken()
    {
        const string raw = "refresh-token-value";

        var first = _hasher.Hash(raw);
        var second = _hasher.Hash(raw);

        first.Should().Be(second);
        first.Should().NotBe(raw);
        first.Length.Should().Be(64); // SHA256 hex
    }
}
