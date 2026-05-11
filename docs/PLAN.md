# Records Port / Upgrade Plan

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
- [ ] Integration events for tenant lifecycle (deferred for now)
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

- [x] Create `Records.Recordsets.Domain`
- [x] Create `Records.Recordsets.Contracts`
- [x] Create `Records.Recordsets.Application`
- [x] Create `Records.Recordsets.Infrastructure.Sql`
- [x] Create `Records.Recordsets.Tests`
- [x] Port Lister list/inventory concepts to Recordsets
- [x] Rewrite aggregates to Holmes-style DDD/CQRS (no manager aggregates)

## Stage 4 — Notifications Module

- [x] Create `Records.Notifications.Domain`
- [x] Create `Records.Notifications.Contracts`
- [x] Create `Records.Notifications.Application`
- [x] Create `Records.Notifications.Infrastructure.Sql`
- [x] Create `Records.Notifications.Tests`
- [x] Add `Records.Notifications.Presentation`
- [x] Add notification rules (CRUD + queries)
- [x] Add notifications list/unread endpoints (Lister-style API surface)
- [x] Rework notification flows around integration events

## Stage 5 — Clocks Module (Port of Holmes SlaClocks)

- [x] Create `Records.Clocks.Domain`
- [x] Create `Records.Clocks.Contracts`
- [x] Create `Records.Clocks.Application`
- [x] Create `Records.Clocks.Infrastructure.Sql`
- [x] Create `Records.Clocks.Tests`
- [x] Define clock definitions + record clocks (tenant-defined kinds + thresholds)
- [x] Add business calendar service + watchdog
- [x] Integrate with Records outbox + projections

## Stage 6 — Cross-Cutting

- [x] Integrations: outbox, projections, SSE (as needed)
- [x] Enforce controller/command rules (one command per write endpoint)
- [x] Module integration event map
- [x] Read model strategy per module

## Stage 7 — API Parity (Backend)

- [x] Keep canonical API as `/api/recordsets/*` (no `/api/lists/*` compatibility alias)
- [x] List history + item history endpoints
- [x] Paged list items with sort (`page`, `pageSize`, `field`, `sort`)
- [x] Add recordset equivalents for names + definition metadata
- [x] Query-options migration: update `Lister/src/Lister.Client/src/query-options.ts` calls to `/api/recordsets/*`

## Stage 8 — Frontend/UI Port (React)

- [x] Port base client app shell + routing
- [x] Recordset setup flow uses in-context detail panels for notifications + clock definitions
- [x] Recordset page exposes scoped notifications drawer (query + unread + mark-all within recordset context)
- [x] Recordset page supports URL-backed status filtering with server-side query filtering
- [x] Lists/Recordsets UI parity (list view, record view, filters, schema editor)
- [x] Notifications UI parity (list, detail, rules)
- [x] Migrations UI parity (plan + progress)
- [x] Identity + tenant admin screens
- [x] Clocks UI baseline (clock definitions + record clock actions)

## Stage 9 — Hardening & Ops

- [x] Auth policies + tenant scoping (global admins vs tenant admins vs ops)
- [x] Background jobs reliability + retries
- [x] Observability (logging, metrics, tracing)
- [x] Data migrations / seed scripts (`ef-reset.ps1` + opt-in dev seed bootstrap)
- [x] Deployment templates (docker-compose, K8s, CI)

## Stage 10 — Release Readiness

- [x] Add hosted-service failure-injection coverage (`DeferredDispatchProcessor`, `NotificationProcessingService`)
- [x] Fill `Records.Notifications.Tests` with real reliability tests
- [x] Add app-host integration smoke tests (`/health`, `/identity/logout`)
- [x] Add performance smoke harness for key API paths
- [x] Final release checklist + runbook pass
