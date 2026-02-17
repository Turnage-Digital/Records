# Release Checklist

Use this before promoting a Records build.

## 1) Build and test gate

- `dotnet build Records.sln`
- `dotnet test Records.sln`
- Confirm all tests pass, including:
- `Records.Notifications.Tests`
- `Records.App.Server.Tests`
- `Records.Architecture.Tests`

## 2) Data and migration gate

- Validate migration reset script locally:
- `./ef-reset.ps1 -QuickReset`
- Confirm no accidental migration drift in module SQL projects.
- Confirm seed settings are safe for target environment:
- `DevelopmentSeed.Enabled` must remain `false` outside local/dev bootstrap.

## 3) Runtime smoke gate

- Health check:
- `GET /health` returns `200`
- Metrics endpoint:
- `GET /metrics` returns `200`
- Identity route smoke:
- `POST /identity/logout` returns success
- Perf smoke baseline:
- `./scripts/perf-smoke.ps1 -BaseUrl "<target-base-url>" -Endpoints "/health","/metrics" -FailOnErrors`

## 4) Deployment artifact gate

- Container image builds from `Dockerfile`.
- CI workflow (`.github/workflows/ci.yml`) is green.
- Deployment manifests are reviewed for target environment:
- `docker-compose.yml`
- `deploy/k8s/mysql.yaml`
- `deploy/k8s/records-app.yaml`

## 5) Post-deploy verification

- Hosted services are running without repeated crash loops:
- Deferred dispatch processor
- Notification processing service
- Recordset migration dispatcher
- Clock watchdog service
- Confirm no elevated error rates in logs during first 10-15 minutes.
- Verify one end-to-end recordset flow and one notification flow.
