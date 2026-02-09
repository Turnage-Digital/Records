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
context is a self-contained module under `src/Modules/{ModuleName}/` with five projects:

1. `Records.{Module}.Domain`
    - Aggregate roots
    - Value objects under `ValueObjects/`
    - Domain events under `Events/`
    - Repository interfaces (`I{Entity}Repository.cs`)
    - Unit of Work interface (`I{Module}UnitOfWork.cs`)
    - Enums for entity states

2. `Records.{Module}.Application`
    - Commands under `Commands/` (CQRS command handlers)
    - Queries under `Queries/` (CQRS query handlers)
    - Event handlers under `EventHandlers/` for domain event projections and side effects

3. `Records.{Module}.Contracts`
    - DTOs under `Dtos/`
    - Query interfaces under `Queries/` (e.g., `I{Entity}Queries.cs`)
    - Projection writer interfaces under `Projections/`
    - Service interfaces under `Services/`
    - Notification/broadcaster interfaces under `Notifications/`

4. `Records.{Module}.Infrastructure.Sql`
    - `{Module}DbContext.cs`
    - `{Module}UnitOfWork.cs`
    - Database entities under `Entities/`
    - Repositories under `Repositories/` implementing domain interfaces
    - Query implementations under `Queries/` (implementing interfaces from Contracts)
    - Projections under `Projections/`
    - Mappers under `Mappers/`
    - Specifications under `Specifications/`
    - EF Migrations under `Migrations/`
    - `DependencyInjection.cs` for service registration

5. `Records.{Module}.Tests`
    - Unit tests for the module

Dependency graph:

```
Application ──────► Contracts ◄────── Infrastructure.Sql
     │                        │                              │
     └────────► Domain ◄──────┴──────────────────────────────┘
```

- Domain has no dependencies (pure domain logic)
- Contracts depends on Domain (for value objects in DTOs)
- Application depends on Domain and Contracts
- Infrastructure.Sql depends on Domain and Contracts (NOT Application)

---

## 2.1) EF Core Naming

- EF Core entity classes in `.Infrastructure.Sql` projects end with `Db`.
- Table names remain unchanged by class renames.

## 2.2) Module Composition and Portability

- Modules are designed to be **portable building blocks**. A Records variant (e.g., "Records for Background Checks")
  should be achievable by composing a different set of modules (e.g., `Orders`, `Services`) and omitting others
  (e.g., dynamic `Recordsets`), with minimal glue code in the app host.
- Each module must expose a single `Add{Module}()` registration extension (services + data access) and a single
  `Map{Module}()` extension (HTTP endpoints). The host composes the product by calling these.
- Modules must declare dependencies only through **Contracts**; cross-module calls are via Contracts interfaces or
  integration events. No hidden or "backchannel" dependencies are allowed.
- Module-owned schemas, migrations, and background services stay inside the module. The host only wires them up.

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
