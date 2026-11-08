using Studium_Catalog.Domain.Entities.MaintenanceBlocks;
using Studium_Catalog.Domain.Entities.ResourceItems;
using Studium_Catalog.Domain.Entities.Rooms;
using Studium_Catalog.Domain.Exceptions;
using Xunit;

namespace Studium_Catalog.Tests.Domain;

public class RoomTests
{
    [Fact]
    public void Constructor_WithValidData_InitializesRoomCorrectly()
    {
        var buildingId = Guid.NewGuid();
        var room = new Room(
            buildingId: buildingId,
            code: "  lab-204  ",
            name: "  Software Lab  ",
            type: RoomType.ComputerLab,
            capacity: 35,
            floor: 2,
            description: "Linux workstations");

        Assert.NotEqual(Guid.Empty, room.Id);
        Assert.Equal(buildingId, room.BuildingId);
        Assert.Equal("LAB-204", room.Code);
        Assert.Equal("Software Lab", room.Name);
        Assert.Equal(RoomType.ComputerLab, room.Type);
        Assert.Equal(35, room.Capacity);
        Assert.Equal(2, room.Floor);
        Assert.Equal("Linux workstations", room.Description);
        Assert.True(room.IsActive);
        Assert.Null(room.MapPositionX);
        Assert.Null(room.MapPositionY);
        Assert.Empty(room.Resources);
        Assert.Empty(room.OperatingHours);
        Assert.Empty(room.MaintenanceBlocks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1001)]
    public void UpdateDetails_WithInvalidCapacity_ThrowsDomainException(int capacity)
    {
        var room = CreateSampleRoom();
        var ex = Assert.Throws<DomainException>(() =>
            room.UpdateDetails("101", "Room", RoomType.Classroom, capacity, 1, null));

        Assert.Equal("INVALID_CAPACITY", ex.ErrorCode);
    }

    [Fact]
    public void SetMapPosition_WithValidCoordinates_SetsValues()
    {
        var room = CreateSampleRoom();
        room.SetMapPosition(45.5, 78.2);

        Assert.Equal(45.5, room.MapPositionX);
        Assert.Equal(78.2, room.MapPositionY);

        room.SetMapPosition(null, null);
        Assert.Null(room.MapPositionX);
        Assert.Null(room.MapPositionY);
    }

    [Theory]
    [InlineData(-0.1, 50.0)]
    [InlineData(100.1, 50.0)]
    [InlineData(50.0, -1.0)]
    [InlineData(50.0, 105.0)]
    public void SetMapPosition_WithCoordinatesOutOfRange_ThrowsDomainException(double x, double y)
    {
        var room = CreateSampleRoom();
        var ex = Assert.Throws<DomainException>(() => room.SetMapPosition(x, y));
        Assert.Equal("INVALID_MAP_POSITION", ex.ErrorCode);
    }

    [Fact]
    public void SetMapPosition_WithOnlyOneCoordinate_ThrowsDomainException()
    {
        var room = CreateSampleRoom();
        var ex = Assert.Throws<DomainException>(() => room.SetMapPosition(50.0, null));
        Assert.Equal("INVALID_MAP_POSITION", ex.ErrorCode);
    }

    [Fact]
    public void OperatingHours_WithOverlappingWindows_ThrowsDomainException()
    {
        var room = CreateSampleRoom();
        var windows = new List<(DayOfWeek, TimeOnly, TimeOnly)>
        {
            (DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)),
            (DayOfWeek.Monday, new TimeOnly(11, 30), new TimeOnly(18, 0))
        };

        var ex = Assert.Throws<DomainException>(() => room.SetOperatingHours(windows));
        Assert.Equal("OVERLAPPING_OPERATING_WINDOWS", ex.ErrorCode);
    }

    [Fact]
    public void OperatingHours_WithClosingBeforeOpening_ThrowsDomainException()
    {
        var room = CreateSampleRoom();
        var windows = new List<(DayOfWeek, TimeOnly, TimeOnly)>
        {
            (DayOfWeek.Monday, new TimeOnly(18, 0), new TimeOnly(8, 0))
        };

        var ex = Assert.Throws<DomainException>(() => room.SetOperatingHours(windows));
        Assert.Equal("INVALID_OPERATING_WINDOW", ex.ErrorCode);
    }

    [Fact]
    public void ScheduleMaintenance_WithValidPeriod_AddsBlock()
    {
        var room = CreateSampleRoom();
        var start = DateTime.UtcNow.AddHours(2);
        var end = DateTime.UtcNow.AddHours(5);

        var block = room.ScheduleMaintenance(start, end, "AC Repair", "admin-1");

        Assert.NotNull(block);
        Assert.Equal("AC Repair", block.Reason);
        Assert.Equal("admin-1", block.CreatedByUserId);
        Assert.Single(room.MaintenanceBlocks);
        Assert.True(room.IsUnderMaintenanceAt(start.AddMinutes(30)));
        Assert.False(room.IsUnderMaintenanceAt(start.AddMinutes(-30)));
    }

    [Fact]
    public void ScheduleMaintenance_WithOverlappingPeriod_ThrowsDomainException()
    {
        var room = CreateSampleRoom();
        var start1 = DateTime.UtcNow.AddHours(1);
        var end1 = DateTime.UtcNow.AddHours(4);
        room.ScheduleMaintenance(start1, end1, "First Maintenance", "admin-1");

        var start2 = DateTime.UtcNow.AddHours(2);
        var end2 = DateTime.UtcNow.AddHours(6);

        var ex = Assert.Throws<DomainException>(() =>
            room.ScheduleMaintenance(start2, end2, "Second Maintenance", "admin-1"));

        Assert.Equal("OVERLAPPING_MAINTENANCE", ex.ErrorCode);
    }

    [Fact]
    public void CancelMaintenance_WithActiveBlock_CancelsBlock()
    {
        var room = CreateSampleRoom();
        var start = DateTime.UtcNow.AddHours(1);
        var end = DateTime.UtcNow.AddHours(3);
        var block = room.ScheduleMaintenance(start, end, "Painting", "admin-1");

        room.CancelMaintenance(block.Id);

        Assert.True(block.IsCancelled);
        Assert.NotNull(block.CancelledAtUtc);
        Assert.False(room.IsUnderMaintenanceAt(start.AddMinutes(30)));
    }

    [Fact]
    public void ResourceManagement_AddUpdateRemove_WorksCorrectly()
    {
        var room = CreateSampleRoom();

        var resource = room.AddResource("Projector Epson", ResourceType.Projector, 1);
        Assert.Single(room.Resources);
        Assert.Equal("Projector Epson", resource.Name);

        room.UpdateResource(resource.Id, "Projector 4K", ResourceType.Projector, 2, ResourceStatus.Operational);
        Assert.Equal("Projector 4K", resource.Name);
        Assert.Equal(2, resource.Quantity);

        room.RemoveResource(resource.Id);
        Assert.Empty(room.Resources);
    }

    private static Room CreateSampleRoom() => new(
        buildingId: Guid.NewGuid(),
        code: "101",
        name: "Standard Classroom",
        type: RoomType.Classroom,
        capacity: 40,
        floor: 1);
}
