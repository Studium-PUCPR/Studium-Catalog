using Mapster;
using Studium_Catalog.Domain.Entities.Buildings;
using Studium_Catalog.Domain.Entities.Rooms;
using Studium_Catalog.Services.CatalogEvents;
using Studium_Catalog.Services.DTOs;
using Xunit;

namespace Studium_Catalog.Tests.DTOs;

public class DtoMappingTests
{
    [Fact]
    public void Mapster_AdaptsBuildingAndRoomEntities()
    {
        var building = new Building("CT", "Centro Tecnológico", "Rua Imaculada", "Campus Curitiba");
        var room = new Room(building.Id, "301", "Lab Multiuso", RoomType.Laboratory, 50, 3, "Equipado");
        room.SetMapPosition(30.0, 45.0);

        var resource = room.AddResource("Microscope", Studium_Catalog.Domain.Entities.ResourceItems.ResourceType.LabEquipment, 10);
        var resDto = resource.Adapt<ResourceItemDto>();
        Assert.Equal(resource.Id, resDto.Id);
        Assert.Equal("Microscope", resDto.Name);
        Assert.Equal(10, resDto.Quantity);

        var eventPayload = CatalogEventFactory.StatusChanged(room);
        Assert.Equal(room.Id, eventPayload.RoomId);
        Assert.True(eventPayload.IsActive);
    }
}
