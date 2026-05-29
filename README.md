# Claims Module — FNOL Intake & Reserve Management

DICEUS Engineering Excellence Programme — Fullstack Technical Assessment.
Greenfield vertical slice of an enterprise Policy Administration System (PAS)
claims module: **First Notice of Loss (FNOL) Intake** plus **Reserve Component
Management** with authority-based approvals and Hangfire-driven GL posting
simulation.

- **Backend:** .NET 9 / C# 13, ASP.NET Core Web API, Clean Architecture,
  CQRS via MediatR, EF Core 9, FluentValidation, AutoMapper, Hangfire,
  Azure Blob Storage.
- **Frontend:** Angular 20 (LTS 18+ compatible), Angular Material,
  Reactive Forms, route-level lazy loading.
- **Database:** SQL Server 2022 (LocalDB / Docker / Azure SQL).
- **CI/CD:** GitHub Actions.

See [`ARCHITECTURE.md`](./ARCHITECTURE.md) for design decisions and
[`AI-WORKFLOW.md`](./AI-WORKFLOW.md) for the AI-assisted delivery log.

## Repository layout

```
Fnol/
├── backend/
│   ├── ClaimsModule.sln
│   └── src/
│       ├── ClaimsModule.Domain         # Entities, value objects, enums, domain events
│       ├── ClaimsModule.Application    # MediatR commands/queries, validators, DTOs, profiles
│       ├── ClaimsModule.Infrastructure # Storage, Hangfire jobs, seeded policy service
│       ├── ClaimsModule.Persistence    # EF Core DbContext, configurations, migrations, UoW
│       └── ClaimsModule.API            # Controllers, middleware, mock auth, Program.cs
├── frontend/                           # Angular workspace (Material, lazy-loaded routes)
├── docker-compose.yml                  # Local SQL Server + Azurite
└── .github/workflows/ci-cd.yml         # Build + (optional) Azure deploy
```

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.0.x |
| Node.js | 22.x |
| npm | 10.x |
| Angular CLI | 20.x (`npm i -g @angular/cli@20`) |
| dotnet-ef tool | 9.0.x (`dotnet tool install --global dotnet-ef --version 9.0.4`) |
| Docker (optional) | for `docker-compose` SQL Server + Azurite |
| SQL Server | 2022, LocalDB, or Azure SQL |

## Local development setup

### 1. Start SQL Server (optional, via Docker)

```bash
docker compose up -d sqlserver azurite
```

If you prefer LocalDB, the default `appsettings.json` already points at
`(localdb)\MSSQLLocalDB`.

### 2. Apply EF Core migrations

```bash
cd backend
dotnet ef database update \
  --project src/ClaimsModule.Persistence/ClaimsModule.Persistence.csproj \
  --startup-project src/ClaimsModule.Persistence/ClaimsModule.Persistence.csproj
```

The API also applies migrations and seeds reference data automatically on
first run (`Program.cs` → `ReferenceDataSeeder.SeedAsync`).

### 3. Run the backend

```bash
cd backend
dotnet run --project src/ClaimsModule.API/ClaimsModule.API.csproj
```

The API listens on `http://localhost:5000` / `https://localhost:5001` by
default. Swagger UI: `http://localhost:5000/swagger`. Hangfire dashboard:
`http://localhost:5000/hangfire`.

### Hangfire dashboard access

The dashboard is gated by `HangfireRoleDashboardFilter`. A request is
allowed if **any** of these is true:

1. The request comes from the loopback interface (the PC admin running the
   API can always reach it).
2. The authenticated principal carries one of the roles configured under
   `Hangfire:Dashboard:AllowedRoles` (default: `Supervisor`, `Manager`).
3. The request supplies `X-Mock-Role: Supervisor|Manager` (Swagger / curl).
4. The browser appends `?role=Supervisor` (or `Manager`) to the URL.

That keeps the dashboard available to operators with elevated rights and
blocks anonymous access from outside the host.

### 4. Run the frontend

```bash
cd frontend
npm install
npm start         # ng serve on http://localhost:4200
```

The frontend points at `http://localhost:5000/api` via
`src/environments/environment.ts`.

## Configuration

Non-secret defaults live in `backend/src/ClaimsModule.API/appsettings.json` and
can be overridden via environment variables (standard ASP.NET configuration).

**Secrets — connection strings and storage account keys — never go in
`appsettings.json`.** Instead, they belong in `appsettings.Local.json` (next to
`appsettings.json`), which is gitignored. A template lives at
`appsettings.Local.json.example` — copy it once:

```powershell
Copy-Item backend/src/ClaimsModule.API/appsettings.Local.json.example `
          backend/src/ClaimsModule.API/appsettings.Local.json
