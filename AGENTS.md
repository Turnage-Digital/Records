# Repository Guidelines

## Project Structure & Module Organization

- Source in `src/` organized by host and capability.
    - Hosts: `Records.App.Server` and `Records.App.Infrastructure.Security`.
    - Modules under `src/Modules/{ModuleName}` (e.g., `Core`, `Tenants`, `Users`, `Recordsets`, `Notifications`).
    - Feature modules follow: Domain, Application, Contracts, Infrastructure.Sql, Presentation, Tests.
    - `Core` is the shared base exception and does not define a `Presentation` project.
- Docs in `docs/` and included in `Records.sln` as solution items.

### Module Project Layout Standards

- `Records.{Module}.Domain`
    - Keep `Events/` and `ValueObjects/` only (flat, no nested subfolders).
    - Keep domain entities, enums, repository interfaces, and module unit-of-work interface at project root.
    - `Notifications.Domain` may also keep `Services/`.

- `Records.{Module}.Application`
    - Keep `Commands/`, `EventHandlers/`, and `Queries/` (omit `Queries/` if unused).
    - `Core.Application` may also keep `Behaviors/`.
    - `Commands/` and `Queries/` are flat (no subfolders).
    - Each command/query file is named by command/query type (for example, `CreateRecordCommand.cs`) and contains both
      request and handler.

- `Records.{Module}.Contracts`
    - Keep only `Dtos/`, `IntegrationEvents/`, `Projections/`, and `Queries/`.
    - Place all other contracts at project root.

- `Records.{Module}.Infrastructure.Sql`
    - Keep only `Entities/`, `Mappers/`, `Migrations/`, and `QueryCriteria/`.
    - Place repositories, queries, projection writers, and unit of work at project root.
    - Namespace must match directory path.

- `Records.{Module}.Presentation`
    - Controllers under `Controllers/` only.

- `Records.{Module}.Tests`
    - Organize by module conventions; keep naming and coverage consistent with neighboring module tests.

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
- Indentation 4 spaces; `using` directives at top.
- Keep one primary concern per file, with two explicit exceptions:
    - Application command/query files contain both request and handler.
    - Some Contracts files intentionally group closely related DTO/projection records.
- Naming: PascalCase (types/methods), camelCase (locals/params), interfaces prefixed with `I`, async methods end with
  `Async`.
- EF Core entity classes in `.Infrastructure.Sql` projects end with `Db` (tables unchanged).
- Architecture boundaries: Domain, Application, Contracts, Infrastructure, Hosts; prefer DI/constructor injection.

## Records.Client Frontend Conventions

- `src/Records.Client/src` imports should target the exact source file they use; do not add or rely on barrel
  `index.ts` files under `components/`, `models/`, or `pages/`.
- Keep single-file UI components as single files. Create a component directory only when the component owns multiple
  implementation files or a small internal surface area (for example, `app-sidebar/`, `detail-panel/`, or
  `recordset-editor/`).
- `app-sidebar/` is the primary left-side application navigation surface. Do not use that area for contextual editors
  or record detail workflows.
- `detail-panel/` is the right-side contextual workspace for tangential tasks such as notifications, history, and
  secondary editors launched from the current page.
- Prefer descriptive props/type names in shared client components (`RecordCardProps`, `DetailPanelHeaderProps`) rather
  than generic `Props` names when the type is exported or the component is reused.

## Testing Guidelines

- Frameworks: follow module conventions (keep consistent within a module).
- Naming: `MethodName_ShouldExpectedBehavior_WhenCondition`.
- Run all tests: `dotnet test Records.sln -c Release`.

## Commit & Pull Request Guidelines

- Commits: small, imperative (e.g., `Core: Rename event store contract`).
- PRs: describe scope, affected modules, and test evidence.
