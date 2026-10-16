using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.InventoryManagement
{
    public class ItemAttributeValueConfiguration : IEntityTypeConfiguration<ItemAttributeValue>
    {
        public void Configure(EntityTypeBuilder<ItemAttributeValue> builder)
        {
            builder.ToTable("ItemAttributeValues");

            builder.Property(iav => iav.Value).HasMaxLength(150).IsRequired();
            builder.Property(iav => iav.Description).HasMaxLength(500).IsRequired(false);

            builder.HasMany(iav => iav.Specifications).WithOne(s => s.Value).HasForeignKey(s => s.ValueId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
