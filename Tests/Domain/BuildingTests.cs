using Studium_Catalog.Domain.Entities.Buildings;
using Studium_Catalog.Domain.Exceptions;
using Xunit;

namespace Studium_Catalog.Tests.Domain;

public class BuildingTests
{
    [Fact]
    public void Constructor_WithValidArguments_NormalizesCodeAndCreatesBuilding()
    {
        var building = new Building("  ct-1  ", "  Central Technology  ", "  Rua Imaculada Conceicao, 1155  ", "  Main engineering complex  ");

        Assert.NotEqual(Guid.Empty, building.Id);
        Assert.Equal("CT-1", building.Code);
        Assert.Equal("Central Technology", building.Name);
        Assert.Equal("Rua Imaculada Conceicao, 1155", building.Address);
        Assert.Equal("Main engineering complex", building.Description);
        Assert.Empty(building.Rooms);
    }

    [Theory]
    [InlineData("", "Building", "INVALID_BUILDING_CODE")]
    [InlineData("   ", "Building", "INVALID_BUILDING_CODE")]
    [InlineData("B-1", "", "INVALID_BUILDING_NAME")]
    [InlineData("B-1", "   ", "INVALID_BUILDING_NAME")]
    public void Update_WithInvalidArguments_ThrowsDomainException(string code, string name, string expectedErrorCode)
    {
        var building = new Building("B-1", "Initial Name");

        var ex = Assert.Throws<DomainException>(() => building.Update(code, name, null, null));
        Assert.Equal(expectedErrorCode, ex.ErrorCode);
    }

    [Fact]
    public void EnsureCanBeDeleted_WhenBuildingHasRooms_ThrowsDomainException()
    {
        var building = new Building("B-1", "Building 1");
        var room = new Studium_Catalog.Domain.Entities.Rooms.Room(
            building.Id,
            "101",
            "Classroom 101",
            Studium_Catalog.Domain.Entities.Rooms.RoomType.Classroom,
            40,
            1);
        building.Rooms.Add(room);

        var ex = Assert.Throws<DomainException>(() => building.EnsureCanBeDeleted());
        Assert.Equal("BUILDING_HAS_ROOMS", ex.ErrorCode);
    }
}
