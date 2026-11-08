using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Studium_Catalog.Domain.Entities.Rooms;

namespace Studium_Catalog.Data.Configurations;

public class RoomConfig : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> entity)
    {
        entity.ToTable("Rooms");
        entity.HasKey(r => r.Id);

        entity.Property(r => r.Id).ValueGeneratedNever();
        entity.Property(r => r.Code).HasMaxLength(20).IsRequired();
        entity.Property(r => r.Name).HasMaxLength(150).IsRequired();
        entity.Property(r => r.Type).HasDefaultValue(RoomType.Classroom).IsRequired();
        entity.Property(r => r.Capacity).IsRequired();
        entity.Property(r => r.Floor).HasDefaultValue(0).IsRequired();
        entity.Property(r => r.Description).HasMaxLength(1000);
        // No database default: a 'true' default would silently override an explicit 'false' (CLR sentinel) on insert.
        entity.Property(r => r.IsActive).IsRequired();
        entity.Property(r => r.CreatedAtUtc).HasDefaultValueSql("now()").IsRequired();

        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Rooms_Capacity", $"\"Capacity\" BETWEEN 1 AND {Room.MaxCapacity}");
            t.HasCheckConstraint("CK_Rooms_MapPositionX", "\"MapPositionX\" IS NULL OR \"MapPositionX\" BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_Rooms_MapPositionY", "\"MapPositionY\" IS NULL OR \"MapPositionY\" BETWEEN 0 AND 100");
        });

        entity.HasMany(r => r.Resources)
              .WithOne()
              .HasForeignKey(i => i.RoomId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(r => r.OperatingHours)
              .WithOne()
              .HasForeignKey(s => s.RoomId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(r => r.MaintenanceBlocks)
              .WithOne()
              .HasForeignKey(m => m.RoomId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(r => new { r.BuildingId, r.Code }).IsUnique();
        entity.HasIndex(r => new { r.BuildingId, r.Floor });
        entity.HasIndex(r => r.Type);
        entity.HasIndex(r => r.IsActive);
    }
}
