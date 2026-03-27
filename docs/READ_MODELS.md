# Read Models

This document summarizes how each module exposes read models and how they are kept up to date.

## Recordsets

- Read models are served from SQL tables in `Records.Recordsets.Infrastructure.Sql`.
- `RecordsetProjectionDb` is updated by application commands after writes and now stores projection-only recency as
  `LastChangedAt`.
- recordset summary queries read from `RecordsetProjectionDb`.
- recordset root history reads from the event stream, not from the `Recordsets` write table.
- record data is still stored in `RecordDb` with JSON bags only; generic record audit columns are gone.
- record history reads from `RecordActivityDb`, which stores explicit action rows with actor/time metadata for record
  create and update operations.

## Notifications

- Read models are stored in `NotificationProjectionDb` and delivery attempt tables.
- `NotificationProjectionHandler` updates the projections from notification domain events.
- `INotificationQueries` uses `NotificationProjectionDb` for timing, status, paging, and history concerns, and loads
  content payload from the notification write row by id.
- `INotificationRuleQueries` reads notification-rule projections directly.

## Clocks

- Read models live in `ClockProjectionDb` and `ClockDefinitionProjectionDb`.
- Projections are updated by clock domain events.
- `ClocksEventProjectionRunner` can rebuild projections from the event store.

## Tenants

- read models are served from `TenantProjectionDb`.
- `TenantProjectionHandler` updates tenant projections from `TenantCreated` and `TenantDisabled` domain events.
- the tenant write table keeps only current business state; projection `CreatedAt` is projection-only data sourced from
  the create event.

## Users

- Read models are served directly from `UsersDbContext` tables.
- Commands write to the same tables; no projection layer yet.
- This is an intentional exception to the aggregate-root pattern while `User` remains ASP.NET Identity-backed.
- Keep lifecycle and role actions explicit, but do not reintroduce generic aggregate audit fields just to mimic the
  evented modules.

## Core Event Store

- Domain events for aggregate roots are persisted to the event store when the module uses the core UnitOfWork.
- Deferred dispatch/outbox is used for asynchronous event delivery.
