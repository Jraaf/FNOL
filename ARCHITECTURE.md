# Architecture

## Solution structure

```
backend/src/
├── ClaimsModule.Domain         (no project refs)
├── ClaimsModule.Application    (-> Domain)
├── ClaimsModule.Persistence    (-> Domain, Application)
├── ClaimsModule.Infrastructure (-> Domain, Application)
└── ClaimsModule.API            (-> all four)
```

Reference direction matches Clean Architecture: the inner layers know
nothing of the outer. The **Domain** layer has zero NuGet dependencies on
ASP.NET, EF Core, MediatR, or Azure — it expresses the business invariants
in pure C#.

Cross-cutting interfaces (`IClaimsDbContext`, `IUnitOfWork`,
`IStorageService`, `IBackgroundJobScheduler`, `IPolicyService`,
`IClaimNumberGenerator`, `ICurrentUser`, `IDateTimeProvider`,
`IDomainEventDispatcher`) live in
`ClaimsModule.Application/Common/Interfaces`. Persistence and
Infrastructure implement them; the API layer wires them up in
`Program.cs`.

## Data model

| Entity | Type | Purpose |
|--------|------|---------|
| `Claim` | Aggregate root | The claim itself, owns the workflow + child collections |
| `LossEvent` | Owned (1:1 with Claim) | Loss date / location / description |
| `ClaimParty` | Child | Claimants, witnesses, third parties (BR-C-03 enforced) |
| `ClaimRiskObject` | Child | Insured assets and damage estimates |
| `ClaimReserveComponent` | Aggregate root | One reserve line per coverage type; rich behavior |
| `ReserveHistory` | Child of reserve | Append-only audit trail of amount changes |
| `ClaimDocument` | Child | Blob reference + metadata |
| `ClaimAuditLog` | Child (append-only) | Immutable per-claim event log |
| `CauseOfLossCode` | Reference | Lookup table; tenant-scoped |
| `ClaimStatusTransition` | Reference | Allowed FSM transitions and required permissions |

Two aggregate roots intentionally: `Claim` owns intake, status workflow,
parties, documents, audit log, and the reserve collection. The
`ClaimReserveComponent` is also marked as an aggregate root because the
authority/approval state machine is meaningful by itself and acts as a
concurrency boundary (`RowVer` on both). Reserves are mutated only through
methods on `Claim` (`OpenReserve`, `AdjustReserve`, `ApproveReserve`,
`RejectReserve`) so the parent invariants (BR-R-07 total cap) stay
consistent.

### Conventions enforced (per FRS / Appendix A.4)

- `UNIQUEIDENTIFIER` PKs with `NEWSEQUENTIALID()` default.
- `DECIMAL(19,4)` for all monetary amounts.
- `DATETIMEOFFSET(7)` for all timestamps.
- Soft delete via `IsDeleted` + `DeletedAt`, enforced by `ClaimsDbContext` (intercepts `EntityState.Deleted` and turns it into a flag update) plus a global query filter.
- Audit columns `CreatedAt / UpdatedAt / UserCreated / UserModified` on every entity, populated by `ClaimsDbContext.ApplyAuditAndTenant()`.
- Tenant isolation via `OrganizationEntityId` and a global query filter that uses the current user's organization.
- `RowVer` (ROWVERSION) optimistic concurrency on aggregate roots (`Claim`, `ClaimReserveComponent`).
- All schema is produced by EF Core migrations — there is no raw SQL at runtime aside from `NEXT VALUE FOR dbo.ClaimNumberSequence`.

## CQRS flow

```
HTTP -> Controller -> IMediator.Send -> PipelineBehavior(Logging) ->
        PipelineBehavior(Validation) -> RequestHandler ->
        Domain method (raises events) -> IUnitOfWork.SaveChangesAsync ->
        IDomainEventDispatcher.DispatchAsync -> MediatR Publish
```

- Every state-changing operation is a `Command` (`CreateClaimCommand`,
  `OpenReserveCommand`, `ApproveReserveCommand`, …). Every read is a
  `Query` (`ListClaimsQuery`, `GetClaimDetailQuery`, …).
- `ValidationBehavior<TRequest, TResponse>` is registered as an open
  MediatR pipeline behavior — every command runs **all** matching
  `IValidator<TRequest>`s before its handler executes, so FluentValidation
  errors short-circuit at the MediatR pipeline level (not just the
  controller).
