# Deployment Baseline

This repo includes first-pass deployment templates for local/dev and cluster environments.

## Local Container Stack

- File: `docker-compose.yml`
- Service: `mysql` (`mysql:8.4`)
- Service: `records-app` (built from `Dockerfile`)

### Bring Up

1. Run module migrations (recommended) from repo root: `./ef-reset.ps1 -QuickReset`
2. Start containers: `docker compose up --build`
3. Open app/API: `http://localhost:8080`
4. Open health endpoint: `http://localhost:8080/health`
5. Open metrics endpoint: `http://localhost:8080/metrics`

## Development Seed Bootstrap

- Config file: `src/Records.App.Server/appsettings.Development.json`
- Toggle: set `DevelopmentSeed.Enabled` to `true` for one local bootstrap run.
- Behavior: ensures a global admin user exists.
- Behavior: grants `GlobalAdmin` membership if missing.
- Behavior: creates the configured seed tenant if missing.
- Behavior: writes user/tenant projection rows.
- After bootstrap, set `DevelopmentSeed.Enabled` back to `false`.

## Kubernetes Templates

- File: `deploy/k8s/mysql.yaml`
- File: `deploy/k8s/records-app.yaml`

These are bootstrap templates only:

- Replace `ghcr.io/your-org/records-app:latest` with your image.
- Replace inline secret values with your secret management process.
- Apply manifest: `kubectl apply -f deploy/k8s/mysql.yaml`
- Apply manifest: `kubectl apply -f deploy/k8s/records-app.yaml`

## CI

- File: `.github/workflows/ci.yml`
- Trigger: pull requests and pushes to `main`
- Step: `dotnet restore`
- Step: `dotnet build` (Release)
- Step: `dotnet test` (Release)
