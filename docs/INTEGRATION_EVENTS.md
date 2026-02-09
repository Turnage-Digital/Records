# Integration Event Map

This document lists integration events and their current consumers. All events are defined under each module's
`Contracts/IntegrationEvents/` namespace.

## Recordsets

- `RecordsetCreatedIntegrationEvent`: recordsetId, createdBy, createdAt. Consumers: none yet.
- `RecordsetUpdatedIntegrationEvent`: recordsetId, updatedBy, updatedAt. Consumers: Notifications (list updated rules).
- `RecordsetDeletedIntegrationEvent`: recordsetId, deletedBy, deletedAt. Consumers: none yet.
- `RecordCreatedIntegrationEvent`: recordsetId, recordId, createdBy, createdAt. Consumers: Notifications (item created rules).
- `RecordUpdatedIntegrationEvent`: recordsetId, recordId, updatedBy, updatedAt, previousBag, newBag. Consumers:
  Notifications (item updated, status changed, column value changed, custom condition rules).
- `RecordDeletedIntegrationEvent`: recordsetId, recordId, deletedBy, deletedAt. Consumers: Notifications (item deleted rules).

## Clocks

- `ClockAtRiskIntegrationEvent`: clockId, tenantId, recordsetId, recordId, definitionId, clockName, atRiskAt, breachDueAt.
  Consumers: Notifications (clock at-risk rules).
- `ClockBreachedIntegrationEvent`: clockId, tenantId, recordsetId, recordId, definitionId, clockName, breachedAt, breachDueAt.
  Consumers: Notifications (clock breached rules).

## Notifications

- No outbound integration events yet.

## Tenants

- No integration events yet.

## Users

- No integration events yet.

## Dispatching

- Integration events are published by module event handlers (domain events → integration events).
- When `SaveChangesAsync(true, ...)` is used, domain events are deferred via the outbox and dispatched by
  `DeferredDispatchProcessor`, which then triggers integration events.
