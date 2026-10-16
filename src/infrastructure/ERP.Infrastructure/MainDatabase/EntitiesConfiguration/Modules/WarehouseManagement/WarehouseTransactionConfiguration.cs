using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.WarehouseManagement
{
    public class WarehouseTransactionConfiguration : IEntityTypeConfiguration<WarehouseTransaction>
    {
        public void Configure(EntityTypeBuilder<WarehouseTransaction> builder)
        {
            builder.ToTable("WarehouseTransactions");

            builder.Property(wt => wt.Description).HasMaxLength(500).IsRequired();
            builder.Property(wt => wt.TransactionEventDateTime);

            builder.HasOne(wt => wt.SourceLocation).WithMany().HasForeignKey(wt => wt.SourceLocationId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(wt => wt.DestinationLocation).WithMany().HasForeignKey(wt => wt.DestinationLocationId).OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(wt => wt.Items).WithOne(wti => wti.Transaction).HasForeignKey(wti => wti.TransactionId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
