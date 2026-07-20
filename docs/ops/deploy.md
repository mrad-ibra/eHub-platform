# Production deploy (GitHub Actions → SSH)

On every push to `main` (after CI tests pass), Actions SSHs into the server and runs
`scripts/deploy/remote-deploy.sh`:

1. `git pull` / hard reset to `origin/main`
2. Read Postgres connection from Vault
3. `dotnet ef database update` (SDK container)
4. `docker build` API image
5. Recreate `ehub-api` container (`--network host` + Vault env)

## One-time server setup

### 1) Repo on server (private GitHub)

```bash
# Deploy key (read-only) — GitHub → Settings → Deploy keys
ssh-keygen -t ed25519 -f ~/.ssh/ehub_deploy -N ""
cat ~/.ssh/ehub_deploy.pub   # add as Deploy key

mkdir -p ~/.ssh
cat >> ~/.ssh/config <<'EOF'
Host github.com
  IdentityFile ~/.ssh/ehub_deploy
  StrictHostKeyChecking accept-new
EOF

sudo mkdir -p /opt/eHub-platform /opt/ehub
sudo chown -R "$USER:$USER" /opt/eHub-platform /opt/ehub
git clone git@github.com:mrad-ibra/eHub-platform.git /opt/eHub-platform
```

### 2) Deploy env (secrets stay on server, not in git)

```bash
sudo tee /opt/ehub/deploy.env <<'EOF'
VAULT_ADDR=http://127.0.0.1:8200
VAULT_TOKEN=hvs.replace_me
VAULT_MOUNT=secret
VAULT_SECRET_PATH=ehub/production
ASPNETCORE_ENVIRONMENT=Production
EOF
sudo chmod 600 /opt/ehub/deploy.env
```

### 3) Prerequisites on server

- Docker
- `python3` (Vault JSON parse in deploy script)
- `curl`
- Vault + Postgres (+ Redis) already running

### 4) Manual smoke test

```bash
cd /opt/eHub-platform
bash scripts/deploy/remote-deploy.sh
curl -s http://127.0.0.1:8080/health
```

## GitHub secrets

Repo → **Settings → Secrets and variables → Actions**:

| Secret | Example |
|--------|---------|
| `DEPLOY_HOST` | `187.127.88.26` |
| `DEPLOY_USER` | `root` |
| `DEPLOY_SSH_KEY` | private key that can SSH as `DEPLOY_USER` |
| `DEPLOY_PORT` | `22` (optional) |
| `DEPLOY_PATH` | `/opt/eHub-platform` (optional) |

Also create GitHub **Environment** named `production` (workflow references it) — optional approval gate.

## Flow

```text
git push origin main
  → Deploy workflow: test job
  → deploy job (SSH)
      → remote-deploy.sh (migrate + rebuild + restart)
```

Manual run: Actions → **Deploy** → **Run workflow**.
