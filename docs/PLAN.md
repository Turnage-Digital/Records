# Records Port / Upgrade Plan

This plan tracks the staged migration from Lister to Records with a Holmes-style DDD/CQRS architecture.

## Decisions Locked In

- Solution name: `Records.sln`
- Bounded contexts: `Tenants`, `Users`, `Recordsets`, `Notifications`
- Auth: ASP.NET Identity with minimal Identity API mapping
- Tenant provisioning: Admin-created tenants (command-based)
- Drop Lister-style OpenAI infrastructure from Core

## Stage 0 — Bootstrap (Now)

- [x] Create `Records` repo + `Records.sln`
- [x] Port Holmes Core → `Records.Core.*`
- [ ] Scaffold `Records.App.Server` (Identity API mapping + host composition root)
- [ ] Add `Records.App.Infrastructure.Security` (auth wiring only, no encryption service)

## Stage 1 — Tenants Module (New)

- [ ] Create `Records.Tenants.Domain`
- [ ] Create `Records.Tenants.Contracts`
- [ ] Create `Records.Tenants.Application`
- [ ] Create `Records.Tenants.Infrastructure.Sql`
- [ ] Create `Records.Tenants.Tests`
- [ ] Define Tenant aggregate + Admin provisioning commands
- [ ] Integration events for tenant lifecycle (Created/Disabled/etc.)

## Stage 2 — Users Module (External Identity)

- [ ] Create `Records.Users.Domain`
- [ ] Create `Records.Users.Contracts`
- [ ] Create `Records.Users.Application`
- [ ] Create `Records.Users.Infrastructure.Sql`
- [ ] Create `Records.Users.Tests`
- [ ] Model User ↔ Tenant membership + roles
- [ ] Map ASP.NET Identity user entity to domain user model
- [ ] Define user provisioning and membership commands

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

## Stage 5 — Cross-Cutting

- [ ] Integrations: outbox, projections, SSE (as needed)
- [ ] Enforce controller/command rules (one command per write endpoint)
- [ ] Module integration event map
- [ ] Read model strategy per module

