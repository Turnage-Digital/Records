# Records Architecture (Target State)

This document captures the intended target architecture for Records. It is forward-looking and may not match the
current repository in every detail.

---

## 1) Executive Summary

Records is a modular, event-driven platform for recordset management, notifications, and identity workflows.
The system is designed to keep bounded contexts isolated, enable independent evolution, and support near-real-time
read models for internal and external clients.

---

## 2) Module Architecture (Target)

Records is a .NET 9 **Modular Monolith** using **Clean Architecture** and **Domain-Driven Design**. Each bounded
context is a self-contained module under `src/Modules/{ModuleName}/` with the following project conventions:

1. `Records.{Module}.Domain`
    - Keep only `Events/` and `ValueObjects/` as domain subfolders (flat, no nested subfolders).
    - Keep aggregates/entities, enums, repository interfaces, and module unit-of-work interface at project root.
    - Exception: `Records.Notifications.Domain` may also keep `Services/`.

2. `Records.{Module}.Application`
    - Keep `Commands/`, `EventHandlers/`, and `Queries/` (`Queries/` may be omitted if unused).
    - `Commands/` and `Queries/` must be flat (no nested folders).
    - Command/query files are named by request type and contain both request and handler in the same file.
    - Exception: `Records.Core.Application` may also keep `Behaviors/`.

3. `Records.{Module}.Contracts`
    - DTOs under `Dtos/`
    - Integration event contracts under `IntegrationEvents/`
    - Projection writer interfaces under `Projections/`
    - Query interfaces under `Queries/` (e.g., `I{Entity}Queries.cs`)
    - All other contracts live at project root.

4. `Records.{Module}.Infrastructure.Sql`
    - `{Module}DbContext.cs`
    - `{Module}UnitOfWork.cs`
    - Database entities under `Entities/`
    - Optional mappers under `Mappers/`
    - Optional query criteria under `QueryCriteria/`
    - EF Migrations under `Migrations/`
    - All other infrastructure classes (repositories, queries, projections, services, jobs, event-store helpers) live at
      the project root
    - Folder policy: only `Entities/`, `Mappers/`, `Migrations/`, and `QueryCriteria/` are valid subfolders
    - Namespace policy: namespaces must match file paths exactly (`Records.{Module}.Infrastructure.Sql` for root files,
      `...Sql.Entities`, `...Sql.Mappers`, `...Sql.QueryCriteria`, `...Sql.Migrations` for foldered files)
    - `DependencyInjection.cs` for service registration

5. `Records.{Module}.Presentation`
    - HTTP controllers under `Controllers/`
    - API-only concerns (routing, auth attributes, request/response mapping)
    - No domain persistence logic

6. `Records.{Module}.Tests`
    - Unit tests for the module

`Core` is the shared base module and is the current exception: it does not define a `Presentation` project.

Dependency graph:

```
Presentation ────► Application ──────► Contracts ◄────── Infrastructure.Sql
       │                   │                        │                              │
       └───────────────────└────────► Domain ◄──────┴──────────────────────────────┘
```

- Domain has no dependencies (pure domain logic)
- Contracts depends on Domain (for value objects in DTOs)
- Application depends on Domain and Contracts
- Presentation depends on Application and Contracts
- Infrastructure.Sql depends on Domain and Contracts (NOT Application)

---

## 2.1) EF Core Naming

- EF Core entity classes in `.Infrastructure.Sql` projects end with `Db`.
- Table names remain unchanged by class renames.
- `DbContext` should expose `DbSet<*Db>` and map persistence models from `Entities/`; do not persist Domain entities
  directly in EF models.

## 2.2) Unit of Work Convention

- Every module must define a concrete `{Module}UnitOfWork` in its `.Infrastructure.Sql` project.
- Do not fold unit-of-work responsibilities into repository class naming; keep the unit-of-work type explicit and
  discoverable.

## 2.3) Migration Reset Workflow

- `Migrations/` is treated as generated infrastructure output, not a hand-maintained source of truth.
- Prefer resetting and regenerating via `pwsh ./ef-reset.ps1` over manual migration surgery.
- After migration reset, starting `Records.App.Server` should repopulate baseline development data through
  `src/Records.App.Server/SeedData.cs`.

## 2.4) Module Composition and Portability

- Modules are designed to be **portable building blocks**. A Records variant (e.g., "Records for Background Checks")
  should be achievable by composing a different set of modules (e.g., `Orders`, `Services`) and omitting others
  (e.g., dynamic `Recordsets`), with minimal glue code in the app host.
- Each module must expose a single `Add{Module}()` registration extension (services + data access) and a single
  `Map{Module}()` extension (HTTP endpoints). The host composes the product by calling these.
- Modules must declare dependencies only through **Contracts**; cross-module calls are via Contracts interfaces or
  integration events. No hidden or "backchannel" dependencies are allowed.
- Module-owned schemas, migrations, and background services stay inside the module. The host only wires them up.
- Aggregate roots are the default write-model shape, but `Users` is an intentional exception while it remains
  ASP.NET Identity-backed. Exceptions must be explicit and documented, not accidental.

## 3) Bounded Contexts (Initial)

- Recordsets
- Notifications
- Users
- Tenants
- Clocks

---

## 4) Cross-Module Integration (Rules)

- Allowed references: `ModuleA.Application` → `ModuleB.Contracts` and
  `ModuleA.Infrastructure.Sql` → `ModuleB.Contracts`.
- Forbidden references: direct dependencies on another module's Domain, Application, or Infrastructure.Sql.
- Cross-module integration handlers live in the consuming module's Application and only reference the
  producer's Contracts.
- Integration event contracts live in the producer's `Contracts/IntegrationEvents/`.
- Integration event publishers and consumers live in each module's `Application/EventHandlers/`.
- No cross-module transactions; use outbox + integration events between modules.

---

## 5) Runtime Hosts and App Projects

- `Records.App.Server` (API surface, background services, integration wiring)
- `Records.App.Infrastructure.Security` (host security/identity wiring and policies)

## 5.1) API Conventions

- Write endpoints map to a single command (CQRS). No direct database writes in controllers.
- `POST` returns `201 Created` with a Location header and the created representation (or identifier payload).
- `PUT`/`PATCH`/state change commands return `204 NoContent` on success.
- Read endpoints use query services and return `200 OK` with DTOs.

---

## 6) Eventing, Unit of Work, and Outbox

- Aggregates emit domain events and are persisted with their `EventRecord` in one transaction.
- Domain events are dispatched after commit via MediatR.
- Deferred dispatch / outbox processing is the mechanism for cross-module and external integration events.

## 6.1) Audit and Aggregate State

- The event store is the source of truth for action audit (`ActorId`, event timestamp, correlation, causation).
- Aggregate roots and domain entities should not carry generic audit fields by default.
- `CreatedBy`, `CreatedAt`, `UpdatedBy`, and `UpdatedAt` are banned from aggregate/domain state unless a documented
  exception proves they are required for current business behavior.
- If the UI or API needs audit fields, project them from events into read models rather than storing them flat on
  aggregates.
- Keep only current-state fields on aggregates: values needed for invariants, transitions, time calculations, or future
  domain decisions.
- Prefer explicit state names like `StartedAt`, `CompletedAt`, or `ReadAt` over generic `UpdatedAt`.
- See `docs/AUDIT_POLICY.md` for the aggregate-by-aggregate plan and exception rules.
