using System.Security.Cryptography;
using System.Text;
using eHub.Application.Identity.Abstractions;

namespace eHub.Infrastructure.Identity;

public sealed class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string GenerateRawToken(int sizeBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sizeBytes, 32);

        Span<byte> bytes = stackalloc byte[sizeBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }
}
