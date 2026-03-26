# Audit Field Policy

This document defines how Records models audit/history versus current business state.

## Core Rule

- The event store is the source of truth for who acted and when.
- Aggregate roots and domain entities do not get generic audit fields by default.
- `CreatedBy`, `CreatedAt`, `UpdatedBy`, and `UpdatedAt` are banned from aggregate/domain state.
- Read models may project audit/history fields from the event store when the UI or APIs need them.
- If a field exists on an aggregate, we must be able to defend it as current business state that affects behavior.

The canonical audit source is the event envelope and stored event metadata in
`src/Modules/Core/Records.Core.Contracts/IEventStore.cs`:

- `StoredEvent.CreatedAt`
- `StoredEvent.ActorId`
- `StoredEvent.CorrelationId`
- `StoredEvent.CausationId`

## Exception Test

Keep a field on the aggregate only if removing it would break one of these:

- state transitions
- time calculations
- idempotence or duplicate prevention
- future domain decisions based on current state

Actor identity has a higher bar than timestamps:

- keep `*By` only if who acted changes later domain behavior
- if actor identity is only needed for history, use the event store and projections

## Naming Rules For Allowed Exceptions

- Use explicit action names, never generic audit names.
- Use `At`, never `On`.
- Acceptable examples: `StartedAt`, `PausedAt`, `CompletedAt`, `ReadAt`, `DisabledAt`
- Avoid `*By` unless the aggregate truly depends on actor identity.
- Generic history DTOs should use names like `ActorId` and `OccurredAt`, not `By` and `On`.

## Command And Projection Rules

- Commands and controllers should not carry actor fields solely to stamp audit columns.
- Actor identity should come from request context and flow into the event envelope.
- If a grid or DTO needs "created", "last changed", or action history, project it from events.
- Generic recency belongs in read models only, under names like `LastChangedAt`.

## Aggregate Plans

### Recordset

Current problems:

- `Recordset` stores `CreatedBy`, `CreatedAt`, `UpdatedBy`, and `UpdatedAt`.
- child `Record` also stores flat audit fields
- queries and DTOs use row audit for history and sorting

Target:

- remove `CreatedBy`, `CreatedAt`, `UpdatedBy`, and `UpdatedAt` from `Recordset`
- remove `CreatedBy`, `CreatedAt`, `UpdatedBy`, and `UpdatedAt` from `Record`
- stop treating row audit as history
- if the UI needs recency, expose `LastChangedAt` from a projection only

Defended exceptions:

- none for generic audit

Implementation notes:

- migrate history screens to event-store-backed projections
- split vague "updated" behavior into explicit actions where the domain cares

### ClockDefinition

Current problems:

- `ClockDefinition` stores `CreatedBy`, `CreatedAt`, `UpdatedBy`, and `UpdatedAt`
- disable is currently smuggled through `UpdatedBy` and `UpdatedAt`

Target:

- remove `CreatedBy`, `CreatedAt`, `UpdatedBy`, and `UpdatedAt`
- keep `IsActive`
- add `DisabledAt` only if time-of-disable becomes part of business behavior

Defended exceptions:

- `IsActive`
- possibly `DisabledAt`, but only if the aggregate needs it for rules

Implementation notes:

- keep created/updated/disabled history in the event stream
- project definition audit into read models if needed by the API

### Clock

Current problems:

- clock operations currently carry actor fields through commands
- `CompletedBy` is stored flat even though the aggregate does not branch on actor identity

Target:

- remove `CompletedBy`
- do not add `StartedBy`, `PausedBy`, or `ResumedBy`
- keep operational timestamps and timers that drive state

Defended exceptions:

- `StartedAt`
- `AtRiskDueAt`
- `BreachDueAt`
- `AtRiskAt`
- `BreachedAt`
- `PausedAt`
- `CompletedAt`
- `PauseReason`
- `AccumulatedPauseTime`

Implementation notes:

- audit for start/pause/resume/complete should come from event metadata
- clock read models may project actor history separately from current state

### NotificationRule

Current problems:

- stores `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy`
- delete is currently encoded as an update
- actor ids are stored as `string`

Target:

- remove generic audit fields
- keep `IsActive` and `IsDeleted`
- clarify whether `UserId` means owner, target, or something else, and rename it if needed

Defended exceptions:

- `IsActive`
- `IsDeleted`
- the owner/target user reference, once it is named correctly

Implementation notes:

- delete history belongs in the event stream and projections
- no aggregate-level actor ids unless actor identity changes rule behavior

