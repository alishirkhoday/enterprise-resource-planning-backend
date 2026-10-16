using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.WarehouseManagement
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouses");

            builder.OwnsOne(w => w.Address, w =>
            {
                w.WithOwner();
                w.Property(w => w.Latitude).HasMaxLength(50).IsRequired(false);
                w.Property(w => w.Longitude).HasMaxLength(50).IsRequired(false);
                w.Property(w => w.Country).HasMaxLength(50).IsRequired();
                w.Property(w => w.Province).HasMaxLength(50).IsRequired();
                w.Property(w => w.City).HasMaxLength(50).IsRequired();
                w.Property(w => w.Region).HasMaxLength(50).IsRequired();
                w.Property(w => w.Street).HasMaxLength(50).IsRequired();
                w.Property(w => w.Plaque).HasMaxLength(50).IsRequired();
                w.Property(w => w.PostalCode).HasMaxLength(50).IsRequired();
            });

            builder.Property(w => w.Code).HasMaxLength(50).IsRequired();
            builder.Property(w => w.Name).HasMaxLength(150).IsRequired();
            builder.Property(w => w.Description).HasMaxLength(400).IsRequired(false);

            builder.HasIndex(w => w.Code).IsUnique();

            builder.HasMany(w => w.StorageLocations).WithOne(wsl => wsl.Warehouse).HasForeignKey(wsl => wsl.WarehouseId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
