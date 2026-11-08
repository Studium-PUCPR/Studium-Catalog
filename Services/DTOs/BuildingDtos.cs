namespace Studium_Catalog.Services.DTOs;

/// <summary>
/// Request payload for creating or updating a building. Read-only.
/// </summary>
/// <param name="Code">Short institutional code, unique across the campus. Read-only.</param>
/// <param name="Name">Display name of the building. Read-only.</param>
/// <param name="Address">Optional street address or campus location reference. Read-only.</param>
/// <param name="Description">Optional free-text description. Read-only.</param>
public record BuildingRequest(
    string Code,
    string Name,
    string? Address = null,
    string? Description = null
);

/// <summary>
/// Representation of a building. Read-only.
/// </summary>
/// <param name="Id">Unique building identifier. Read-only.</param>
/// <param name="Code">Short institutional code. Read-only.</param>
/// <param name="Name">Display name of the building. Read-only.</param>
/// <param name="Address">Street address or campus location reference. Read-only.</param>
/// <param name="Description">Free-text description. Read-only.</param>
/// <param name="RoomsCount">Number of rooms registered in the building. Read-only.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the building was registered. Read-only.</param>
public record BuildingDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string? Description,
    int RoomsCount,
    DateTime CreatedAtUtc
);
