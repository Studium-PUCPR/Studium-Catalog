using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Studium_Catalog.Domain.Entities.ResourceItems;

namespace Studium_Catalog.Data.Configurations;

public class ResourceItemConfig : IEntityTypeConfiguration<ResourceItem>
{
    public void Configure(EntityTypeBuilder<ResourceItem> entity)
    {
        entity.ToTable("ResourceItems", t =>
            t.HasCheckConstraint("CK_ResourceItems_Quantity", "\"Quantity\" >= 1"));
        entity.HasKey(i => i.Id);

        entity.Property(i => i.Id).ValueGeneratedNever();
        entity.Property(i => i.RoomId).IsRequired();
        entity.Property(i => i.Name).HasMaxLength(150).IsRequired();
        entity.Property(i => i.Type).IsRequired();
        entity.Property(i => i.Quantity).HasDefaultValue(1).IsRequired();
        entity.Property(i => i.Status).HasDefaultValue(ResourceStatus.Operational).IsRequired();

        entity.HasIndex(i => i.RoomId);
        entity.HasIndex(i => i.Type);
    }
}
