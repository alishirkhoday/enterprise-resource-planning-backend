using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.WarehouseManagement
{
    public class WarehouseStorageLocationConfiguration : IEntityTypeConfiguration<WarehouseStorageLocation>
    {
        public void Configure(EntityTypeBuilder<WarehouseStorageLocation> builder)
        {
            builder.ToTable("WarehouseStorageLocations");

            builder.Property(wsl => wsl.Code).HasMaxLength(50).IsRequired();
            builder.Property(wsl => wsl.Name).HasMaxLength(150).IsRequired();
            builder.Property(wsl => wsl.Description).HasMaxLength(500);

            builder.HasIndex(wsl => wsl.Code).IsUnique();
        }
    }
}
