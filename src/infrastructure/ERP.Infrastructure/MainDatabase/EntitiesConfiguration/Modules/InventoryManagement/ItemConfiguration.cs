using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.InventoryManagement
{
    public class ItemConfiguration : IEntityTypeConfiguration<Item>
    {
        public void Configure(EntityTypeBuilder<Item> builder)
        {
            builder.ToTable("Items");

            builder.Property(i => i.StockKeepingUnit).HasMaxLength(50).IsRequired(false);
            builder.Property(i => i.Name).HasMaxLength(200).IsRequired(false);
            builder.Property(i => i.BarCode).HasMaxLength(100).IsRequired(false);
            builder.Property(i => i.QRCode).HasMaxLength(100).IsRequired(false);
            builder.Property(i => i.Description).HasMaxLength(500).IsRequired(false);

            builder.HasIndex(i => i.StockKeepingUnit).IsUnique();

            builder.HasMany(i => i.Specifications).WithOne(s => s.Item).HasForeignKey(s => s.ItemId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
