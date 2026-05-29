# AI-Assisted Delivery Workflow

This document is the candidate's transparent log of how Claude (Claude Code
CLI, Opus 4.7) was used to deliver the assessment, where it added value,
and where it generated output that needed correction.

## Tools used

| Tool | Role |
|------|------|
| **Claude Code (CLI, Opus 4.7)** | Primary driver. Scaffolded the .NET solution end-to-end via `dotnet` and `ng` CLI commands, wrote Domain / Application / Infrastructure / API / Angular code, and produced this documentation. Operated with file-system + Bash tools. |
| **dotnet CLI** | `dotnet new sln/classlib/webapi`, `dotnet add reference/package`, `dotnet ef migrations add`. |
| **Angular CLI** | `ng new`, `ng build` validation. |
| **GitHub Copilot (in IDE)** | Used opportunistically during refinement passes — small completions, no architecture-level prompts. |

## Workflow phases

The work followed the phased approach suggested in §5.2 of the
assessment brief, with explicit checkpoints.

### Phase 1 — Architecture & domain design (Claude)

- Loaded the entire FRS excerpt (Section 3 of the brief) into Claude's
  context.
- Asked Claude to enumerate aggregate roots, value objects, and events.
  Claude proposed `Claim` as a single aggregate root. Rejected: the
  reserve's authority state machine deserves its own concurrency boundary
  and is mutated independently. Re-prompted; landed on
  **two aggregate roots** (`Claim`, `ClaimReserveComponent`) with
  reserves mutated through the parent's methods so BR-R-07 stays
  consistent.
- Hand-wrote the BR-* enforcement matrix (`ARCHITECTURE.md`) so the
  validators and domain code could be cross-referenced against it.

### Phase 2 — Database schema (Claude)

- Provided the FRS conventions table (Appendix A.4) and asked Claude to
  generate EF Core entity configurations.
- **Correction:** initial configurations omitted `RowVer` (ROWVERSION)
  on aggregate roots. Caught during a self-review pass and added on
  `Claim` and `ClaimReserveComponent`.
- **Correction:** the global query filter for tenant isolation was
  initially hard-coded per entity. Replaced with a reflection loop in
  `ClaimsDbContext.OnModelCreating` so future entities automatically
  inherit the `OrganizationEntityId == current && !IsDeleted` filter.

### Phase 3 — Backend handlers (Claude)

- For each command/query, the prompt sequence was: spec → command record
  → validator → handler. Each handler explicitly cites the BR-* rule
  it enforces.
- **Correction:** Claude's first pass enqueued the GL posting job from
  inside `OpenReserveHandler` **before** `SaveChangesAsync`. If the
  save failed, the job would still run against a non-existent reserve.
  Reordered to enqueue **after** save. Same fix applied to `Adjust` and
  `Approve` handlers.
- **Correction:** `Hangfire` idempotency was initially "check if any
  job with this key exists in Hangfire's queue" — fragile because
  Hangfire purges completed jobs. Replaced with checking the
  `ClaimAuditLog` for a prior `GL_POSTING_SIMULATED` entry containing the
  idempotency key in its `NewValues`. The audit log is the durable source
  of truth.

### Phase 4 — Frontend (Claude)

- Scaffolded with `ng new frontend --routing --style=scss --defaults`.
- Asked Claude to generate the typed `ClaimsApiService`, the
  `authInterceptor` and `errorInterceptor` (functional interceptors,
  Angular 20 idiom), the role-switcher in the toolbar, and the three
  feature components.
- **Correction:** the multi-step form initially put `requiredHasClaimant`
  as a `RuleForEach` validator that fired on every party. Refactored to
  a `FormArray`-level validator (`hasOneClaimant`) so the error message
  is shown once at the top of step 2.
- **Correction:** Material 20 changed palette tokens — Claude wrote
  `mat.$indigo-palette`, which exists only in M2. Build failure caught
  the issue; switched to `mat.$violet-palette` (M3 token).

### Phase 5 — Review, docs, deploy (Claude)

- Ran `dotnet build` after each layer and `ng build` after the frontend.
  Fixes:
  - Application project missing EF Core reference (added when
    `IClaimsDbContext` exposes `DbSet<>`).
  - `ValidationException` name collision with `FluentValidation.ValidationException`
    in `ValidationBehavior` — qualified with `Exceptions.ValidationException`.
  - `@angular/animations` peer dependency missing — added via `npm install`.
- Claude drafted `ARCHITECTURE.md`, `README.md`, and this file. The
  candidate reviewed and corrected the wording around BR-R-06 (Claude
  initially described it as "update in place with audit", which
  contradicts the spec that a rejected reserve becomes a **new** reserve
  record). The domain code enforces this with `DomainException
  RESERVE_REJECTED`.

## Representative prompts used

