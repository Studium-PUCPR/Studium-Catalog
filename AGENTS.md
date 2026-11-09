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

### 1.2 Messaging Topology: Wolverine with RabbitMQ
- **Decision:**
  - Connect to RabbitMQ broker (`amqp://studium:StudiumRabbitMQ2026!@studium-rabbitmq-service.studium.svc.cluster.local:5672`) with `.AutoProvision()`.
  - Publish `RoomMaintenanceEvent`, `OperatingHoursChangedEvent`, `RoomStatusChangedEvent`, and `ResourceUpdatedEvent` to `studium-booking-events-queue`.
  - Configured transactional outbox via `opts.Policies.AutoApplyTransactions()`.
  - Poison Message & Dead-Letter Handling: Configured Wolverine transient exception retries (`NpgsqlException`, `TimeoutException`, `HttpRequestException`) with exponential cooldowns and unhandled errors routing to `.error` queue.

### 1.3 Asynchronous Event Choreography vs Synchronous Query
- **Decision:** Avoid direct HTTP RPC between services for availability checking. `Studium-Booking` maintains its own replicated read model (`ReplicatedRoom`, `ReplicatedMaintenanceBlock`, `ReplicatedOperatingHours`) updated purely via RabbitMQ events published by `Studium-Catalog`.

### 1.4 Database Resilience & Health Check Segregation
- Configured Npgsql's transient execution strategy (`EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)`).
- Segregated Kubernetes probes: shallow liveness at `/health` and deep readiness at `/health/ready` via `DatabaseHealthCheck` (`CatalogDbContext.Database.CanConnectAsync()`).

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
  - `DTOs/` — Request, response, and event contracts.
  - `Mapping/MappingConfig.cs` — Mapster compile-safe mappings.
- **`Api/`:**
  - `Controllers/BuildingsController.cs` & `RoomsController.cs` — REST endpoints under `/api/catalog/buildings` and `/api/catalog/rooms`.
  - `HostedServices/DatabaseMigrationHostedService.cs` — Startup migration runner.
  - `Extensions/AuthenticationExtensions.cs`, `MessagingExtensions.cs`, `DatabaseHealthCheck.cs`.
- **`Tests/`:**
  - `Domain/BuildingTests.cs` & `RoomTests.cs` — Comprehensive domain unit tests.
  - `DTOs/DtoMappingTests.cs` — Mapster mapping tests.

---

## 3. Recent Changes History
- **2026-11-09 (Database Resilience, Deep Readiness Probes & DLQ Policies):**
  - Configured `EnableRetryOnFailure(3)` on PostgreSQL connection.
  - Added deep readiness probe `/health/ready` (`DatabaseHealthCheck`) and pointed Kubernetes `readinessProbe` to it.
  - Configured Wolverine exception policies for transient retries and dead-letter error queue routing.
  - Verified all 23 unit tests pass.
- **2026-11-09 (Wolverine Transactional Outbox):**
  - Enabled transactional outbox via `opts.Policies.AutoApplyTransactions()` in `MessagingExtensions.cs`.
- **2026-11-09 (Cleaned Obsolete Notifications Queue & SQS Config):**
  - Removed obsolete publishing of `RoomMaintenanceEvent` to `studium-notifications-queue`.
- **2026-11-09 (Migrated Messaging from AWS SQS to RabbitMQ):**
  - Replaced `WolverineFx.AmazonSqs` with `WolverineFx.RabbitMQ` (v6.63.0).
