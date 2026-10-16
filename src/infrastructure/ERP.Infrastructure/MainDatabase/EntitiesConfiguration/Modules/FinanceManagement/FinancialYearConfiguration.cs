using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.FinanceManagement
{
    public class FinancialYearConfiguration : IEntityTypeConfiguration<FinancialYear>
    {
        public void Configure(EntityTypeBuilder<FinancialYear> builder)
        {
            builder.ToTable("FinancialYears");

            builder.OwnsOne(fy => fy.Setting, fy =>
            {
                fy.WithOwner();
                fy.Property(fy => fy.BaseSalary).HasPrecision(18, 3).IsRequired();
            });
        }
    }
}
