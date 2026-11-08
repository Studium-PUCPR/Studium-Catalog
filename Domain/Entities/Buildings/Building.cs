using Studium_Catalog.Domain.Entities.Rooms;
using Studium_Catalog.Domain.Exceptions;

namespace Studium_Catalog.Domain.Entities.Buildings;

public class Building
{
    /// <summary>
    /// Unique identifier of the building. Read-only.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Short institutional code, unique across the campus.
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Display name of the building.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Optional street address or campus location reference.
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Optional free-text description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// UTC timestamp when the building was registered. Read-only.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Rooms located inside this building.
    /// </summary>
    public List<Room> Rooms { get; init; } = [];

    private Building() { }

    public Building(string code, string name, string? address = null, string? description = null)
    {
        Id = Guid.NewGuid();
        CreatedAtUtc = DateTime.UtcNow;
        Update(code, name, address, description);
    }

    public void Update(string code, string name, string? address, string? description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("INVALID_BUILDING_CODE", "Building code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("INVALID_BUILDING_NAME", "Building name is required.");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Ensures the building can be removed. Requires <see cref="Rooms"/> to be loaded.
    /// </summary>
    public void EnsureCanBeDeleted()
    {
        if (Rooms.Count > 0)
            throw new DomainException("BUILDING_HAS_ROOMS", "Buildings that still contain rooms cannot be deleted.");
    }
}
