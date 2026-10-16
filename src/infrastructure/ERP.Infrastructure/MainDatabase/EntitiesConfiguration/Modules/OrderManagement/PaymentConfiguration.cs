using ERP.Domain.Entities.Modules.OrderManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.OrderManagement
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.OwnsOne(p => p.PaymentGatewayInformation, p =>
            {
                p.WithOwner();
                p.Property(p => p.Name).HasMaxLength(50).IsRequired(false);
                p.Property(p => p.Domain).HasMaxLength(50).IsRequired(false);
                p.Property(p => p.TrackingCode).HasMaxLength(50).IsRequired(false);
            });

            builder.Property(u => u.PaymentDateTime).IsRequired(false);
        }
    }
}
