using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.InventoryManagement
{
    public class ItemAttributeConfiguration : IEntityTypeConfiguration<ItemAttribute>
    {
        public void Configure(EntityTypeBuilder<ItemAttribute> builder)
        {
            builder.ToTable("ItemAttributes");

            builder.Property(ia => ia.Title).HasMaxLength(150).IsRequired();
            builder.Property(ia => ia.Description).HasMaxLength(500).IsRequired(false);

            builder.HasMany(ia => ia.Values).WithOne(iav => iav.Attribute).HasForeignKey(iav => iav.AttributeId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(ia => ia.Specifications).WithOne(s => s.Attribute).HasForeignKey(s => s.AttributeId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
