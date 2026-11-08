using Studium_Catalog.Domain.Common;
using Studium_Catalog.Domain.Entities.AvailabilitySchedules;
using Studium_Catalog.Domain.Entities.Buildings;
using Studium_Catalog.Domain.Entities.MaintenanceBlocks;
using Studium_Catalog.Domain.Entities.ResourceItems;
using Studium_Catalog.Domain.Exceptions;

namespace Studium_Catalog.Domain.Entities.Rooms;

public enum RoomType
{
    Classroom,
    Laboratory,
    ComputerLab,
    StudyRoom,
    MeetingRoom,
    Auditorium
}

/// <summary>
/// Bookable institutional space. Aggregate root for its resources, weekly operating hours and maintenance blocks.
/// </summary>
public class Room
{
    public const int MaxCapacity = 1000;

    /// <summary>
    /// Unique identifier of the room. Read-only.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Identifier of the building that contains the room. Read-only.
    /// </summary>
    public Guid BuildingId { get; init; }

    /// <summary>
    /// Building that contains the room.
    /// </summary>
    public Building Building { get; private set; } = null!;

    /// <summary>
    /// Room code, unique within its building.
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Display name of the room.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Category of the room.
    /// </summary>
    public RoomType Type { get; private set; }

    /// <summary>
    /// Maximum number of occupants.
    /// </summary>
    public int Capacity { get; private set; }

    /// <summary>
    /// Floor number inside the building (0 is the ground floor, negative values are basements).
    /// </summary>
    public int Floor { get; private set; }

    /// <summary>
    /// Optional free-text description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Horizontal pin position on the floor plan, as a percentage (0-100) of the image width.
    /// </summary>
    public double? MapPositionX { get; private set; }

    /// <summary>
    /// Vertical pin position on the floor plan, as a percentage (0-100) of the image height.
    /// </summary>
    public double? MapPositionY { get; private set; }

    /// <summary>
    /// Whether the room is listed and bookable. Inactive rooms are retired without deleting history.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// UTC timestamp when the room was registered. Read-only.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Equipment installed in the room.
    /// </summary>
    public List<ResourceItem> Resources { get; init; } = [];

    /// <summary>
    /// Weekly operating windows of the room.
    /// </summary>
    public List<AvailabilitySchedule> OperatingHours { get; init; } = [];

    /// <summary>
    /// Temporary maintenance blocks of the room.
    /// </summary>
    public List<MaintenanceBlock> MaintenanceBlocks { get; init; } = [];

    private Room() { }

    public Room(
        Guid buildingId,
        string code,
        string name,
        RoomType type,
        int capacity,
        int floor,
        string? description = null)
    {
        if (buildingId == Guid.Empty)
            throw new DomainException("INVALID_BUILDING_ID", "Building identifier is required.");

        Id = Guid.NewGuid();
        BuildingId = buildingId;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdateDetails(code, name, type, capacity, floor, description);
    }

