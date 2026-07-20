#!/usr/bin/env bash
# Runs on the production host after git pull (invoked by GitHub Actions SSH).
set -euo pipefail

ROOT="${EHUB_ROOT:-/opt/eHub-platform}"
ENV_FILE="${EHUB_DEPLOY_ENV:-/opt/ehub/deploy.env}"
IMAGE="${EHUB_API_IMAGE:-ehub-api:latest}"
CONTAINER="${EHUB_API_CONTAINER:-ehub-api}"

cd "$ROOT"

echo "==> Pull latest (skip if already updated by CI)"
if [[ "${EHUB_SKIP_GIT_PULL:-}" != "1" ]]; then
  git fetch origin
  git reset --hard "origin/${EHUB_DEPLOY_BRANCH:-main}"
fi

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing deploy env: $ENV_FILE" >&2
  echo "Create it with VAULT_ADDR, VAULT_TOKEN, VAULT_MOUNT, VAULT_SECRET_PATH" >&2
  exit 1
fi

# shellcheck disable=SC1090
set -a
# shellcheck source=/dev/null
source "$ENV_FILE"
set +a

: "${VAULT_ADDR:?VAULT_ADDR required in $ENV_FILE}"
: "${VAULT_TOKEN:?VAULT_TOKEN required in $ENV_FILE}"
: "${VAULT_MOUNT:=secret}"
: "${VAULT_SECRET_PATH:=ehub/production}"

echo "==> Resolve Postgres connection from Vault (for migrations)"
VAULT_URL="${VAULT_ADDR%/}/v1/${VAULT_MOUNT}/data/${VAULT_SECRET_PATH}"
CONN="$(
  curl -sf -H "X-Vault-Token: ${VAULT_TOKEN}" "$VAULT_URL" \
    | python3 -c 'import json,sys; d=json.load(sys.stdin)["data"]["data"]; print(d.get("ConnectionStrings__DefaultConnection") or d.get("ConnectionStrings:DefaultConnection") or "")'
)"
if [[ -z "$CONN" ]]; then
  echo "Could not read ConnectionStrings__DefaultConnection from Vault" >&2
  exit 1
fi

echo "==> Apply EF migrations"
docker run --rm --network host \
  -v "$ROOT:/src" \
  -w /src \
  -e "ConnectionStrings__DefaultConnection=$CONN" \
  -e DOTNET_NOLOGO=true \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  bash -c '
    set -euo pipefail
    dotnet tool install -g dotnet-ef --version 9.* >/dev/null
    export PATH="$PATH:/root/.dotnet/tools"
    dotnet ef database update \
      --project src/eHub.Persistence/eHub.Persistence.csproj \
      --startup-project src/eHub.Api/eHub.Api.csproj
  '

echo "==> Build API image"
docker build -t "$IMAGE" .

echo "==> Restart API container"
docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
docker run -d \
  --name "$CONTAINER" \
  --restart unless-stopped \
  --network host \
  -e ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}" \
  -e "VAULT_ADDR=$VAULT_ADDR" \
  -e "VAULT_TOKEN=$VAULT_TOKEN" \
  -e "VAULT_MOUNT=$VAULT_MOUNT" \
  -e "VAULT_SECRET_PATH=$VAULT_SECRET_PATH" \
  "$IMAGE"

sleep 3
echo "==> Health"
curl -sf "http://127.0.0.1:8080/health" | head -c 500 || true
echo
echo "==> Deploy done"
docker ps --filter "name=$CONTAINER" --format "table {{.Names}}\t{{.Status}}\t{{.Image}}"
