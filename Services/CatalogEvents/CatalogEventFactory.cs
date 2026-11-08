using Mapster;
using Studium_Catalog.Domain.Entities.AvailabilitySchedules;
using Studium_Catalog.Domain.Entities.MaintenanceBlocks;
using Studium_Catalog.Domain.Entities.ResourceItems;
using Studium_Catalog.Domain.Entities.Rooms;
using Studium_Catalog.Services.DTOs;

namespace Studium_Catalog.Services.CatalogEvents;

/// <summary>
/// Builds outbound integration events from domain state.
/// </summary>
public static class CatalogEventFactory
{
    public static RoomMaintenanceEvent Maintenance(Room room, MaintenanceBlock block, RoomMaintenanceEventType eventType) =>
        new(
            EventType: eventType,
            RoomId: room.Id,
            BuildingId: room.BuildingId,
            RoomCode: room.Code,
            RoomName: room.Name,
            MaintenanceBlockId: block.Id,
            StartsAtUtc: block.StartsAtUtc,
            EndsAtUtc: block.EndsAtUtc,
            Reason: block.Reason,
            OccurredAtUtc: DateTime.UtcNow);

    public static OperatingHoursChangedEvent OperatingHoursChanged(
        Guid roomId,
        IEnumerable<AvailabilitySchedule> operatingHours,
        string timeZone) =>
        new(
            RoomId: roomId,
            TimeZone: timeZone,
            OperatingHours: operatingHours.Adapt<List<OperatingHoursDto>>(),
            OccurredAtUtc: DateTime.UtcNow);

    public static RoomStatusChangedEvent StatusChanged(Room room) =>
        new(room.Id, room.IsActive, DateTime.UtcNow);

    public static ResourceUpdatedEvent Resource(ResourceItem resource, ResourceEventType eventType) =>
        new(
            EventType: eventType,
            RoomId: resource.RoomId,
            ResourceId: resource.Id,
            Name: resource.Name,
            Type: resource.Type,
            Quantity: resource.Quantity,
            Status: resource.Status,
            OccurredAtUtc: DateTime.UtcNow);
}
