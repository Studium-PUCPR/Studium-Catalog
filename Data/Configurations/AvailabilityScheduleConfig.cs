using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Studium_Catalog.Domain.Entities.AvailabilitySchedules;

namespace Studium_Catalog.Data.Configurations;

public class AvailabilityScheduleConfig : IEntityTypeConfiguration<AvailabilitySchedule>
{
    public void Configure(EntityTypeBuilder<AvailabilitySchedule> entity)
    {
        entity.ToTable("AvailabilitySchedules", t =>
            t.HasCheckConstraint("CK_AvailabilitySchedules_Window", "\"ClosesAt\" > \"OpensAt\""));
        entity.HasKey(s => s.Id);

        entity.Property(s => s.Id).ValueGeneratedNever();
        entity.Property(s => s.RoomId).IsRequired();
        entity.Property(s => s.DayOfWeek).IsRequired();
        entity.Property(s => s.OpensAt).IsRequired();
        entity.Property(s => s.ClosesAt).IsRequired();

        entity.HasIndex(s => new { s.RoomId, s.DayOfWeek });
    }
}
