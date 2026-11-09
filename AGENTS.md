# Studium-Catalog — Project State & Agent Context (`AGENTS.md`)

> **MANDATORY AGENT DIRECTIVE:**
> After **every user prompt**, always evaluate whether we reached a useful architectural conclusion, resolved a design question, or made code/configuration changes. If so, **update both the relevant sections and Section 3 (`Recent Changes History`) in this `./AGENTS.md` file before finishing your turn** so any future chat session immediately has full context of the project state.

---

## 1. Architectural Decisions & Conclusions Log

### 1.1 Target Framework (`.NET 10.0`) & Clean Architecture
- **Decision:** Build `Studium-Catalog` matching the `Studium-Users` gold standard with .NET 10 (`net10.0`), Central Package Management (`Directory.Packages.props`), and five Clean Architecture projects:
  - `Studium-Catalog.Domain` (zero external dependencies; rich entities and domain rules)
  - `Studium-Catalog.Data` (EF Core 10, Npgsql PostgreSQL)
  - `Studium-Catalog.Services` (DTOs, Wolverine message contracts, Mapster)
  - `Studium-Catalog.Api` (Thin composition root, Scalar, OpenAPI, Health check)
  - `Studium-Catalog.Tests` (xUnit test suite with 23 passing unit tests)

### 1.2 Messaging Topology: Wolverine with RabbitMQ & EventId Propagation
- **Decision:**
  - Connect to RabbitMQ broker (`amqp://studium:StudiumRabbitMQ2026!@studium-rabbitmq-service.studium.svc.cluster.local:5672`) with `.AutoProvision()`.
  - Publish `RoomMaintenanceEvent`, `OperatingHoursChangedEvent`, `RoomStatusChangedEvent`, and `ResourceUpdatedEvent` to `studium-booking-events-queue`.
  - Publish `AuditIntegrationEvent` (`[MessageIdentity("studium.audit.integration-event")]`) to `studium-audit-events-queue` for all administrative actions (building creation/update/deletion, room creation/update/activation/deactivation, and maintenance block scheduling/cancellation).
  - Outbound EventId Propagation: All events generated via `CatalogEventFactory` and controllers carry a deterministic `EventId: Guid.NewGuid()` for distributed deduplication.
  - Configured transactional outbox via `opts.Policies.AutoApplyTransactions()`.
  - Poison Message & Dead-Letter Handling: Configured Wolverine transient exception retries (`NpgsqlException`, `TimeoutException`, `HttpRequestException`) with exponential cooldowns and unhandled errors routing to `.error` queue.

### 1.3 Asynchronous Event Choreography vs Synchronous Query
- **Decision:** Avoid direct HTTP RPC between services for availability checking. `Studium-Booking` maintains its own replicated read model (`ReplicatedRoom`, `ReplicatedMaintenanceBlock`, `ReplicatedOperatingHours`) updated purely via RabbitMQ events published by `Studium-Catalog`.

### 1.4 Database Resilience & Health Check Segregation
- Configured Npgsql's transient execution strategy (`EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)`).
- Segregated Kubernetes probes: shallow liveness at `/health` and deep readiness at `/health/ready` via `DatabaseHealthCheck` (`CatalogDbContext.Database.CanConnectAsync()`).

### 1.5 Dynamic CORS Configuration
- **Decision:** Created reusable `CorsExtensions.AddStudiumCors(IConfiguration)` reading comma-separated allowed origins from `Cors:AllowedOrigins` or environment variable `CORS_ALLOWED_ORIGINS` / `Cors__AllowedOrigins` (defaulting to allow all for local development).

---

## 2. Directory Structure & Key Files
- **`Domain/`:**
  - `Entities/Buildings/Building.cs` — Building aggregate root.
  - `Entities/Rooms/Room.cs` — Room aggregate root managing resources, operating hours, and maintenance blocks.
  - `Entities/Rooms/RoomResource.cs`, `OperatingHours.cs`, `MaintenanceBlock.cs` — Owned entities.
