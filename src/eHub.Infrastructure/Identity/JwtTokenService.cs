using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace eHub.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<AuthOptions> options, IClock clock) : IJwtTokenService
{
    public const string SessionIdClaimType = "sid";

    private readonly JwtOptions _jwt = options.Value.Jwt;

    public AccessTokenResult CreateAccessToken(
        Guid userId,
        string email,
        string accountKind,
        IEnumerable<string> roles,
        Guid? sessionId = null)
    {
        EnsureConfigured();

        var now = clock.UtcNow;
        var expires = now.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new("account_kind", accountKind),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (sessionId is { } sid)
        {
            claims.Add(new Claim(SessionIdClaimType, sid.ToString("D")));
        }

        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_jwt.Key) || _jwt.Key.Length < 32)
        {
            throw new ConfigurationException(ErrorResources.Get(ErrorCodes.JwtConfigMissing));
        }
    }
}
