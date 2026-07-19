<#
.SYNOPSIS
  Seeds eHub application secrets into HashiCorp Vault (KV v2).

.EXAMPLE
  $env:VAULT_ADDR  = 'http://187.127.88.26:8200'
  $env:VAULT_TOKEN = '<token>'
  .\scripts\vault\seed-ehub-secrets.ps1

  Or pass secrets via env so nothing is hardcoded:
  $env:EHUB_PG_CONNECTION = 'Host=...;Password=...'
  $env:EHUB_JWT_KEY = '...'
#>

$ErrorActionPreference = 'Stop'

$VaultAddr = $env:VAULT_ADDR
if ([string]::IsNullOrWhiteSpace($VaultAddr)) {
    $VaultAddr = 'http://187.127.88.26:8200'
}

$Token = $env:VAULT_TOKEN
if ([string]::IsNullOrWhiteSpace($Token)) {
    throw 'Set VAULT_TOKEN (root or write-capable token) before running this script.'
}

$Path = if ($env:VAULT_SECRET_PATH) { $env:VAULT_SECRET_PATH } else { 'ehub/production' }
$Mount = if ($env:VAULT_MOUNT) { $env:VAULT_MOUNT } else { 'secret' }

$Pg = $env:EHUB_PG_CONNECTION
$Redis = if ($env:EHUB_REDIS) { $env:EHUB_REDIS } else { '187.127.88.26:6379' }
$Jwt = $env:EHUB_JWT_KEY
$SeedEmail = if ($env:EHUB_SEED_EMAIL) { $env:EHUB_SEED_EMAIL } else { 'admin@ehub.local' }
$SeedPassword = $env:EHUB_SEED_PASSWORD
$SeedEnabled = if ($env:EHUB_SEED_ENABLED) { $env:EHUB_SEED_ENABLED } else { 'false' }

if ([string]::IsNullOrWhiteSpace($Pg)) {
    throw 'Set EHUB_PG_CONNECTION to the full Npgsql connection string.'
}
if ([string]::IsNullOrWhiteSpace($Jwt) -or $Jwt.Length -lt 32) {
    throw 'Set EHUB_JWT_KEY to a random secret at least 32 characters.'
}
if ([string]::IsNullOrWhiteSpace($SeedPassword)) {
    $SeedPassword = 'ChangeMe123!'
    Write-Warning "EHUB_SEED_PASSWORD not set; using placeholder ChangeMe123!"
}

# Ensure KV v2 mount exists (ignore if already enabled)
$headers = @{ 'X-Vault-Token' = $Token }
try {
    Invoke-RestMethod -Method Post -Uri "$VaultAddr/v1/sys/mounts/$Mount" -Headers $headers -ContentType 'application/json' -Body (@{
        type = 'kv'
        options = @{ version = '2' }
    } | ConvertTo-Json) | Out-Null
    Write-Host "Enabled KV v2 at mount '$Mount'"
}
catch {
    Write-Host "Mount '$Mount' already present or not creatable (continuing)."
}

$body = @{
    data = @{
        'ConnectionStrings__DefaultConnection' = $Pg
        'ConnectionStrings__Redis'             = $Redis
        'Auth__Jwt__Key'                       = $Jwt
        'Auth__Seed__Enabled'                  = $SeedEnabled
        'Auth__Seed__Email'                    = $SeedEmail
        'Auth__Seed__Password'                 = $SeedPassword
    }
} | ConvertTo-Json -Depth 5

$uri = "$VaultAddr/v1/$Mount/data/$Path"
Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -ContentType 'application/json' -Body $body | Out-Null

Write-Host "Wrote secrets to $Mount/data/$Path"
$check = Invoke-RestMethod -Method Get -Uri $uri -Headers $headers
Write-Host "Keys present:" ($check.data.data.PSObject.Properties.Name -join ', ')
