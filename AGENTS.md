# Repository Guidelines

## Project Structure & Module Organization

- Source in `src/` organized by host and capability.
    - Hosts: `Records.App.Server` and `Records.App.Infrastructure.Security`.
    - Modules under `src/Modules/{ModuleName}` (e.g., `Core`, `Tenants`, `Users`, `Recordsets`, `Notifications`).
    - Each module follows: Domain, Application, Contracts, Infrastructure.Sql, Tests.
- Docs in `docs/` and included in `Records.sln` as solution items.

## Build, Test, and Development Commands

- Restore: `dotnet restore Records.sln`
- Build: `dotnet build Records.sln -c Debug`
- Test: `dotnet test Records.sln -c Release`
- Run Host (when available): `dotnet run --project src/Records.App.Server`
- Migration reset workflow: prefer `pwsh ./ef-reset.ps1` instead of hand-editing `Migrations/` files.
- Treat `Migrations/` as generated output; after reset, app startup seed logic in
  `src/Records.App.Server/SeedData.cs` restores baseline data for local/dev use.

## Coding Style & Naming Conventions

- C#/.NET 9 with nullable and implicit usings enabled.
- Indentation 4 spaces; one class per file; `using` directives at top.
- Naming: PascalCase (types/methods), camelCase (locals/params), interfaces prefixed with `I`, async methods end with
  `Async`.
- EF Core entity classes in `.Infrastructure.Sql` projects end with `Db` (tables unchanged).
- Architecture boundaries: Domain, Application, Contracts, Infrastructure, Hosts; prefer DI/constructor injection.

## Testing Guidelines

- Frameworks: follow module conventions (keep consistent within a module).
- Naming: `MethodName_ShouldExpectedBehavior_WhenCondition`.
- Run all tests: `dotnet test Records.sln -c Release`.

## Commit & Pull Request Guidelines

- Commits: small, imperative (e.g., `Core: Rename event store contract`).
- PRs: describe scope, affected modules, and test evidence.
