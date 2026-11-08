using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studium_Catalog.Data;
using Studium_Catalog.Domain.Entities.Buildings;
using Studium_Catalog.Services.DTOs;

namespace Studium_Catalog.Api.Controllers;

[ApiController]
[Route("api/catalog/buildings")]
public class BuildingsController(CatalogDbContext db) : ControllerBase
{
    /// <summary>
    /// Lists all buildings ordered by code, with their registered room count.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BuildingDto>>> ListBuildings(CancellationToken ct)
    {
        var buildings = await db.Buildings
            .AsNoTracking()
            .OrderBy(b => b.Code)
            .Select(b => new BuildingDto(
                b.Id,
                b.Code,
                b.Name,
                b.Address,
                b.Description,
                b.Rooms.Count,
                b.CreatedAtUtc))
            .ToListAsync(ct);

        return Ok(buildings);
    }

    /// <summary>
    /// Returns a specific building by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BuildingDto>> GetBuildingById(Guid id, CancellationToken ct)
    {
        var building = await db.Buildings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BuildingDto(
                b.Id,
                b.Code,
                b.Name,
                b.Address,
                b.Description,
                b.Rooms.Count,
                b.CreatedAtUtc))
            .FirstOrDefaultAsync(ct);

        if (building is null)
            return NotFound(new { error = "BUILDING_NOT_FOUND", message = $"Building '{id}' was not found." });

        return Ok(building);
    }

    /// <summary>
    /// Registers a new building.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BuildingDto>> CreateBuilding(
        [FromBody] BuildingRequest request,
        CancellationToken ct)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var exists = await db.Buildings.AnyAsync(b => b.Code == normalizedCode, ct);
        if (exists)
        {
            return Conflict(new
            {
                error = "DUPLICATE_BUILDING_CODE",
                message = $"A building with code '{normalizedCode}' already exists."
            });
        }

        var building = new Building(request.Code, request.Name, request.Address, request.Description);
        db.Buildings.Add(building);
        await db.SaveChangesAsync(ct);

        var dto = new BuildingDto(
            building.Id,
            building.Code,
            building.Name,
            building.Address,
            building.Description,
            0,
            building.CreatedAtUtc);

        return CreatedAtAction(nameof(GetBuildingById), new { id = building.Id }, dto);
    }

    /// <summary>
    /// Updates a building's descriptive details.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BuildingDto>> UpdateBuilding(
        Guid id,
        [FromBody] BuildingRequest request,
        CancellationToken ct)
    {
        var building = await db.Buildings
            .Include(b => b.Rooms)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (building is null)
            return NotFound(new { error = "BUILDING_NOT_FOUND", message = $"Building '{id}' was not found." });

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var codeInUse = await db.Buildings.AnyAsync(b => b.Id != id && b.Code == normalizedCode, ct);
        if (codeInUse)
        {
            return Conflict(new
            {
                error = "DUPLICATE_BUILDING_CODE",
                message = $"A building with code '{normalizedCode}' already exists."
            });
        }

        building.Update(request.Code, request.Name, request.Address, request.Description);
        await db.SaveChangesAsync(ct);

        return Ok(new BuildingDto(
            building.Id,
            building.Code,
            building.Name,
            building.Address,
            building.Description,
            building.Rooms.Count,
            building.CreatedAtUtc));
    }

    /// <summary>
    /// Removes a building if it does not contain any rooms.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteBuilding(Guid id, CancellationToken ct)
    {
        var building = await db.Buildings
            .Include(b => b.Rooms)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (building is null)
            return NotFound(new { error = "BUILDING_NOT_FOUND", message = $"Building '{id}' was not found." });

        building.EnsureCanBeDeleted();

        db.Buildings.Remove(building);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
