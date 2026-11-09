# Studium-Catalog — Project State & Agent Context (`AGENTS.md`)

> **MANDATORY AGENT DIRECTIVE:**
> After **every user prompt**, always evaluate whether we reached a useful architectural conclusion, resolved a design question, or made code/configuration changes. If so, **update both the relevant sections and Section 3 (`Recent Changes History`) in this `./AGENTS.md` file before finishing your turn** so any future chat session immediately has full context of the project state.

---

## 1. Architectural Decisions & Conclusions Log

### 1.1 Target Framework (`.NET 10.0`) & Clean Architecture
- **Decision:** Build `Studium-Catalog` matching the `Studium-Users` gold standard with .NET 10 (`net10.0`), Central Package Management (`Directory.Packages.props`), and five Clean Architecture projects:
  - `Studium-Catalog.Domain` (zero external dependencies)
  - `Studium-Catalog.Data` (EF Core 10, Npgsql PostgreSQL)
  - `Studium-Catalog.Services` (DTOs, Wolverine, Mapster)
  - `Studium-Catalog.Api` (Thin composition root, Scalar, OpenAPI, Health check)
  - `Studium-Catalog.Tests` (xUnit test suite with 23 passing tests)

### 1.2 Domain Model & Aggregates
- **Entities:**
  - `Building`: Campus building with unique institutional `Code`, `Name`, optional `Address`, and `Rooms` collection. Enforces `EnsureCanBeDeleted` invariant preventing deletion when rooms exist.
  - `Room` (Aggregate Root): Represents physical room with `BuildingId`, `Code` (unique per building), `Name`, `Type` (`RoomType`), `Capacity` (1-1000), `Floor`, `MapPositionX`/`MapPositionY` (0-100% pin coordinates for floor plan), and `IsActive` flag. Owns resources, operating hours, and maintenance blocks.
  - `ResourceItem`: Equipment installed in room (`ResourceType`, `Quantity`, `ResourceStatus`). Mutated through `Room.AddResource`, `UpdateResource`, `RemoveResource`.
  - `AvailabilitySchedule`: Weekly operating windows (`DayOfWeek`, `OpensAt`, `ClosesAt`). Enforces chronological ordering, opening < closing, and non-overlapping windows per day via `Room.SetOperatingHours`.
  - `MaintenanceBlock`: Temporary blocking interval (`StartsAtUtc`, `EndsAtUtc`, `Reason`, `CreatedByUserId`). Validates future end date and non-overlapping intervals via `Room.ScheduleMaintenance` and `CancelMaintenance`.

### 1.3 Wolverine Messaging over RabbitMQ
- **Conclusion:** Wolverine over RabbitMQ (`WolverineFx.RabbitMQ`) is used for outbound domain events connecting to `studium-rabbitmq-service.studium.svc.cluster.local:5672`:
  - `RoomMaintenanceEvent`: Published when maintenance is scheduled or cancelled. Dispatched to `studium-booking-events-queue`.
  - `OperatingHoursChangedEvent`: Published to `studium-booking-events-queue`.
  - `RoomStatusChangedEvent`: Published to `studium-booking-events-queue`.
  - `ResourceUpdatedEvent`: Published to `studium-booking-events-queue`.
- **Pre-generated Code:** Configured with `CritterStackDefaults` using `TypeLoadMode.Static` in Production and pre-generated via `codegen write` during Docker multi-stage build.
- **Legacy branch:** AWS SQS version preserved on branch `origin/sqs`.

---

## 2. Project Structure & File Map
- [`Studium-Catalog.sln`](./Studium-Catalog.sln) — Root solution file.
- [`Directory.Build.props`](./Directory.Build.props) & [`Directory.Packages.props`](./Directory.Packages.props) — CPM with `WolverineFx.RabbitMQ`.
- **`Domain/`:**
  - `Entities/Buildings/Building.cs` — Building entity and invariants.
  - `Entities/Rooms/Room.cs` — Room aggregate root.
  - `Entities/ResourceItems/ResourceItem.cs` — Equipment entity.
  - `Entities/AvailabilitySchedules/AvailabilitySchedule.cs` — Weekly operating window record.
  - `Entities/MaintenanceBlocks/MaintenanceBlock.cs` — Maintenance block entity.
  - `Exceptions/DomainException.cs` & `EntityNotFoundException.cs` — Domain exceptions.
  - `Common/UtcDateTime.cs` — UTC normalization helper.
- **`Data/`:**
  - `CatalogDbContext.cs` — DbContext applying Fluent API configurations.
  - `Configurations/BuildingConfig.cs`, `RoomConfig.cs`, `ResourceItemConfig.cs`, `AvailabilityScheduleConfig.cs`, `MaintenanceBlockConfig.cs` — Strict EF Core mappings.
  - `Migrations/20261108230207_Initial_Migration.cs` — EF Core migration for PostgreSQL.
- **`Services/`:**
  - `DTOs/` — Positional record DTOs (`BuildingDtos.cs`, `RoomDtos.cs`, `ResourceItemDtos.cs`, `ScheduleDtos.cs`, `EventDtos.cs`).
  - `CatalogEvents/CatalogEventFactory.cs` — Outbound event builder.
  - `Options/CatalogOptions.cs` — Timezone and catalog options.
- **`Api/`:**
  - `Program.cs` — Thin composition root with OpenAPI, Scalar, Wolverine RabbitMQ, and Health check.
  - `Controllers/BuildingsController.cs` & `RoomsController.cs` — REST controllers.
  - `Middleware/DomainExceptionHandler.cs` — ProblemDetails exception handler.
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