- `IUnitOfWork` is implemented by `UnitOfWork` in `Persistence`. It
  collects pending domain events from tracked aggregate roots, saves
  the EF Core change set, and dispatches the events afterwards. This is
  the **transactional-outbox-lite** pattern: events are only published
  after persistence succeeds.
- The dispatcher converts each `IDomainEvent` into a
  `DomainEventNotification` and publishes it through `MediatR.IPublisher`,
  allowing handlers to live anywhere in the Application layer.

## Domain events

| Event | Raised when | Default handler outcome |
|-------|-------------|-------------------------|
| `ClaimCreatedEvent` | `Claim.CreateFnol` | Audit log entry via the entity, plus available for external subscribers |
| `ClaimStatusChangedEvent` | `Claim.TransitionStatus` | Status-change audit entry |
| `ReserveOpenedEvent` | `ClaimReserveComponent.Open` | Opens audit trail; enqueues GL posting if auto-approved |
| `ReserveAdjustedEvent` | `ClaimReserveComponent.Adjust` | History entry + GL posting if new amount auto-approves |
| `ReserveApprovedEvent` | `ClaimReserveComponent.Approve` | Audit entry + GL posting enqueue |
| `ReserveRejectedEvent` | `ClaimReserveComponent.Reject` | Audit entry with reason |
| `DocumentUploadedEvent` | `Claim.AddDocument` | Audit entry |
| `SlaBreachDetectedEvent` | `Claim.MarkSlaBreached` (Hangfire) | SLA_BREACH_DETECTED audit entry |

Audit log entries are added eagerly inside the aggregate so they sit in
the same transaction as the state change, guaranteeing audit/state
consistency. The MediatR events stay available for cross-cutting
extensions (notifications, integration events, etc.).

## Business rule mapping

| Rule | Enforcement point |
|------|-------------------|
| BR-C-01 (no future loss date) | `Claim.CreateFnol` + `CreateClaimValidator` |
| BR-C-02 (loss date in policy period) | `CreateClaimHandler` (warning only — audit `LOSS_DATE_OUTSIDE_POLICY_PERIOD`) |
| BR-C-03 (at least one Claimant) | `CreateClaimValidator` + `Claim.EnsureHasClaimant()` |
| BR-C-04 (claim number format & uniqueness) | `ClaimNumberGenerator` (`NEXT VALUE FOR dbo.ClaimNumberSequence`) + unique index on `(OrganizationEntityId, ClaimNumber)` |
| BR-C-05 (cause code exists & active) | `CreateClaimValidator.MustAsync` |
| BR-C-06 (allowed status transitions) | `Claim.TransitionStatus` checks against the seeded `ClaimStatusTransitions` table |
| BR-R-01 (amount > 0) | Validator + domain constructor / adjust |
| BR-R-02..04 (authority bands) | `ClaimReserveComponent.ResolveApprovalStatus` (≤10K auto, ≤100K supervisor, >100K manager) |
| BR-R-05 (idempotent GL posting) | Key format `Reserve:{ReserveId:N}:Change:{ChangeSequence}`; `GlPostingJob` checks the audit log for the key before posting and is also re-entrant safe |
| BR-R-06 (re-submit after rejection) | A rejected reserve cannot be adjusted (`DomainException` `RESERVE_REJECTED`) — callers open a new reserve, original stays in history |
| BR-R-07 (total cap) | `Claim.EnforceTotalReserveLimit` raises an audit warning when approved totals exceed $10M without `ManagerOverrideFlag` |

## Hangfire job design

### GL posting (`GlPostingJob`)

- Triggered by `IBackgroundJobScheduler.EnqueueGlPosting(claimId, reserveId, changeSequence, idempotencyKey)`.
- Idempotency strategy: before posting, query the audit log for any prior
  `GL_POSTING_SIMULATED` entry containing the idempotency key in its
  `NewValues` column. If found, no-op. This makes a retry produce no
  duplicates.
- Also guards against staleness: if the reserve's `ChangeSequence` has
  advanced (a newer adjustment happened) or the reserve is no longer
  approved, the job logs and exits. This handles races where multiple
  changes are queued faster than they run.
- Configured with `[AutomaticRetry(Attempts = 3, DelaysInSeconds = [10,30,60])]`.
- Posts a structured audit entry simulating the journal:
  `DR Change in Outstanding Reserves / CR Outstanding Loss Reserves`.

### SLA monitor (`SlaMonitorJob`)

