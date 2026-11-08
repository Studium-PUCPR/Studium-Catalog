using Studium_Catalog.Domain.Exceptions;

namespace Studium_Catalog.Domain.Entities.ResourceItems;

public enum ResourceType
{
    Projector,
    Computer,
    Whiteboard,
    Television,
    AudioSystem,
    VideoConference,
    AirConditioning,
    LabEquipment,
    Other
}

public enum ResourceStatus
{
    Operational,
    UnderMaintenance,
    OutOfService
}

/// <summary>
/// Equipment or asset installed in a room. Mutated exclusively through its owning <c>Room</c> aggregate.
/// </summary>
public class ResourceItem
{
    /// <summary>
    /// Unique identifier of the resource. Read-only.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Identifier of the room where the resource is installed. Read-only.
    /// </summary>
    public Guid RoomId { get; init; }

    /// <summary>
    /// Display name of the resource.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Category of the resource.
    /// </summary>
    public ResourceType Type { get; private set; }

    /// <summary>
    /// Number of identical units installed in the room.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Current operational status.
    /// </summary>
    public ResourceStatus Status { get; private set; }

    private ResourceItem() { }

    internal ResourceItem(Guid roomId, string name, ResourceType type, int quantity, ResourceStatus status)
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        Update(name, type, quantity, status);
    }

    internal void Update(string name, ResourceType type, int quantity, ResourceStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("INVALID_RESOURCE_NAME", "Resource name is required.");

        if (quantity < 1)
            throw new DomainException("INVALID_RESOURCE_QUANTITY", "Resource quantity must be at least 1.");

        Name = name.Trim();
        Type = type;
        Quantity = quantity;
        Status = status;
    }
}
