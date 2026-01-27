4# Records Port / Upgrade Plan

This plan tracks the staged migration from Lister to Records with a Holmes-style DDD/CQRS architecture.

## Decisions Locked In

- Solution name: `Records.sln`
- Bounded contexts: `Tenants`, `Users`, `Recordsets`, `Notifications`, `Clocks`
- Auth: ASP.NET Identity with minimal Identity API mapping
- Tenant provisioning: Admin-created tenants (command-based)
- Drop Lister-style OpenAI infrastructure from Core

## Stage 0 — Bootstrap (Now)

- [x] Create `Records` repo + `Records.sln`
- [x] Port Holmes Core → `Records.Core.*`
- [x] Scaffold `Records.App.Server` (Identity API mapping + host composition root)
- [x] Add `Records.App.Infrastructure.Security` (auth wiring only, no encryption service)

## Stage 1 — Tenants Module (New)

- [x] Create `Records.Tenants.Domain`
- [x] Create `Records.Tenants.Contracts`
- [x] Create `Records.Tenants.Application`
- [x] Create `Records.Tenants.Infrastructure.Sql`
- [x] Create `Records.Tenants.Tests`
- [x] Define Tenant aggregate + Admin provisioning commands
- [ ] Integration events for tenant lifecycle (Created/Disabled/etc.)
- [x] Add tenant queries + controller + projection writer

## Stage 2 — Users Module (External Identity)

- [x] Create `Records.Users.Domain`
- [x] Create `Records.Users.Contracts`
- [x] Create `Records.Users.Application`
- [x] Create `Records.Users.Infrastructure.Sql`
- [x] Create `Records.Users.Tests`
- [x] Model User ↔ Tenant membership + roles
- [x] Map ASP.NET Identity user entity to domain user model
- [x] Define user provisioning and membership commands
- [x] Add users queries + controller + projections

## Stage 3 — Recordsets Module (Port of Lists)

- [ ] Create `Records.Recordsets.Domain`
- [ ] Create `Records.Recordsets.Contracts`
- [ ] Create `Records.Recordsets.Application`
- [ ] Create `Records.Recordsets.Infrastructure.Sql`
- [ ] Create `Records.Recordsets.Tests`
- [ ] Port Lister list/inventory concepts to Recordsets
- [ ] Rewrite aggregates to Holmes-style DDD/CQRS (no manager aggregates)

## Stage 4 — Notifications Module

- [ ] Create `Records.Notifications.Domain`
- [ ] Create `Records.Notifications.Contracts`
- [ ] Create `Records.Notifications.Application`
- [ ] Create `Records.Notifications.Infrastructure.Sql`
- [ ] Create `Records.Notifications.Tests`
- [ ] Rework notification flows around integration events

## Stage 5 — Clocks Module (Port of Holmes SlaClocks)

- [ ] Create `Records.Clocks.Domain`
- [ ] Create `Records.Clocks.Contracts`
- [ ] Create `Records.Clocks.Application`
- [ ] Create `Records.Clocks.Infrastructure.Sql`
- [ ] Create `Records.Clocks.Tests`
- [ ] Port Holmes `SlaClocks` domain concepts into `Clocks`
- [ ] Integrate with Records outbox + projections

## Stage 6 — Cross-Cutting

- [ ] Integrations: outbox, projections, SSE (as needed)
- [ ] Enforce controller/command rules (one command per write endpoint)
- [ ] Module integration event map
- [ ] Read model strategy per module
