# Repository Guidelines

## Project Structure & Module Organization

- Source in `src/` organized by host and capability.
    - Hosts: `VirtueRn.Web.Host`, `VirtueRn.Identity.Web.Host`, `VirtueRn.App.Host`, background workers in
      `src/pollers/` and `src/receivers/`.
    - Core libraries: `VirtueRn.Core`, `VirtueRn.Domain*`, `VirtueRn.Identity.*`, `VirtueRn.Messaging*`, plus
      `services/`, `common/`, `plugins/`, `utilities/`.
- Tests mirror source in `tests/` with `.Tests` suffix (e.g., `tests/VirtueRn.App.Tests`).
- Ops: Dockerfiles per host, `docker-compose.yml`, Kubernetes templates (`kube-template-*.yml`), SQL in `db/` and
  scripts in `scripts/`.

## Build, Test, and Development Commands

- Restore: `dotnet restore VirtueRNext-Core.sln` — restore all projects.
- Build: `dotnet build VirtueRNext-Core.sln -c Debug` — compile (.NET 8).
- Test (coverage): `dotnet test --settings coverlet.runsettings -c Release --results-directory TestResults`.
- Run Web Host: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/VirtueRn.Web.Host`.
- Run Identity Host: `dotnet run --project src/VirtueRn.Identity.Web.Host`.
- Run worker (example): `dotnet run --project src/pollers/VirtueRn.Visits.Poller`.

## Coding Style & Naming Conventions

- C#/.NET 8 with nullable and implicit usings enabled.
- Indentation 4 spaces; one class per file; `using` directives at top.
- Naming: PascalCase (types/methods), camelCase (locals/params), interfaces prefixed with `I`, async methods end with
  `Async`.
- Architecture boundaries: Domain, Application, Infrastructure, Hosts; prefer DI/constructor injection.
- Formatting: run `dotnet format` before pushing.

## Testing Guidelines

- Frameworks: NUnit + FluentAssertions + Moq; coverage by `coverlet.collector`.
- Structure: mirror namespaces in `tests/<Project>.Tests`.
- Naming: `MethodName_ShouldExpectedBehavior_WhenCondition`.
- Run all tests: `dotnet test -c Release`.

## Commit & Pull Request Guidelines

- Commits: small, imperative (e.g., `Web.Host: Fix null tenant check`). Avoid vague messages.
- Branching: feature branches from `development`; rebase or merge frequently.
- PRs: clear description, linked issues/PRs, affected projects, test evidence (logs/screenshots), and any config notes.
- CI: PRs must build cleanly and pass `dotnet test`.

## Security & Configuration Tips

- Local DB: `docker-compose up -d` starts MySQL on `localhost:3306`.
- Configuration: use `appsettings.json` per host/worker; do not commit secrets. Use `swap-secrets.ps1` and environment
  variables.
- Kubernetes: see `kube-template-*.yml` for required env vars and ports.

## Coverage

- Filters: `coverlet.runsettings` includes `VirtueRn.*`, excludes tests, migrations, and `*DbContext*`.
- Optional HTML report: after tests,
  `reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:Coverage -reporttypes:HtmlSummary`.

