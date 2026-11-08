# Studium-Catalog

Stack: `.NET 10.0` Web API + PostgreSQL (`studium_catalog`) + Amazon SQS (`WolverineFx.AmazonSqs`) + Amazon Cognito (`JwtBearer`) + Scalar API Reference (`/scalar/v1`).

## Scope
- **Campus Buildings & Spaces:** Manage buildings (`Building`) and rooms (`Room`) with capacity, floor, and categorization.
- **Floor-Plan Map Coordinates (UC021):** Pin coordinates `MapPositionX` and `MapPositionY` (0-100% of floor plan width/height).
- **Weekly Operating Hours (UC007):** Define weekly operating windows in local time (`America/Sao_Paulo`), ensuring non-overlapping intervals and chronological validation.
- **Temporary Maintenance Blocks (UC008):** Schedule and cancel temporary maintenance windows with overlap prevention and automatic status resolution.
- **Room Equipment & Resources:** Track room assets and operational status (`ResourceItem`).
- **Inter-Service Availability Contract:** Exposes `GET /api/rooms/{id}/availability-rules` for `Studium-Booking` to validate reservation constraints over internal Kubernetes ClusterIP.
- **Async Event Publishing:** Publishes `RoomMaintenanceEvent` (to booking and notifications queue with CloudEvents interop), `OperatingHoursChangedEvent`, `RoomStatusChangedEvent`, and `ResourceUpdatedEvent` over Amazon SQS.

## Local Environment Setup & Testing

### 1. Start PostgreSQL in Docker
```bash
docker run --name studium-catalog-db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=studium_catalog \
  -p 5432:5432 \
  -d postgres:16-alpine
```

### 2. Restore & Run Application
```bash
dotnet restore
dotnet run --project Api/Studium-Catalog.Api.csproj
```
Once running in `Development` mode, open the interactive API documentation at `http://localhost:5186/scalar/v1`.

### 3. Run Unit Tests
```bash
dotnet test
```
