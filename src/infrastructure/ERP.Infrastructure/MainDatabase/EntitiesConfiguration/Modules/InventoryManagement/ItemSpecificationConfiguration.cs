using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.InventoryManagement
{
    public class ItemSpecificationConfiguration : IEntityTypeConfiguration<ItemSpecification>
    {
        public void Configure(EntityTypeBuilder<ItemSpecification> builder)
        {
            builder.ToTable("ItemSpecifications");
        }
    }
}
