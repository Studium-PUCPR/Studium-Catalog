using Studium_Catalog.Domain.Entities.Rooms;

namespace Studium_Catalog.Services.DTOs;

/// <summary>
/// Request payload for registering a room. Read-only.
/// </summary>
/// <param name="BuildingId">Identifier of the building that contains the room. Read-only.</param>
/// <param name="Code">Room code, unique within its building. Read-only.</param>
/// <param name="Name">Display name of the room. Read-only.</param>
/// <param name="Type">Category of the room. Read-only.</param>
/// <param name="Capacity">Maximum number of occupants. Read-only.</param>
/// <param name="Floor">Floor number inside the building. Read-only.</param>
/// <param name="Description">Optional free-text description. Read-only.</param>
public record CreateRoomRequest(
    Guid BuildingId,
    string Code,
    string Name,
    RoomType Type,
    int Capacity,
    int Floor,
    string? Description = null
);

/// <summary>
/// Request payload for updating a room's descriptive fields. Read-only.
/// </summary>
/// <param name="Code">Room code, unique within its building. Read-only.</param>
/// <param name="Name">Display name of the room. Read-only.</param>
/// <param name="Type">Category of the room. Read-only.</param>
/// <param name="Capacity">Maximum number of occupants. Read-only.</param>
/// <param name="Floor">Floor number inside the building. Read-only.</param>
/// <param name="Description">Optional free-text description. Read-only.</param>
public record UpdateRoomRequest(
    string Code,
    string Name,
    RoomType Type,
    int Capacity,
    int Floor,
    string? Description = null
);

/// <summary>
/// Request payload for activating or deactivating a room. Read-only.
/// </summary>
/// <param name="IsActive">Whether the room should be listed and bookable. Read-only.</param>
public record RoomStatusRequest(bool IsActive);

/// <summary>
/// Request payload for placing (or clearing, when both are null) the room pin on its floor plan. Read-only.
/// </summary>
/// <param name="X">Horizontal position as a percentage (0-100) of the floor plan width. Read-only.</param>
/// <param name="Y">Vertical position as a percentage (0-100) of the floor plan height. Read-only.</param>
public record MapPositionRequest(double? X, double? Y);

/// <summary>
/// Summary representation of a room for listings and floor-plan pins. Read-only.
/// </summary>
/// <param name="Id">Unique room identifier. Read-only.</param>
/// <param name="BuildingId">Identifier of the building. Read-only.</param>
/// <param name="BuildingCode">Code of the building. Read-only.</param>
/// <param name="BuildingName">Name of the building. Read-only.</param>
/// <param name="Code">Room code. Read-only.</param>
/// <param name="Name">Display name of the room. Read-only.</param>
/// <param name="Type">Category of the room. Read-only.</param>
/// <param name="Capacity">Maximum number of occupants. Read-only.</param>
/// <param name="Floor">Floor number inside the building. Read-only.</param>
/// <param name="MapPositionX">Horizontal floor-plan pin position (0-100). Read-only.</param>
/// <param name="MapPositionY">Vertical floor-plan pin position (0-100). Read-only.</param>
/// <param name="IsActive">Whether the room is listed and bookable. Read-only.</param>
public record RoomListItemDto(
    Guid Id,
    Guid BuildingId,
    string BuildingCode,
    string BuildingName,
    string Code,
    string Name,
    RoomType Type,
    int Capacity,
    int Floor,
    double? MapPositionX,
    double? MapPositionY,
    bool IsActive
);

/// <summary>
/// Detailed representation of a room including equipment, operating hours and pending maintenance. Read-only.
/// </summary>
/// <param name="Id">Unique room identifier. Read-only.</param>
/// <param name="BuildingId">Identifier of the building. Read-only.</param>
/// <param name="BuildingCode">Code of the building. Read-only.</param>
/// <param name="BuildingName">Name of the building. Read-only.</param>
/// <param name="Code">Room code. Read-only.</param>
/// <param name="Name">Display name of the room. Read-only.</param>
/// <param name="Type">Category of the room. Read-only.</param>
/// <param name="Capacity">Maximum number of occupants. Read-only.</param>
/// <param name="Floor">Floor number inside the building. Read-only.</param>
/// <param name="Description">Free-text description. Read-only.</param>
/// <param name="MapPositionX">Horizontal floor-plan pin position (0-100). Read-only.</param>
/// <param name="MapPositionY">Vertical floor-plan pin position (0-100). Read-only.</param>
/// <param name="IsActive">Whether the room is listed and bookable. Read-only.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the room was registered. Read-only.</param>
/// <param name="Resources">Equipment installed in the room. Read-only.</param>
/// <param name="OperatingHours">Weekly operating windows in local time. Read-only.</param>
/// <param name="MaintenanceBlocks">Pending or active maintenance blocks. Read-only.</param>
public record RoomDetailDto(
    Guid Id,
    Guid BuildingId,
    string BuildingCode,
    string BuildingName,
    string Code,
    string Name,
    RoomType Type,
    int Capacity,
    int Floor,
    string? Description,
    double? MapPositionX,
    double? MapPositionY,
    bool IsActive,
    DateTime CreatedAtUtc,
    IReadOnlyList<ResourceItemDto> Resources,
    IReadOnlyList<OperatingHoursDto> OperatingHours,
    IReadOnlyList<MaintenanceBlockDto> MaintenanceBlocks
);

/// <summary>
/// Generic paginated response wrapper. Read-only.
/// </summary>
/// <typeparam name="T">Type of items in the current page.</typeparam>
/// <param name="Items">Collection of items on the requested page. Read-only.</param>
/// <param name="Page">Current 1-based page number. Read-only.</param>
/// <param name="PageSize">Maximum number of items per page. Read-only.</param>
/// <param name="TotalCount">Total number of matching items across all pages. Read-only.</param>
/// <param name="TotalPages">Total number of available pages. Read-only.</param>
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