- **`Data/`:**
  - `CatalogDbContext.cs` — EF Core context with applied configurations.
  - `Configurations/` — Fluent API configurations.
  - `Migrations/` — Auto-applied migrations.
- **`Services/`:**
  - `DTOs/EventDtos.cs` — Request, response, and event contracts (includes `AuditIntegrationEvent`).
  - `CatalogEvents/CatalogEventFactory.cs` — Outbound event factory propagating deterministic `EventId`.
  - `Mapping/MappingConfig.cs` — Mapster compile-safe mappings.
- **`Api/`:**
  - `Controllers/BuildingsController.cs` & `RoomsController.cs` — REST endpoints under `/api/catalog/buildings` and `/api/catalog/rooms` with audit log publishing.
  - `HostedServices/DatabaseMigrationHostedService.cs` — Startup migration runner.
  - `Extensions/AuthenticationExtensions.cs`, `MessagingExtensions.cs`, `CorsExtensions.cs`, `DatabaseHealthCheck.cs`.
- **`Tests/`:**
  - `Domain/BuildingTests.cs` & `RoomTests.cs` — Comprehensive domain unit tests.
  - `DTOs/DtoMappingTests.cs` — Mapster mapping tests.

---

## 3. Recent Changes History
- **2026-11-09 (Administrative Audit Publishing & Outbound EventId Propagation):**
  - Added `AuditIntegrationEvent` message contract and configured Wolverine routing to `studium-audit-events-queue`.
  - Added `EventId: Guid? = null` to all catalog integration events, populated via `CatalogEventFactory`.
  - Added audit logging to `BuildingsController` (create, update, delete) and `RoomsController` (create, update, activate/deactivate, schedule/cancel maintenance).
  - Added `RabbitMQ:AuditQueueName` in `appsettings.json` and `k8s/deployment.yaml`.
  - Verified 23/23 unit tests pass.
- **2026-11-09 (Dynamic CORS Configuration):**
  - Added `CorsExtensions.cs` reading allowed origins dynamically from configuration/environment variables.
  - Hooked `AddStudiumCors` and `UseCors(CorsExtensions.PolicyName)` into `Program.cs`.
  - Added `Cors:AllowedOrigins: "*"` in `appsettings.json` and `Cors__AllowedOrigins` in `k8s/deployment.yaml`.
  - Verified 23/23 unit tests pass.
- **2026-11-09 (Database Resilience, Deep Readiness Probe & Wolverine DLQ):**
  - Added `EnableRetryOnFailure(3)` inside PostgreSQL `UseNpgsql` registration.
  - Added `DatabaseHealthCheck.cs` verifying database connectivity for Kubernetes `readinessProbe` at `/health/ready`.
  - Configured Wolverine 3x retry policies with cooldown for `NpgsqlException`, `TimeoutException`, `HttpRequestException`, and routed terminal poison messages to dead-letter error queue (`opts.OnException<Exception>().MoveToErrorQueue()`).
  - Regenerated Wolverine static handlers via `codegen write` and verified 23/23 unit tests pass.
- **2026-11-09 (RBAC Hardening, Message Identity & Transactional Outbox):**
  - Configured `AdminOnly` authorization policy and Cognito claim mapping in `AuthenticationExtensions.cs`.
  - Added `[MessageIdentity]` to events in `Services/DTOs/EventDtos.cs`.
  - Enabled transactional outbox via `opts.Policies.AutoApplyTransactions()` in `MessagingExtensions.cs`.
  - Regenerated Wolverine static handlers and verified all 23 unit tests pass.
- **2026-11-09 (Initial Microservice Scaffold & Implementation):**
  - Scaffolded .NET 10 Clean Architecture solution with PostgreSQL database `studium_catalog`.
  - Implemented building and room management, operating hours, maintenance blocks, and Wolverine RabbitMQ publishing.
  - Verified 23/23 unit tests and generated static Wolverine handlers via `codegen write`.
