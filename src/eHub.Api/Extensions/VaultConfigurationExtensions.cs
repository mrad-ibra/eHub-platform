using System.Net.Http.Json;
using System.Text.Json;

namespace eHub.Api.Extensions;

/// <summary>
/// Loads flat KV v2 secrets from Vault into <see cref="IConfiguration"/>.
/// Keys use ASP.NET env style (<c>ConnectionStrings__DefaultConnection</c>) and are mapped to <c>:</c>.
/// Enabled when <c>VAULT_ADDR</c> is set. Auth: <c>VAULT_TOKEN</c> or AppRole (<c>VAULT_ROLE_ID</c> + <c>VAULT_SECRET_ID</c>).
/// </summary>
public static class VaultConfigurationExtensions
{
    public static WebApplicationBuilder AddEHubVaultConfiguration(this WebApplicationBuilder builder)
    {
        var addr = Environment.GetEnvironmentVariable("VAULT_ADDR");
        if (string.IsNullOrWhiteSpace(addr))
        {
            return builder;
        }

        var config = builder.Configuration;
        var mount = FirstNonEmpty(
            Environment.GetEnvironmentVariable("VAULT_MOUNT"),
            config["Vault:Mount"]) ?? "secret";
        var secretPath = FirstNonEmpty(
            Environment.GetEnvironmentVariable("VAULT_SECRET_PATH"),
            config["Vault:SecretPath"]) ?? "ehub/production";

        ((IConfigurationBuilder)builder.Configuration).Add(new VaultKvConfigurationSource
        {
            Address = addr.TrimEnd('/'),
            Mount = mount,
            SecretPath = secretPath,
            // Prefer process env; fall back to user-secrets / appsettings (Vault:Token, etc.).
            Token = FirstNonEmpty(
                Environment.GetEnvironmentVariable("VAULT_TOKEN"),
                config["Vault:Token"]),
            RoleId = FirstNonEmpty(
                Environment.GetEnvironmentVariable("VAULT_ROLE_ID"),
                config["Vault:RoleId"]),
            SecretId = FirstNonEmpty(
                Environment.GetEnvironmentVariable("VAULT_SECRET_ID"),
                config["Vault:SecretId"])
        });

        // Serilog may not be fully configured yet; console helps confirm Vault wired at boot.
        Console.WriteLine($"[eHub] Vault configuration source registered: {addr.TrimEnd('/')} → {mount}/data/{secretPath}");

        return builder;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

file sealed class VaultKvConfigurationSource : IConfigurationSource
{
    public required string Address { get; init; }
    public required string Mount { get; init; }
    public required string SecretPath { get; init; }
    public string? Token { get; init; }
    public string? RoleId { get; init; }
    public string? SecretId { get; init; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new VaultKvConfigurationProvider(this);
}

file sealed class VaultKvConfigurationProvider(VaultKvConfigurationSource source) : ConfigurationProvider
{
    public override void Load()
    {
        using var http = new HttpClient { BaseAddress = new Uri(source.Address + "/") };
        var token = ResolveToken(http);
        http.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var path = $"v1/{source.Mount}/data/{source.SecretPath.Trim('/')}";
        using var response = http.GetAsync(path).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(response.Content.ReadAsStream());
        if (!doc.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("data", out var secrets))
        {
            throw new InvalidOperationException($"Vault response at '{path}' has no data.data payload.");
        }

        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in secrets.EnumerateObject())
        {
            var key = property.Name.Replace("__", ":", StringComparison.Ordinal);
            map[key] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => property.Value.GetRawText()
            };
        }

        Data = map;
    }

    private string ResolveToken(HttpClient http)
    {
        if (!string.IsNullOrWhiteSpace(source.Token))
        {
            return source.Token!;
        }

        if (string.IsNullOrWhiteSpace(source.RoleId) || string.IsNullOrWhiteSpace(source.SecretId))
        {
            throw new InvalidOperationException(
                "Vault is enabled (VAULT_ADDR) but no auth is configured. Set VAULT_TOKEN (or user-secret Vault:Token), " +
                "or AppRole via VAULT_ROLE_ID + VAULT_SECRET_ID (or Vault:RoleId + Vault:SecretId).");
        }

        var login = http.PostAsJsonAsync(
                "v1/auth/approle/login",
                new { role_id = source.RoleId, secret_id = source.SecretId })
            .GetAwaiter()
            .GetResult();
        login.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(login.Content.ReadAsStream());
        var clientToken = doc.RootElement
            .GetProperty("auth")
            .GetProperty("client_token")
            .GetString();

        return clientToken
            ?? throw new InvalidOperationException("AppRole login did not return client_token.");
    }
}