```

then edit the copy with your real values. Configuration precedence (lowest →
highest):

1. `appsettings.json` (checked in, no secrets)
2. `appsettings.{Env}.json` (e.g. `appsettings.Development.json`)
3. `appsettings.Local.json` and `appsettings.{Env}.Local.json` (gitignored)
4. User Secrets (Development only)
5. Environment variables (e.g. `ConnectionStrings__ClaimsDb=...`)
6. Command-line arguments

| Key | Purpose | Example |
|-----|---------|---------|
| `ConnectionStrings:ClaimsDb` | Main SQL Server connection | `Server=(localdb)\MSSQLLocalDB;Database=ClaimsModule;Trusted_Connection=True;TrustServerCertificate=True;` |
| `ConnectionStrings:Hangfire` | Hangfire SQL Server connection (blank or missing → falls back to `ClaimsDb`) | leave blank for local |
| `Hangfire:Dashboard:AllowedRoles` | Roles allowed to view `/hangfire` | `[ "Supervisor", "Manager" ]` |
| `Hangfire:Dashboard:AllowLocalRequests` | Loopback bypass for the PC admin | `true` |
| `Storage:Provider` | `LocalFileSystem` or `AzureBlob` | `LocalFileSystem` |
| `Storage:AzureConnectionString` | Required when `Provider=AzureBlob` | `DefaultEndpointsProtocol=https;AccountName=...` |
| `Storage:LocalRootPath` | Local upload root | `App_Data/uploads` |
| `Storage:ContainerName` | Blob container name | `claim-documents` |
| `Cors:AllowedOrigins` | Allowed frontend origins | `["http://localhost:4200"]` |

## Mock authentication

The frontend has a role switcher in the top-right (Handler / Supervisor /
Manager). It builds an unsigned JWT and also sends `X-Mock-Role`,
`X-Mock-UserId`, `X-Mock-UserName` headers — the backend
`MockJwtMiddleware` accepts either. Use this to test the role-gated reserve
approval flow.

Pre-seeded users:

| User Id | Display name | Role | Use for |
|---------|--------------|------|---------|
| `handler-1` | Hayley Handler | Handler | FNOL intake, opening reserves ≤ $10,000 |
| `supervisor-1` | Sam Supervisor | Supervisor | Approving reserves $10,000–$100,000 |
| `manager-1` | Morgan Manager | Manager | Approving reserves > $100,000 |

## Implemented endpoints

All endpoints are surfaced through Swagger. Summary:

| Method + Route | Notes |
|----------------|-------|
| `POST /api/claims` | Create claim (FNOL); validates BR-C-01..06, raises `ClaimCreatedEvent` |
| `GET /api/claims` | List with filters: status, date range, handler, cause code |
| `GET /api/claims/{id}` | Detail incl. parties, risk objects, reserves, documents |
| `PUT /api/claims/{id}/status` | Validates allowed transitions table |
| `GET /api/claims/{id}/audit` | Immutable append-only event log |
| `POST /api/claims/{id}/documents` | Upload to Azure Blob (or local FS) |
| `GET /api/claims/{id}/documents` | Returns 1-hour SAS URLs |
| `POST /api/claims/{id}/reserves` | Open reserve; auto-approves if ≤ $10K |
| `PUT /api/claims/{id}/reserves/{rid}` | Adjust reserve, creates `ReserveHistory` |
| `GET /api/claims/{id}/reserves` | List with history |
| `POST /api/claims/{id}/reserves/{rid}/approve` | Role-gated (Supervisor or Manager) |
| `POST /api/claims/{id}/reserves/{rid}/reject` | Role-gated with reason |
| `GET /api/policies/search?q=` | Simulated policy lookup (seeded dataset) |
| `GET /api/policies/{id}/coverage` | Simulated coverages |
| `GET /api/reference/cause-of-loss-codes` | Active codes, optional peril filter |
| `GET /api/reference/claim-statuses` | Statuses with allowed transitions |

## Frontend screens

- **Claims Dashboard** (`/claims`) — paginated table with filter chips,
  color-coded status badges, click-through to detail.
- **FNOL Intake** (`/claims/new`) — 3-step reactive form (policy + loss,
  parties + risk objects, initial reserve + review). Real-time authority
  threshold indicator.
- **Claim Detail** (`/claims/:id`) — tabs for Overview, Parties, Reserves,
  Documents, Audit Log. Status-transition menu, reserve approve/reject
  visibility gated to Supervisor/Manager roles.

## Running tests

The assessment does not require an exhaustive test suite, but the domain
layer is designed for testability — `Claim` and `ClaimReserveComponent`
have no framework dependencies and enforce all BR-* rules in pure C#.
Add a test project with:

```bash
cd backend
dotnet new xunit -o tests/ClaimsModule.Domain.Tests
dotnet sln add tests/ClaimsModule.Domain.Tests
```

## Docker (single combined container)

`Dockerfile` at the repo root builds the Angular SPA and the .NET API in a
three-stage build, then copies the SPA bundle into `wwwroot` so a single
container serves everything:

- `/api/*`, `/swagger`, `/hangfire`, `/health` — handled by ASP.NET Core.
- everything else falls back to `wwwroot/index.html` so Angular's client-side
  routing (`/claims`, `/claims/{guid}`, `/claims/new`) survives a browser
  refresh.

```powershell
# build + run the whole stack (SQL Server + Azurite + app)
docker compose up --build

# open
#   http://localhost:8080            -> Angular SPA
#   http://localhost:8080/swagger    -> Swagger UI
#   http://localhost:8080/hangfire   -> Hangfire dashboard (loopback bypass)
```

To stop and wipe data:

```powershell
docker compose down -v
```

## Azure deployment (free-tier path)

`deploy/azure-deploy.sh` provisions the smallest Azure footprint that runs
this image inside the always-free quotas:

- **Azure Container Registry (Basic)** — free for 100 GB.
- **Azure SQL Database (Free offer)** — 32 GB, auto-pauses when idle.
- **Azure Container Apps** — 180 000 vCPU-seconds + 360 000 GiB-seconds free
  per month. With `--min-replicas 0` the app scales to zero between
  requests, so the free quota covers a continuous demo easily.
- **Log Analytics workspace** — required by Container Apps; first 5 GB/month
  ingestion is free.

```bash
# Once: log in + register providers
az login
az provider register -n Microsoft.App
az provider register -n Microsoft.OperationalInsights
az provider register -n Microsoft.ContainerRegistry

# Deploy
export SQL_PASSWORD='Your-12-char-strong!Password'
bash deploy/azure-deploy.sh

# The script prints the public URL at the end, e.g.
#   https://claims-fnol.<random>.westeurope.azurecontainerapps.io
```

`az acr build` does the image build server-side, so you don't need a
running local Docker daemon for the Azure path — only the Azure CLI.

Tear down with `az group delete --name rg-claims-fnol --yes`.

## CI/CD (alternative — split deployment)

The deployment script above is the recommended path. The repo also keeps a
GitHub Actions pipeline at `.github/workflows/ci-cd.yml` that builds the
backend, generates an idempotent EF migration script, builds the frontend,
and (when triggered manually with the right secrets/vars set) deploys the
two halves to App Service and Static Web Apps separately.

Required GitHub variables / secrets for deploy:

- `vars.AZURE_BACKEND_APP_NAME`, `vars.AZURE_FRONTEND_APP_NAME`
- `secrets.AZURE_CLIENT_ID`, `secrets.AZURE_TENANT_ID`, `secrets.AZURE_SUBSCRIPTION_ID` (OIDC)
- `secrets.AZURE_SQL_CONNECTION_STRING`
- `secrets.AZURE_STATIC_WEB_APPS_API_TOKEN`

## Application walkthrough

1. Open `http://localhost:4200`. Default role is **Handler**.
2. Click **Log New Claim**.
   - Step 1: search a seeded policy (e.g. `POL-2026-0000001`), pick a loss
     date in the policy's effective window, choose a cause code (e.g.
     `COLLISION`), fill location/description.
   - Step 2: at least one Claimant party is required (BR-C-03).
   - Step 3: optionally open an initial reserve — the band indicator shows
     whether it will auto-approve, require Supervisor, or require Manager.
3. Submit. You're redirected to the claim detail.
4. Use the role switcher to become a Supervisor or Manager and approve a
   pending reserve.
5. Upload a document on the Documents tab.
6. Watch the Audit Log tab: every change writes an immutable event.
7. Check `/hangfire` to see the GL posting jobs and the recurring
   SLA-monitor job (every 15 minutes).

## Known caveats

- AutoMapper 12.0.1 is pinned to match `AutoMapper.Extensions.Microsoft.DependencyInjection` 12.0.1. The package emits a known-vulnerability warning that is not addressed in any maintained branch; production deployments would migrate to Mapster or hand-rolled mappers.
- The Hangfire dashboard is role-gated by `HangfireRoleDashboardFilter` (Supervisor / Manager by default, plus localhost). For production replace the mock-role inputs with the real JWT principal — the filter already inspects `ClaimsPrincipal` first.
- Mock JWT is unsigned. Replace `MockJwtMiddleware` + `HttpCurrentUser` with the real auth handler for production.
- BaseEntity sets `Id = Guid.NewGuid()` so aggregates can wire up child FKs immediately. `ClaimsDbContext.OnModelCreating` marks Guid PKs as `ValueGenerated.Never` so EF Core does not mis-classify navigation-added rows as UPDATEs. The DB column keeps the `NEWSEQUENTIALID()` default per FRS Appendix A.4.