### Notification

Current problems:

- several timestamps existed mostly for history/read concerns
- current state and audit state were mixed together in the aggregate and write table

Target:

- keep only state that is needed for notification workflow
- push pure audit/history timestamps into projections

Defended exceptions:

- `ScheduledFor`
- `ReadAt`
- `Status`
- `DeliveryAttempts`

Likely removals unless a rule proves otherwise:

- `CreatedAt`
- `ProcessedAt`
- `DeliveredAt`

Implementation notes:

- queue, delivery, bounce, cancel, and read history should come from events
- inbox/read-model views can project whatever timestamps the API needs

Status:

- completed for aggregate state and write-table timestamps
- notification reads now use projections for timing/history concerns and the write row for content payload only

## Future Aggregate Work

### Tenant

Target:

- make `Tenant` inherit `AggregateRoot`
- emit `TenantCreated` and `TenantDisabled`
- persist tenant history through the event store
- keep only business state on the aggregate

Reason:

- tenants are a true domain concept and should not remain a special non-evented island

### Users

Target:

- either promote `User` into a real aggregate root or explicitly document why it remains an exception
- keep role and lifecycle actions explicit: invite, grant, revoke, suspend
- stop using generic audit fields as a fallback design

Reason:

- user lifecycle and role membership are domain actions, not just table mutations

## Phase Plan

### Phase 0

- write this policy into the repo
- ban new aggregate/domain `Created*` and `Updated*` fields
- ban new generic history DTO names like `By` and `On`

### Phase 1

- refactor `Clock` to remove actor fields from aggregate state
- decide whether `ClockDefinition` needs `DisabledAt`; otherwise remove generic audit there too
- start pushing audit columns to clock projections instead of aggregate tables

### Phase 2

- refactor `NotificationRule` and `Notification` under the same rule
- rename ambiguous ownership fields
- remove string-based actor audit from domain state

### Phase 3

- refactor `Recordset` and `Record`
- replace row-based history with event-backed projections
- remove generic audit from domain, persistence, commands, and DTOs

### Phase 4

- promote `Tenant` to `AggregateRoot`
- choose the long-term `Users` direction and apply the same policy there

## Progress

Completed so far:

- `Clock` no longer stores actor audit in aggregate state or command payloads.
- `Clock` actor history now flows through event-store metadata via `ITenantContext`.
- `ClockDefinition` no longer stores `CreatedBy`, `CreatedAt`, `UpdatedBy`, or `UpdatedAt` in aggregate state.
- `ClockDefinition` no longer stores generic audit columns in its write table.
- `ClockDefinition` reads now come from projections, and the DTO no longer exposes generic audit timestamps.
- browser payloads for clock operations and clock-definition operations no longer send audit actor/time fields that the server owns.
- `NotificationRule` no longer stores generic audit fields in aggregate state or its write table.
- `NotificationRule` create, update, and delete now emit explicit lifecycle events so audit lands in the event store.
- notification-rule commands and controllers no longer carry synthetic audit actor/time inputs.
- `NotificationsUnitOfWork` now persists notification-rule lifecycle events through the event store pipeline.
- `Notification` no longer stores `CreatedAt`, `ProcessedAt`, or `DeliveredAt` in aggregate state or its write table.
- notification inbox, detail, pending, retry, and history reads now use `NotificationProjectionDb` for timing/state concerns and use the write row only for content and recipient payload.
- mark-read no longer pushes actor identity through aggregate state or notification events; ownership is enforced at the command boundary from the current request user.
- notification query tests were updated to seed projection state explicitly, matching the new read-model contract.
- `Recordset` no longer stores `CreatedBy`, `CreatedAt`, `UpdatedBy`, or `UpdatedAt` in aggregate state.
- the `Recordsets` root write table no longer stores generic audit columns.
- recordset create and schema-update commands/controllers/browser payloads no longer carry synthetic audit actor/time fields.
- recordset summaries now use projection-only `LastChangedAt` instead of generic `UpdatedAt`.
- recordset root history now reads the event stream instead of reconstructing history from row-audit columns.
- recordset migration and seed paths were updated to use the new root contract without reintroducing generic audit.
- the local database and module migrations were regenerated with `ef-reset.ps1` after the `Clock`, `ClockDefinition`, `NotificationRule`, `Notification`, and `Recordset` cleanup.

Active next step:

- apply the same exception test to `Record`, especially row-audit-backed record history and record list DTO timestamps
