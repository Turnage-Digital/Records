# Read Models

This document summarizes how each module exposes read models and how they are kept up to date.

## Recordsets

- Read models are served from SQL tables in `Records.Recordsets.Infrastructure.Sql`.
- `RecordsetProjectionDb` is updated by application commands (projection writer) after writes.
- Record data is stored in `RecordDb` with JSON bags; queries read directly from that table.

## Notifications

- Read models are stored in `NotificationProjectionDb` and delivery attempt tables.
- `NotificationProjectionHandler` updates the projections from notification domain events.
- Queries read from projection tables via `INotificationQueries` and `INotificationRuleQueries`.

## Clocks

- Read models live in `ClockProjectionDb` and `ClockDefinitionProjectionDb`.
- Projections are updated by clock domain events.
- `ClocksEventProjectionRunner` can rebuild projections from the event store.

## Tenants

- Read models are served directly from `TenantsDbContext` tables.
- Commands write to the same tables; no projection layer yet.

## Users

- Read models are served directly from `UsersDbContext` tables.
- Commands write to the same tables; no projection layer yet.

## Core Event Store

- Domain events for aggregate roots are persisted to the event store when the module uses the core UnitOfWork.
- Deferred dispatch/outbox is used for asynchronous event delivery.