> *(Reproduced from the candidate's session with Claude.)*

### Prompt 1 — Domain modeling

> Here are the FRS entities for the Claims module (paste of §3.2). Treat
> `Claim` as the aggregate root for FNOL intake + the parties + the
> documents + the audit log. Treat `ClaimReserveComponent` as **also**
> an aggregate root — it has its own concurrency boundary and authority
> state machine, but it can only be mutated through methods on `Claim`
> so the total-reserve cap (BR-R-07) stays consistent. Generate the C#
> entities. Use `BaseEntity` with `OrganizationEntityId`, soft delete,
> audit columns; `AggregateRoot : BaseEntity` adds `RowVer` and a
> `DomainEvents` list. Pure C# only — no EF/MediatR references.

### Prompt 2 — Hangfire idempotency

> Implement `GlPostingJob` so that retries are safe. The idempotency key
> is `Reserve:{ReserveId:N}:Change:{ChangeSequence}`. The job should not
> trust Hangfire's queue state — instead, check the `ClaimAuditLog` for a
> prior `GL_POSTING_SIMULATED` entry whose `NewValues` contains the key,
> and skip if found. Also exit cleanly if the reserve's
> `ChangeSequence` has advanced beyond the value passed in (a newer
> adjustment happened) or if its `ApprovalStatus` is no longer Approved
> / AutoApproved. Use `[AutomaticRetry(3, [10,30,60])]`.

### Prompt 3 — Multi-step FNOL form

> Build an Angular reactive multi-step form using `MatStepper` with three
> steps: (1) policy autocomplete + loss date + cause code + location +
> description, (2) parties (FormArray, at least one Claimant required —
> array-level validator) + optional risk objects, (3) optional initial
> reserve with a live "authority band" indicator showing whether the
> amount will auto-approve / require Supervisor / require Manager.
> Standalone components, Angular 20 control flow (`@if`, `@for`),
> typed reactive forms, no NgModules.

## What was AI-generated vs hand-authored

| Area | AI share | Human refinement |
|------|---------|------------------|
| Domain entities (`Claim`, `ClaimReserveComponent`) | ~80% | Decided aggregate-root split; tweaked rule wording in `DomainException` codes to match BR-* numbers |
| EF Core configurations | ~90% | Added `RowVer` on aggregate roots; reflection-based query filter loop |
| MediatR pipeline behaviors | ~70% | Resolved `ValidationException` name collision; added logging behavior |
| FluentValidation validators | ~85% | Added explicit `WithErrorCode("BR-…")` per rule |
| `GlPostingJob` idempotency | ~50% | Switched from queue-state check to audit-log check (described above) |
| Angular service layer | ~90% | None — generated from the OpenAPI contract |
| Angular FNOL form | ~70% | Lifted Claimant validator from per-row to array-level |
| `ARCHITECTURE.md` / `README.md` | ~80% | Corrected BR-R-06 description; tightened the Azure topology diagram |

## Two specific AI-correction examples

### Example A — `GlPostingJob` idempotency

**AI output (initial):**

```csharp
var alreadyEnqueued = await _hangfireMonitoring.GetEnqueuedJobsAsync(...)
    .AnyAsync(j => j.Job.Args[3].ToString() == idempotencyKey);
if (alreadyEnqueued) return;
```

**Problem:** Hangfire deletes completed jobs after a retention window. If
the job runs successfully, then is enqueued again (e.g. a retry from
upstream), the duplicate check fails because the prior run is no longer
in `GetEnqueuedJobs`. We'd double-post.

**Fix (committed):** Use the application's own durable audit log as
the idempotency record. The `ClaimAuditLog` table is append-only and
includes the key in `NewValues`:

```csharp
var alreadyLogged = await _db.ClaimAuditLogs
    .IgnoreQueryFilters()
    .AnyAsync(a => a.ClaimId == claimId
        && a.EventType == AuditEventType.GlPostingSimulated
        && a.NewValues != null
        && a.NewValues.Contains(idempotencyKey), cancellationToken);
if (alreadyLogged) return;
```

### Example B — `CreateClaimValidator` "at least one Claimant"

**AI output (initial):**

```csharp
RuleForEach(x => x.Parties).ChildRules(p =>
{
    p.RuleFor(x => x.PartyType)
        .Equal(PartyType.Claimant)
        .WithErrorCode("BR-C-03");
});
```

**Problem:** This fires the error for **every** non-Claimant party, so
a claim with one Claimant + one Witness shows two errors and incorrectly
flags the witness as invalid.

**Fix (committed):** Validate the collection as a whole, not each item:

```csharp
RuleFor(x => x.Parties)
    .Must(p => p != null && p.Any(party => party.PartyType == PartyType.Claimant))
    .WithErrorCode("BR-C-03")
    .WithMessage("A claim must include at least one Claimant party.");
```

The domain also enforces it via `Claim.EnsureHasClaimant()` as a
defense-in-depth.

## Honest assessment — where AI shone, where it didn't

**Where AI accelerated delivery most:**

- **Mechanical scaffolding** — generating the 10 EF Core configurations,
  the parallel `Command/Validator/Handler` triplets, and the Angular
  typed service was minutes instead of hours.
- **Cross-cutting concerns** — pipeline behaviors, error middleware,
  problem+JSON formatting were one-shot generations that compiled.
- **Documentation** — both `ARCHITECTURE.md` and this file follow a
  template that Claude can reliably populate, freeing time for actual
  design decisions.

**Where AI needed close supervision:**

- **Concurrency-sensitive code** (the GL-posting job) is exactly the kind
  of area where Claude's first answer was plausible but wrong in a way
  that a junior reviewer might have missed. The fix required understanding
  Hangfire's retention model, which the AI did not surface unprompted.
- **Domain modeling decisions** (single vs. multiple aggregate roots,
  whether the audit log is written inside the aggregate or by an event
  handler) are genuinely architectural and benefit from human-led
  reasoning. Claude defaults to the most popular pattern in its training
  data; for an insurance domain with strict audit requirements, the
  audit-inside-aggregate decision matters and was a human call.
- **Cross-framework API drift** (Material 20's M3 palette tokens,
  Angular 20 functional interceptors) tripped Claude up because the
  training data spans multiple major versions; build failures caught
  the issues, but those iterations cost cycles.

## AI interaction history

This entire session is the AI interaction history. The chat transcript
between the candidate and Claude (this Claude Code session, run via the
CLI in this repository) can be exported from the
`.claude/projects/.../` directory in the user's home folder, or shared as
a screen recording during the live review session.
