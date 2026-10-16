using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.FinanceManagement
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");

            builder.Property(a => a.Code).HasMaxLength(50).IsRequired();
            builder.Property(a => a.Name).HasMaxLength(150).IsRequired();
            builder.Property(a => a.Inventory).HasPrecision(18, 3).HasDefaultValue(null).IsRequired();
        }
    }
}
