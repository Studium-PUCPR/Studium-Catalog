# Studium-Catalog 

Stack: .NET + PostgreSQL (db-catalog).

Scope: Room CRUD (UC005), floor-plan pin coordinates (UC021), operating hour grids (UC007), and temporary maintenance blocks (UC008).   

REST + SNS Producer: Exposes GET http://studium-catalog-service/api/rooms/{id}/availability-rules for internal Kubernetes Service Discovery, and publishes RoomBlocked / OperatingHoursChanged to the SNS Topic (studium-catalog-events) whenever an admin blocks a room.