    public void UpdateDetails(string code, string name, RoomType type, int capacity, int floor, string? description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("INVALID_ROOM_CODE", "Room code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("INVALID_ROOM_NAME", "Room name is required.");

        if (capacity is < 1 or > MaxCapacity)
            throw new DomainException("INVALID_CAPACITY", $"Room capacity must be between 1 and {MaxCapacity}.");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Type = type;
        Capacity = capacity;
        Floor = floor;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetMapPosition(double? x, double? y)
    {
        if (x.HasValue != y.HasValue)
            throw new DomainException("INVALID_MAP_POSITION", "Both map coordinates must be provided, or neither to clear the pin.");

        if (x is < 0 or > 100 || y is < 0 or > 100)
            throw new DomainException("INVALID_MAP_POSITION", "Map coordinates must be percentages between 0 and 100.");

        MapPositionX = x;
        MapPositionY = y;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public ResourceItem AddResource(string name, ResourceType type, int quantity, ResourceStatus status = ResourceStatus.Operational)
    {
        var resource = new ResourceItem(Id, name, type, quantity, status);
        Resources.Add(resource);
        return resource;
    }

    public ResourceItem UpdateResource(Guid resourceId, string name, ResourceType type, int quantity, ResourceStatus status)
    {
        var resource = FindResource(resourceId);
        resource.Update(name, type, quantity, status);
        return resource;
    }

    public ResourceItem RemoveResource(Guid resourceId)
    {
        var resource = FindResource(resourceId);
        Resources.Remove(resource);
        return resource;
    }

    /// <summary>
    /// Replaces the full weekly operating grid. Windows on the same day must not overlap.
    /// </summary>
    public IReadOnlyList<AvailabilitySchedule> SetOperatingHours(
        IEnumerable<(DayOfWeek DayOfWeek, TimeOnly OpensAt, TimeOnly ClosesAt)> windows)
    {
        var ordered = windows
            .OrderBy(w => w.DayOfWeek)
            .ThenBy(w => w.OpensAt)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var window = ordered[i];

            if (!Enum.IsDefined(window.DayOfWeek))
                throw new DomainException("INVALID_DAY_OF_WEEK", "Day of week is invalid.");

            if (window.ClosesAt <= window.OpensAt)
                throw new DomainException("INVALID_OPERATING_WINDOW", $"Closing time must be after opening time on {window.DayOfWeek}.");

            if (i > 0 && ordered[i - 1].DayOfWeek == window.DayOfWeek && ordered[i - 1].ClosesAt > window.OpensAt)
                throw new DomainException("OVERLAPPING_OPERATING_WINDOWS", $"Operating windows overlap on {window.DayOfWeek}.");
        }

        OperatingHours.Clear();
        OperatingHours.AddRange(ordered.Select(w =>
            new AvailabilitySchedule(Guid.NewGuid(), Id, w.DayOfWeek, w.OpensAt, w.ClosesAt)));

        return OperatingHours;
    }

    /// <summary>
    /// Blocks the room for maintenance. Requires pending/active <see cref="MaintenanceBlocks"/> to be loaded for overlap checks.
    /// </summary>
    public MaintenanceBlock ScheduleMaintenance(DateTime startsAtUtc, DateTime endsAtUtc, string reason, string createdByUserId)
    {
        var block = new MaintenanceBlock(Id, startsAtUtc, endsAtUtc, reason, createdByUserId);

        if (MaintenanceBlocks.Any(b => b.Overlaps(block.StartsAtUtc, block.EndsAtUtc)))
            throw new DomainException("OVERLAPPING_MAINTENANCE", "The room already has a maintenance block in this period.");

        MaintenanceBlocks.Add(block);
        return block;
    }

    public MaintenanceBlock CancelMaintenance(Guid maintenanceBlockId)
    {
        var block = MaintenanceBlocks.FirstOrDefault(b => b.Id == maintenanceBlockId)
            ?? throw new EntityNotFoundException(
                "MAINTENANCE_BLOCK_NOT_FOUND",
                $"Maintenance block '{maintenanceBlockId}' was not found in room '{Code}'.");

        block.Cancel();
        return block;
    }

    /// <summary>
    /// Whether a maintenance block is in effect at <paramref name="referenceUtc"/>.
    /// </summary>
    public bool IsUnderMaintenanceAt(DateTime referenceUtc)
    {
        var reference = UtcDateTime.Normalize(referenceUtc);
        return MaintenanceBlocks.Any(b => b.Overlaps(reference, reference.AddTicks(1)));
    }

    private ResourceItem FindResource(Guid resourceId) =>
        Resources.FirstOrDefault(r => r.Id == resourceId)
        ?? throw new EntityNotFoundException(
            "RESOURCE_NOT_FOUND",
            $"Resource '{resourceId}' was not found in room '{Code}'.");
}
