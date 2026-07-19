# Vault — eHub secrets

**Vault:** `http://187.127.88.26:8200` (healthy, unsealed)  
**Engine:** KV v2 mount `secret`  
**App path:** `secret/data/ehub/production` (and optionally `ehub/development`)

Secrets live in Vault. The API reads them at startup when `VAULT_ADDR` + auth are set.  
`appsettings*.json` keeps **non-secret** defaults only (`CHANGE_ME` / empty connection strings).

## What goes into Vault

| Config key (ASP.NET) | Purpose |
|----------------------|---------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL |
| `ConnectionStrings:Redis` | Redis |
| `Auth:Jwt:Key` | JWT signing key (≥32 chars, not `CHANGE_ME*`) |
| `Auth:Seed:Email` | Seed admin email (optional) |
| `Auth:Seed:Password` | Seed admin password |
| `Auth:Seed:Enabled` | `true` / `false` |
| Future: `Payment:Providers:*` | Provider API keys / webhook secrets |

## One-time Vault setup (on server or from your PC)

```bash
# Set token (root or a token with write on secret/*)
export VAULT_ADDR=http://187.127.88.26:8200
export VAULT_TOKEN=<your-token>

# Enable KV v2 if not already
vault secrets enable -path=secret kv-v2 || true

# Write production secrets (edit values; never commit the real password)
vault kv put secret/ehub/production \
  ConnectionStrings__DefaultConnection='Host=187.127.88.26;Port=5432;Database=appdb;Username=appuser;Password=***' \
  ConnectionStrings__Redis='187.127.88.26:6379' \
  Auth__Jwt__Key='REPLACE_WITH_LONG_RANDOM_SECRET_32+' \
  Auth__Seed__Enabled='false' \
  Auth__Seed__Email='admin@ehub.local' \
  Auth__Seed__Password='REPLACE_ME'

# Verify
vault kv get secret/ehub/production
```

PowerShell equivalent: [`scripts/vault/seed-ehub-secrets.ps1`](../../scripts/vault/seed-ehub-secrets.ps1)

## AppRole (recommended for API, not root token)

```bash
vault policy write ehub-api - <<'EOF'
path "secret/data/ehub/*" {
  capabilities = ["read"]
}
path "secret/metadata/ehub/*" {
  capabilities = ["read", "list"]
}
EOF

vault auth enable approle || true
vault write auth/approle/role/ehub-api \
  token_policies="ehub-api" \
  token_ttl=1h \
  token_max_ttl=4h

vault read -field=role_id auth/approle/role/ehub-api/role-id
vault write -field=secret_id -f auth/approle/role/ehub-api/secret-id
```

API env:

```text
VAULT_ADDR=http://187.127.88.26:8200
VAULT_ROLE_ID=...
VAULT_SECRET_ID=...
VAULT_MOUNT=secret
VAULT_SECRET_PATH=ehub/production
```

Or for local/dev with a short-lived token:

```text
VAULT_ADDR=http://187.127.88.26:8200
VAULT_TOKEN=...
VAULT_SECRET_PATH=ehub/development
```

### Local IDE (recommended): user-secrets

`launchSettings` sets `VAULT_ADDR` only (token is **not** committed). Store the token in user-secrets:

```powershell
cd C:\Users\murad.i\source\eHub-platform\src\eHub.Api
dotnet user-secrets set "Vault:Token" "<your-vault-token>"
```

Then F5 / `dotnet run` with the `https` profile. Auth resolution order: env `VAULT_TOKEN` → user-secret `Vault:Token` → AppRole env/config.

## How the API loads secrets

When `VAULT_ADDR` is set, `Program` adds Vault as a configuration source (after appsettings + user-secrets).  
Nested keys use `__` in Vault and map to `:` in .NET (`ConnectionStrings__DefaultConnection` → `ConnectionStrings:DefaultConnection`).

## Security notes

- Do **not** commit real passwords or root tokens.
- Prefer AppRole over long-lived root token for the API.
- After putting Postgres password in Vault, rotate it if it was ever shared in chat.
- Vault UI/API port `8200` should not be public without TLS + auth; restrict firewall.
