namespace eHub.Application.Identity.Abstractions;

/// <summary>
/// Generates and hashes refresh tokens. Raw tokens are returned to clients; only hashes are persisted.
/// </summary>
public interface IRefreshTokenHasher
{
    string GenerateRawToken(int sizeBytes);

    string Hash(string rawToken);
}
