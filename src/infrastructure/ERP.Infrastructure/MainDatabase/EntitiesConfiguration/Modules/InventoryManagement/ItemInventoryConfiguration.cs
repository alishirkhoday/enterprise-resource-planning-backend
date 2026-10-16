using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.InventoryManagement
{
    public class ItemInventoryConfiguration : IEntityTypeConfiguration<ItemInventory>
    {
        public void Configure(EntityTypeBuilder<ItemInventory> builder)
        {
            builder.ToTable("ItemInventories");

            builder.Property(ii => ii.MinimumQuantity).HasPrecision(18, 3);
            builder.Property(ii => ii.OnHandQuantity).HasPrecision(18, 3);
            builder.Property(ii => ii.ReservedQuantity).HasPrecision(18, 3);
            builder.Property(ii => ii.LastUpdatedOnHandQuantity);
            builder.Property(ii => ii.LastUpdatedReservedQuantity);

            builder.HasOne(ii => ii.Item).WithOne().HasForeignKey<ItemInventory>(iv => iv.ItemId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
