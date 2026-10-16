using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.InventoryManagement
{
    public class ItemUnitOfMeasureConfiguration : IEntityTypeConfiguration<ItemUnitOfMeasure>
    {
        public void Configure(EntityTypeBuilder<ItemUnitOfMeasure> builder)
        {
            builder.ToTable("ItemUnitOfMeasures");
        }
    }
}
