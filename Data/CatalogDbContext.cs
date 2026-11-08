using Microsoft.EntityFrameworkCore;
using Studium_Catalog.Domain.Entities.AvailabilitySchedules;
using Studium_Catalog.Domain.Entities.Buildings;
using Studium_Catalog.Domain.Entities.MaintenanceBlocks;
using Studium_Catalog.Domain.Entities.ResourceItems;
using Studium_Catalog.Domain.Entities.Rooms;

namespace Studium_Catalog.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<ResourceItem> ResourceItems => Set<ResourceItem>();
    public DbSet<AvailabilitySchedule> AvailabilitySchedules => Set<AvailabilitySchedule>();
    public DbSet<MaintenanceBlock> MaintenanceBlocks => Set<MaintenanceBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
