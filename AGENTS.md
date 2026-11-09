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
- **Context:** Decoupled event publishing for room status, maintenance schedules, operating hours, and resource modifications.
- **Decision:**
  - Connect to RabbitMQ broker (`amqp://studium:StudiumRabbitMQ2026!@studium-rabbitmq-service.studium.svc.cluster.local:5672`) with `.AutoProvision()`.
  - Publish `RoomMaintenanceEvent`, `OperatingHoursChangedEvent`, `RoomStatusChangedEvent`, and `ResourceUpdatedEvent` to `studium-booking-events-queue`.
  - Configured transactional outbox via `opts.Policies.AutoApplyTransactions()` to guarantee atomic messaging across EF Core updates.
  - SQS version preserved on branch `origin/sqs`.

### 1.3 Asynchronous Event Choreography vs Synchronous Query
- **Decision:** Avoid direct HTTP RPC between services for availability checking. `Studium-Booking` maintains its own replicated read model (`ReplicatedRoom`, `ReplicatedMaintenanceBlock`, `ReplicatedOperatingHours`) updated purely via RabbitMQ events published by `Studium-Catalog`.

### 1.4 Pre-Generated Wolverine Codegen in Docker Build
- Configured `CritterStackDefaults` with `TypeLoadMode.Static` in Production, and `codegen write` pre-generates handler code during Docker multi-stage builds.

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
  - `Extensions/AuthenticationExtensions.cs` & `MessagingExtensions.cs` — Modular DI extensions.
- **`Tests/`:**
  - `Domain/BuildingTests.cs` & `RoomTests.cs` — Comprehensive domain unit tests.
  - `DTOs/DtoMappingTests.cs` — Mapster mapping tests.
- [`Dockerfile`](./Dockerfile) — Multi-stage build with `codegen write`.
- [`k8s/deployment.yaml`](./k8s/deployment.yaml) — Deployment, Service, ConfigMap, HPA for `studium-catalog`.
- [`.github/workflows/ci-cd.yml`](./.github/workflows/ci-cd.yml) — CI/CD workflow building, pushing to ECR, syncing secrets, and deploying to EKS.

---

## 3. Recent Changes History
- **2026-11-09 (Wolverine Transactional Outbox):**
  - Enabled transactional outbox via `opts.Policies.AutoApplyTransactions()` in `MessagingExtensions.cs`.
  - Regenerated Wolverine static handlers and verified all 23 unit tests pass.
- **2026-11-09 (Cleaned Obsolete Notifications Queue & SQS Config):**
  - Removed obsolete publishing of `RoomMaintenanceEvent` to `studium-notifications-queue` since notifications are synchronous in `Studium-Notifications`.
  - Replaced legacy `SQS` settings in `appsettings.json` with active `RabbitMQ` settings.
  - Regenerated static handlers via `codegen write` and verified 23 unit tests pass.
- **2026-11-09 (Migrated Messaging from AWS SQS to RabbitMQ):**
  - Replaced `WolverineFx.AmazonSqs` with `WolverineFx.RabbitMQ` (v6.63.0) across `Directory.Packages.props`, `Services/Studium-Catalog.Services.csproj`, and `Api/Studium-Catalog.Api.csproj`.
  - Updated `Api/Extensions/MessagingExtensions.cs` to connect to RabbitMQ broker with `.AutoProvision()`.
  - Updated `k8s/deployment.yaml` with RabbitMQ configuration keys.
  - Re-generated static handlers; 23 unit tests passing.
  - SQS version preserved on branch `origin/sqs`.
- **2026-11-08 (Alignment with Golden Path Directives):**
  - Standardized on singular canonical Ingress path prefix (`[Route("api/catalog/buildings")]` and `[Route("api/catalog/rooms")]`).
  - Removed internal synchronous HTTP contract `GET /api/rooms/{id}/availability-rules`, adhering strictly to asynchronous event-driven inter-service architecture.
