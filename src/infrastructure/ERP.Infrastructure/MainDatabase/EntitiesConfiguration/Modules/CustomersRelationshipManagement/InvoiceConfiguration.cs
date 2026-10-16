using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.CustomersRelationshipManagement
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.Property(i => i.Number).HasMaxLength(50).IsRequired();
        }
    }
}
