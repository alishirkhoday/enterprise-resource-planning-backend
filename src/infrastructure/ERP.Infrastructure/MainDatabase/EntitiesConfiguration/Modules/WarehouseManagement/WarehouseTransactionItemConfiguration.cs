using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.WarehouseManagement
{
    public class WarehouseTransactionItemConfiguration : IEntityTypeConfiguration<WarehouseTransactionItem>
    {
        public void Configure(EntityTypeBuilder<WarehouseTransactionItem> builder)
        {
            builder.ToTable("WarehouseTransactionItems");

            builder.Property(wti => wti.Quantity).HasPrecision(18, 3).IsRequired();
        }
    }
}