- Registered as a Hangfire recurring job with cron `*/15 * * * *`.
- Scans claims in `Draft` or `Open` whose `LastTouchedAt` is older than 48h
  and flips them to `SlaBreached` via the aggregate (which emits the audit
  entry and `SlaBreachDetectedEvent`).

## Storage abstraction

`IStorageService` has two implementations behind `Storage:Provider`:

- `AzureBlobStorageService` — uploads to a container with the path
  `{organizationId:N}/{claimId:N}/{guid}-{fileName}`, returns SAS URLs
  with a 1-hour TTL (`BlobSasBuilder` + the connection string's account
  key).
- `LocalFileSystemStorageService` — same path layout under
  `App_Data/uploads`, returns `file://` URIs (or a configurable
  `PublicBaseUrl` so the SPA can render a real download link in
  developer setups).

The `UploadDocumentHandler` stays storage-agnostic; switching providers
is one `appsettings.json` line.

## API surface

- ASP.NET Core controllers, OpenAPI via Swashbuckle, error responses as
  `application/problem+json` produced by `ErrorHandlingMiddleware`.
- Domain exceptions become 422 with the rule code (e.g. `BR-R-01`).
  `ValidationException` becomes 400 with the per-property error map.
  `NotFoundException` → 404. `ForbiddenException` → 403.
- The mock JWT middleware accepts both real-ish unsigned JWTs (decoded
  with `JwtSecurityTokenHandler`) and the `X-Mock-*` headers, so
  Postman/Swagger UI work without authentication boilerplate.

## Azure topology

Recommended minimal deployment (free / trial tier acceptable):

```
+--------------------------+         +----------------------------+
| Azure Static Web App     |  HTTPS  | Azure App Service (Linux)  |
| Angular bundle (CDN)     +-------->+ ClaimsModule.API + Hangfire|
+--------------------------+         +-------------+--------------+
                                                   |
                                  +----------------+----------------+
                                  |                                 |
                            +-----v------+                  +-------v--------+
                            | Azure SQL  |                  | Azure Blob     |
                            | Database   |                  | Storage        |
                            | ClaimsModule|                 | claim-documents|
                            +------------+                  +----------------+
```

- **Azure App Service (Linux, .NET 9)** runs the API, the Hangfire server,
  and the recurring SLA monitor in the same process. For higher scale,
  split the Hangfire worker into a separate Web Job.
- **Azure SQL Database** hosts both the domain schema and the
  Hangfire schema (separate logical DB or same DB). Connection strings
  via App Service config / Key Vault references.
- **Azure Blob Storage** with one container `claim-documents`; SAS URLs
  for read access.
- **Azure Key Vault** (optional, but the secret-reference syntax is
  documented in `appsettings`).
- **Azure Static Web Apps** (or a second App Service) hosts the Angular
  bundle. CORS is allowed from the SWA hostname via `Cors:AllowedOrigins`.

## Key design decisions & trade-offs

| Decision | Why |
|----------|-----|
| Two aggregate roots (`Claim`, `ClaimReserveComponent`) | The reserve has its own concurrency boundary and state machine. Wrapping every adjustment in the `Claim` aggregate would require re-loading every reserve and history row on every approve/reject — too heavy for a hot path. |
| Audit log written inside the aggregate, not by an event handler | Guarantees audit and state mutation are atomic. Event handlers can still react asynchronously for integration concerns. |
| `IDomainEvent` is framework-free | Keeps the Domain project free of MediatR. The `MediatRDomainEventDispatcher` in Infrastructure wraps events in a `DomainEventNotification` so MediatR can publish them. |
| Idempotency key uses `ChangeSequence` | An adjust + approve in quick succession both enqueue jobs. The sequence number lets the job tell whether it's stale. |
| Local-FS storage fallback | The assessment requires graceful degradation when Azure Blob isn't available. `IStorageService` is the seam. |
| Mock auth via middleware (not full ASP.NET Authentication) | Keeps the assessment focused on the domain. Replacing with `AddAuthentication().AddJwtBearer()` is one method call. |
| AutoMapper pinned at 12.0.1 | The DI extensions package only supports 12.0.x; bumping to 13/14 needs a different bootstrap. Acknowledged vulnerability, documented in README. |
| AutoMapper used via `ProjectTo` for read queries | Pushes projections into SQL — keeps the list endpoint snappy without N+1. |
| Status transition matrix in the database | Lets operations adjust the workflow without redeploying. Also enables per-tenant overrides without code changes. |
