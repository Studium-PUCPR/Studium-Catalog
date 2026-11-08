using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Studium_Catalog.Domain.Entities.Buildings;

namespace Studium_Catalog.Data.Configurations;

public class BuildingConfig : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> entity)
    {
        entity.ToTable("Buildings");
        entity.HasKey(b => b.Id);

        entity.Property(b => b.Id).ValueGeneratedNever();
        entity.Property(b => b.Code).HasMaxLength(20).IsRequired();
        entity.Property(b => b.Name).HasMaxLength(150).IsRequired();
        entity.Property(b => b.Address).HasMaxLength(250);
        entity.Property(b => b.Description).HasMaxLength(1000);
        entity.Property(b => b.CreatedAtUtc).HasDefaultValueSql("now()").IsRequired();

        entity.HasMany(b => b.Rooms)
              .WithOne(r => r.Building)
              .HasForeignKey(r => r.BuildingId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(b => b.Code).IsUnique();
    }
}
