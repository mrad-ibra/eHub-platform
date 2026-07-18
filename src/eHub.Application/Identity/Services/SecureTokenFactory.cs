using System.Security.Cryptography;

namespace eHub.Application.Identity.Services;

internal static class SecureTokenFactory
{
    public static string Create(int byteLength = 32)
    {
        Span<byte> bytes = stackalloc byte[byteLength];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
