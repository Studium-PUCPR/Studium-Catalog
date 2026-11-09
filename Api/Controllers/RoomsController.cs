using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Studium_Catalog.Api.Extensions;
using Studium_Catalog.Data;
using Studium_Catalog.Domain.Entities.MaintenanceBlocks;
using Studium_Catalog.Domain.Entities.ResourceItems;
using Studium_Catalog.Domain.Entities.Rooms;
using Studium_Catalog.Services.CatalogEvents;
using Studium_Catalog.Services.DTOs;
using Studium_Catalog.Services.Options;
using Wolverine;

namespace Studium_Catalog.Api.Controllers;

[ApiController]
[Route("api/catalog/rooms")]
public class RoomsController(
    CatalogDbContext db,
    IMessageBus bus,
    IOptions<CatalogOptions> options) : ControllerBase
{
    private readonly CatalogOptions _options = options.Value;

    /// <summary>
    /// Paginated search, filter, and floor-plan pin listing of rooms.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<RoomListItemDto>>> ListRooms(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] int? floor = null,
        [FromQuery] RoomType? type = null,
        [FromQuery] int? minCapacity = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Rooms.AsNoTracking().AsQueryable();

        if (buildingId.HasValue)
            query = query.Where(r => r.BuildingId == buildingId.Value);

        if (floor.HasValue)
            query = query.Where(r => r.Floor == floor.Value);

        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);

        if (minCapacity.HasValue)
            query = query.Where(r => r.Capacity >= minCapacity.Value);

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.Name, pattern) ||
                EF.Functions.ILike(r.Code, pattern) ||
                EF.Functions.ILike(r.Building.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderBy(r => r.Building.Code)
            .ThenBy(r => r.Floor)
            .ThenBy(r => r.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoomListItemDto(
                r.Id,
                r.BuildingId,
                r.Building.Code,
                r.Building.Name,
                r.Code,
                r.Name,
                r.Type,
                r.Capacity,
                r.Floor,
                r.MapPositionX,
                r.MapPositionY,
                r.IsActive))
            .ToListAsync(ct);

        return Ok(new PagedResponse<RoomListItemDto>(items, page, pageSize, totalCount, totalPages));
    }

    /// <summary>
    /// Returns full details of a room, including resources, operating hours and active maintenance blocks.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomDetailDto>> GetRoomById(Guid id, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var room = await db.Rooms
            .AsNoTracking()
            .Include(r => r.Building)
            .Include(r => r.Resources)
            .Include(r => r.OperatingHours)
            .Include(r => r.MaintenanceBlocks.Where(m => m.CancelledAtUtc == null && m.EndsAtUtc > now))
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        return Ok(ToDetailDto(room));
    }

    /// <summary>
    /// Registers a new room inside a building.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDetailDto>> CreateRoom(
        [FromBody] CreateRoomRequest request,
        CancellationToken ct)
    {
        var building = await db.Buildings.FirstOrDefaultAsync(b => b.Id == request.BuildingId, ct);
        if (building is null)
            return NotFound(new { error = "BUILDING_NOT_FOUND", message = $"Building '{request.BuildingId}' was not found." });

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var codeInUse = await db.Rooms.AnyAsync(r => r.BuildingId == request.BuildingId && r.Code == normalizedCode, ct);
        if (codeInUse)
        {
            return Conflict(new
            {
                error = "DUPLICATE_ROOM_CODE",
                message = $"A room with code '{normalizedCode}' already exists in building '{building.Code}'."
            });
        }

        var room = new Room(
            request.BuildingId,
            request.Code,
            request.Name,
            request.Type,
            request.Capacity,
            request.Floor,
            request.Description);

        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(new AuditIntegrationEvent(
            ServiceSource: "Catalog",
            ResourceId: room.Id.ToString(),
            ActorId: User.GetUserId(),
            Action: "RoomCreated",
            MetadataJson: $"{{\"code\":\"{room.Code}\",\"name\":\"{room.Name}\",\"buildingId\":\"{building.Id}\"}}",
            TimestampUtc: DateTime.UtcNow,
            EventId: Guid.NewGuid()));

        var dto = new RoomDetailDto(
            room.Id,
            building.Id,
            building.Code,
            building.Name,
            room.Code,
            room.Name,
            room.Type,
            room.Capacity,
            room.Floor,
            room.Description,
            room.MapPositionX,
            room.MapPositionY,
            room.IsActive,
            room.CreatedAtUtc,
            [],
            [],
            []);

        return CreatedAtAction(nameof(GetRoomById), new { id = room.Id }, dto);
    }

    /// <summary>
    /// Updates room details (code, name, type, capacity, floor, description).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDetailDto>> UpdateRoom(
        Guid id,
        [FromBody] UpdateRoomRequest request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var room = await db.Rooms
            .Include(r => r.Building)
            .Include(r => r.Resources)
            .Include(r => r.OperatingHours)
            .Include(r => r.MaintenanceBlocks.Where(m => m.CancelledAtUtc == null && m.EndsAtUtc > now))
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var codeInUse = await db.Rooms.AnyAsync(r => r.BuildingId == room.BuildingId && r.Id != id && r.Code == normalizedCode, ct);
        if (codeInUse)
        {
            return Conflict(new
            {
                error = "DUPLICATE_ROOM_CODE",
                message = $"A room with code '{normalizedCode}' already exists in building '{room.Building.Code}'."
            });
        }

        room.UpdateDetails(request.Code, request.Name, request.Type, request.Capacity, request.Floor, request.Description);
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(new AuditIntegrationEvent(
            ServiceSource: "Catalog",
            ResourceId: room.Id.ToString(),
            ActorId: User.GetUserId(),
            Action: "RoomUpdated",
            MetadataJson: $"{{\"code\":\"{room.Code}\",\"name\":\"{room.Name}\"}}",
            TimestampUtc: DateTime.UtcNow,
            EventId: Guid.NewGuid()));

        return Ok(ToDetailDto(room));
    }

    /// <summary>
    /// Updates the floor plan pin coordinates (UC021). Pass null coordinates to clear the pin.
    /// </summary>
    [HttpPatch("{id:guid}/map-position")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDetailDto>> UpdateMapPosition(
        Guid id,
        [FromBody] MapPositionRequest request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var room = await db.Rooms
            .Include(r => r.Building)
            .Include(r => r.Resources)
            .Include(r => r.OperatingHours)
            .Include(r => r.MaintenanceBlocks.Where(m => m.CancelledAtUtc == null && m.EndsAtUtc > now))
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        room.SetMapPosition(request.X, request.Y);
        await db.SaveChangesAsync(ct);

        return Ok(ToDetailDto(room));
    }

    /// <summary>
    /// Activates or deactivates a room. Publishes RoomStatusChangedEvent.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDetailDto>> UpdateStatus(
        Guid id,
        [FromBody] RoomStatusRequest request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var room = await db.Rooms
            .Include(r => r.Building)
            .Include(r => r.Resources)
            .Include(r => r.OperatingHours)
            .Include(r => r.MaintenanceBlocks.Where(m => m.CancelledAtUtc == null && m.EndsAtUtc > now))
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        if (request.IsActive)
            room.Activate();
        else
            room.Deactivate();

        await db.SaveChangesAsync(ct);
        await bus.PublishAsync(CatalogEventFactory.StatusChanged(room));

        await bus.PublishAsync(new AuditIntegrationEvent(
            ServiceSource: "Catalog",
            ResourceId: room.Id.ToString(),
            ActorId: User.GetUserId(),
            Action: room.IsActive ? "RoomActivated" : "RoomDeactivated",
            MetadataJson: $"{{\"isActive\":{room.IsActive.ToString().ToLowerInvariant()}}}",
            TimestampUtc: DateTime.UtcNow,
            EventId: Guid.NewGuid()));

        return Ok(ToDetailDto(room));
    }

    /// <summary>
    /// Replaces the entire weekly operating hours grid for a room (UC007). Publishes OperatingHoursChangedEvent.
    /// </summary>
    [HttpPut("{id:guid}/operating-hours")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IReadOnlyList<OperatingHoursDto>>> SetOperatingHours(
        Guid id,
        [FromBody] SetOperatingHoursRequest request,
        CancellationToken ct)
    {
        var room = await db.Rooms
            .Include(r => r.OperatingHours)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        var windows = request.OperatingHours
            .Select(h => (h.DayOfWeek, h.OpensAt, h.ClosesAt))
            .ToList();

        var updated = room.SetOperatingHours(windows);
        await db.SaveChangesAsync(ct);

        var eventPayload = CatalogEventFactory.OperatingHoursChanged(room.Id, updated, _options.TimeZone);
        await bus.PublishAsync(eventPayload);

        var result = updated
            .OrderBy(h => h.DayOfWeek)
            .ThenBy(h => h.OpensAt)
            .Select(h => new OperatingHoursDto(h.DayOfWeek, h.OpensAt, h.ClosesAt))
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Schedules a temporary maintenance block (UC008). Publishes RoomMaintenanceEvent.
    /// </summary>
    [HttpPost("{id:guid}/maintenance-blocks")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MaintenanceBlockDto>> ScheduleMaintenance(
        Guid id,
        [FromBody] MaintenanceBlockRequest request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var room = await db.Rooms
            .Include(r => r.MaintenanceBlocks.Where(m => m.CancelledAtUtc == null && m.EndsAtUtc > now))
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        var adminUserId = User.GetUserId();
        var block = room.ScheduleMaintenance(request.StartsAtUtc, request.EndsAtUtc, request.Reason, adminUserId);

        await db.SaveChangesAsync(ct);

        var eventPayload = CatalogEventFactory.Maintenance(room, block, RoomMaintenanceEventType.MaintenanceScheduled);
        await bus.PublishAsync(eventPayload);

        await bus.PublishAsync(new AuditIntegrationEvent(
            ServiceSource: "Catalog",
            ResourceId: room.Id.ToString(),
            ActorId: adminUserId,
            Action: "MaintenanceScheduled",
            MetadataJson: $"{{\"blockId\":\"{block.Id}\",\"startsAtUtc\":\"{block.StartsAtUtc:O}\",\"endsAtUtc\":\"{block.EndsAtUtc:O}\",\"reason\":\"{block.Reason}\"}}",
            TimestampUtc: DateTime.UtcNow,
            EventId: Guid.NewGuid()));

        return CreatedAtAction(
            nameof(GetRoomById),
            new { id = room.Id },
            ToMaintenanceDto(block));
    }

    /// <summary>
    /// Cancels an active or pending maintenance block. Publishes RoomMaintenanceEvent.
    /// </summary>
    [HttpDelete("{id:guid}/maintenance-blocks/{blockId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MaintenanceBlockDto>> CancelMaintenance(
        Guid id,
        Guid blockId,
        CancellationToken ct)
    {
        var room = await db.Rooms
            .Include(r => r.MaintenanceBlocks)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        var block = room.CancelMaintenance(blockId);
        await db.SaveChangesAsync(ct);

        var eventPayload = CatalogEventFactory.Maintenance(room, block, RoomMaintenanceEventType.MaintenanceCancelled);
        await bus.PublishAsync(eventPayload);

        await bus.PublishAsync(new AuditIntegrationEvent(
            ServiceSource: "Catalog",
            ResourceId: room.Id.ToString(),
            ActorId: User.GetUserId(),
            Action: "MaintenanceCancelled",
            MetadataJson: $"{{\"blockId\":\"{block.Id}\"}}",
            TimestampUtc: DateTime.UtcNow,
            EventId: Guid.NewGuid()));

        return Ok(ToMaintenanceDto(block));
    }

    /// <summary>
    /// Adds equipment to a room. Publishes ResourceUpdatedEvent.
    /// </summary>
    [HttpPost("{id:guid}/resources")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ResourceItemDto>> AddResource(
        Guid id,
        [FromBody] ResourceItemRequest request,
        CancellationToken ct)
    {
        var room = await db.Rooms
            .Include(r => r.Resources)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        var resource = room.AddResource(request.Name, request.Type, request.Quantity, request.Status);
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(CatalogEventFactory.Resource(resource, ResourceEventType.ResourceAdded));

        var dto = new ResourceItemDto(resource.Id, resource.Name, resource.Type, resource.Quantity, resource.Status);
        return CreatedAtAction(nameof(GetRoomById), new { id = room.Id }, dto);
    }

    /// <summary>
    /// Updates equipment details or operational status in a room. Publishes ResourceUpdatedEvent.
    /// </summary>
    [HttpPut("{id:guid}/resources/{resourceId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ResourceItemDto>> UpdateResource(
        Guid id,
        Guid resourceId,
        [FromBody] ResourceItemRequest request,
        CancellationToken ct)
    {
        var room = await db.Rooms
            .Include(r => r.Resources)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        var resource = room.UpdateResource(resourceId, request.Name, request.Type, request.Quantity, request.Status);
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(CatalogEventFactory.Resource(resource, ResourceEventType.ResourceUpdated));

        return Ok(new ResourceItemDto(resource.Id, resource.Name, resource.Type, resource.Quantity, resource.Status));
    }

    /// <summary>
    /// Removes equipment from a room. Publishes ResourceUpdatedEvent.
    /// </summary>
    [HttpDelete("{id:guid}/resources/{resourceId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RemoveResource(
        Guid id,
        Guid resourceId,
        CancellationToken ct)
    {
        var room = await db.Rooms
            .Include(r => r.Resources)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is null)
            return NotFound(new { error = "ROOM_NOT_FOUND", message = $"Room '{id}' was not found." });

        var resource = room.RemoveResource(resourceId);
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(CatalogEventFactory.Resource(resource, ResourceEventType.ResourceRemoved));

        return NoContent();
    }

    private static RoomDetailDto ToDetailDto(Room room) => new(
        room.Id,
        room.BuildingId,
        room.Building?.Code ?? string.Empty,
        room.Building?.Name ?? string.Empty,
        room.Code,
        room.Name,
        room.Type,
        room.Capacity,
        room.Floor,
        room.Description,
        room.MapPositionX,
        room.MapPositionY,
        room.IsActive,
        room.CreatedAtUtc,
        room.Resources
            .OrderBy(r => r.Name)
            .Select(r => new ResourceItemDto(r.Id, r.Name, r.Type, r.Quantity, r.Status))
            .ToList(),
        room.OperatingHours
            .OrderBy(h => h.DayOfWeek)
            .ThenBy(h => h.OpensAt)
            .Select(h => new OperatingHoursDto(h.DayOfWeek, h.OpensAt, h.ClosesAt))
            .ToList(),
        room.MaintenanceBlocks
            .OrderBy(m => m.StartsAtUtc)
            .Select(ToMaintenanceDto)
            .ToList());

    private static MaintenanceBlockDto ToMaintenanceDto(MaintenanceBlock m) => new(
        m.Id,
        m.StartsAtUtc,
        m.EndsAtUtc,
        m.Reason,
        m.CreatedByUserId,
        m.CreatedAtUtc,
        m.CancelledAtUtc);
}
