using Studium_Catalog.Domain.Entities.ResourceItems;
using Wolverine.Attributes;

namespace Studium_Catalog.Services.DTOs;

public enum RoomMaintenanceEventType
{
    MaintenanceScheduled,
    MaintenanceCancelled
}

public enum ResourceEventType
{
    ResourceAdded,
    ResourceUpdated,
    ResourceRemoved
}

/// <summary>
/// Outbound event published when a room maintenance block is scheduled or cancelled. Read-only.
/// </summary>
/// <param name="EventType">Whether the block was scheduled or cancelled. Read-only.</param>
/// <param name="RoomId">Identifier of the blocked room. Read-only.</param>
/// <param name="BuildingId">Identifier of the room's building. Read-only.</param>
/// <param name="RoomCode">Code of the blocked room. Read-only.</param>
/// <param name="RoomName">Name of the blocked room. Read-only.</param>
/// <param name="MaintenanceBlockId">Identifier of the maintenance block. Read-only.</param>
/// <param name="StartsAtUtc">UTC timestamp when the block starts. Read-only.</param>
/// <param name="EndsAtUtc">UTC timestamp when the block ends. Read-only.</param>
/// <param name="Reason">Justification for the block. Read-only.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the change occurred. Read-only.</param>
[MessageIdentity("studium.catalog.room-maintenance")]
public record RoomMaintenanceEvent(
    RoomMaintenanceEventType EventType,
    Guid RoomId,
    Guid BuildingId,
    string RoomCode,
    string RoomName,
    Guid MaintenanceBlockId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string Reason,
    DateTime OccurredAtUtc,
    Guid? EventId = null
);

/// <summary>
/// Outbound event published when a room's weekly operating grid is replaced. Read-only.
/// </summary>
/// <param name="RoomId">Identifier of the room. Read-only.</param>
/// <param name="TimeZone">IANA time zone in which the operating hours are expressed. Read-only.</param>
/// <param name="OperatingHours">New complete weekly operating grid. Read-only.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the change occurred. Read-only.</param>
/// <param name="EventId">Deterministic deduplication identifier for idempotent message consumers.</param>
[MessageIdentity("studium.catalog.operating-hours-changed")]
public record OperatingHoursChangedEvent(
    Guid RoomId,
    string TimeZone,
    IReadOnlyList<OperatingHoursDto> OperatingHours,
    DateTime OccurredAtUtc,
    Guid? EventId = null
);

/// <summary>
/// Outbound event published when a room is activated or deactivated. Read-only.
/// </summary>
/// <param name="RoomId">Identifier of the room. Read-only.</param>
/// <param name="IsActive">Whether the room now accepts reservations. Read-only.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the change occurred. Read-only.</param>
/// <param name="EventId">Deterministic deduplication identifier for idempotent message consumers.</param>
[MessageIdentity("studium.catalog.room-status-changed")]
public record RoomStatusChangedEvent(
    Guid RoomId,
    bool IsActive,
    DateTime OccurredAtUtc,
    Guid? EventId = null
);

/// <summary>
/// Outbound event published when room equipment is added, updated or removed. Read-only.
/// </summary>
/// <param name="EventType">Kind of change applied to the resource. Read-only.</param>
/// <param name="RoomId">Identifier of the room. Read-only.</param>
/// <param name="ResourceId">Identifier of the resource. Read-only.</param>
/// <param name="Name">Display name of the resource. Read-only.</param>
/// <param name="Type">Category of the resource. Read-only.</param>
/// <param name="Quantity">Number of identical units installed. Read-only.</param>
/// <param name="Status">Operational status of the resource. Read-only.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the change occurred. Read-only.</param>
/// <param name="EventId">Deterministic deduplication identifier for idempotent message consumers.</param>
[MessageIdentity("studium.catalog.resource-updated")]
public record ResourceUpdatedEvent(
    ResourceEventType EventType,
    Guid RoomId,
    Guid ResourceId,
    string Name,
    ResourceType Type,
    int Quantity,
    ResourceStatus Status,
    DateTime OccurredAtUtc,
    Guid? EventId = null
);

/// <summary>
/// Outbound message payload published to Studium-Audit for administrative catalog changes.
/// </summary>
[MessageIdentity("studium.audit.integration-event")]
public record AuditIntegrationEvent(
    string ServiceSource,
    string ResourceId,
    string ActorId,
    string Action,
    string? MetadataJson = null,
    DateTime? TimestampUtc = null,
    Guid? EventId = null
);
