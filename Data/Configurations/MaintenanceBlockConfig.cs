using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Studium_Catalog.Domain.Entities.MaintenanceBlocks;

namespace Studium_Catalog.Data.Configurations;

public class MaintenanceBlockConfig : IEntityTypeConfiguration<MaintenanceBlock>
{
    public void Configure(EntityTypeBuilder<MaintenanceBlock> entity)
    {
        entity.ToTable("MaintenanceBlocks", t =>
            t.HasCheckConstraint("CK_MaintenanceBlocks_Period", "\"EndsAtUtc\" > \"StartsAtUtc\""));
        entity.HasKey(m => m.Id);

        entity.Property(m => m.Id).ValueGeneratedNever();
        entity.Property(m => m.RoomId).IsRequired();
        entity.Property(m => m.StartsAtUtc).IsRequired();
        entity.Property(m => m.EndsAtUtc).IsRequired();
        entity.Property(m => m.Reason).HasMaxLength(500).IsRequired();
        entity.Property(m => m.CreatedByUserId).HasMaxLength(128).IsRequired();
        entity.Property(m => m.CreatedAtUtc).HasDefaultValueSql("now()").IsRequired();

        entity.Ignore(m => m.IsCancelled);

        entity.HasIndex(m => new { m.RoomId, m.EndsAtUtc });
    }
}
