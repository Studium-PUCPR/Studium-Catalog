using Studium_Catalog.Domain.Entities.ResourceItems;

namespace Studium_Catalog.Services.DTOs;

/// <summary>
/// Request payload for adding or updating a room resource. Read-only.
/// </summary>
/// <param name="Name">Display name of the resource. Read-only.</param>
/// <param name="Type">Category of the resource. Read-only.</param>
/// <param name="Quantity">Number of identical units installed. Read-only.</param>
/// <param name="Status">Current operational status. Read-only.</param>
public record ResourceItemRequest(
    string Name,
    ResourceType Type,
    int Quantity = 1,
    ResourceStatus Status = ResourceStatus.Operational
);

/// <summary>
/// Representation of a room resource. Read-only.
/// </summary>
/// <param name="Id">Unique resource identifier. Read-only.</param>
/// <param name="Name">Display name of the resource. Read-only.</param>
/// <param name="Type">Category of the resource. Read-only.</param>
/// <param name="Quantity">Number of identical units installed. Read-only.</param>
/// <param name="Status">Current operational status. Read-only.</param>
public record ResourceItemDto(
    Guid Id,
    string Name,
    ResourceType Type,
    int Quantity,
    ResourceStatus Status
);
