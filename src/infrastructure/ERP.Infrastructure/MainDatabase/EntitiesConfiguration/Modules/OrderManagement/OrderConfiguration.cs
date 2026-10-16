using ERP.Domain.Entities.Modules.OrderManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.OrderManagement
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            // The delivery address is a snapshot taken when the order was placed, so it is owned
            // by the order rather than pointing at the customer address that may since have changed.
            builder.OwnsOne(o => o.ShipToAddress, ship =>
            {
                ship.WithOwner();

                ship.Property(s => s.ReceiverFullName).HasColumnName("ReceiverFullName").HasMaxLength(150).IsRequired();
                ship.Property(s => s.ReceiverMobileNumber).HasColumnName("ReceiverMobileNumber").HasMaxLength(50).IsRequired();
                ship.Property(s => s.ReceiverNationalId).HasColumnName("ReceiverNationalId").HasMaxLength(20).IsRequired(false);
                ship.Property(s => s.DeliveryInstruction).HasColumnName("DeliveryInstruction").HasMaxLength(500).IsRequired(false);

                ship.OwnsOne(s => s.Address, a =>
                {
                    a.Property(p => p.Latitude).HasColumnName("Latitude").HasMaxLength(50).IsRequired(false);
                    a.Property(p => p.Longitude).HasColumnName("Longitude").HasMaxLength(50).IsRequired(false);
                    a.Property(p => p.CountryId).HasColumnName("CountryId").IsRequired(false);
                    a.Property(p => p.ProvinceId).HasColumnName("ProvinceId").IsRequired(false);
                    a.Property(p => p.CityId).HasColumnName("CityId").IsRequired(false);
                    a.Property(p => p.Country).HasColumnName("Country").HasMaxLength(50).IsRequired();
                    a.Property(p => p.Province).HasColumnName("Province").HasMaxLength(50).IsRequired();
                    a.Property(p => p.City).HasColumnName("City").HasMaxLength(50).IsRequired();
                    a.Property(p => p.Region).HasColumnName("Region").HasMaxLength(50).IsRequired();
                    a.Property(p => p.Street).HasColumnName("Street").HasMaxLength(100).IsRequired();
                    a.Property(p => p.Plaque).HasColumnName("Plaque").HasMaxLength(50).IsRequired();
                    a.Property(p => p.Unit).HasColumnName("Unit").HasMaxLength(50).IsRequired(false);
                    a.Property(p => p.PostalCode).HasColumnName("PostalCode").HasMaxLength(50).IsRequired();
                });
            });

            builder.Property(o => o.Code).HasMaxLength(50).IsRequired();
            builder.Property(o => o.RegistrationDateTime).IsRequired();
            builder.Property(o => o.TimeLeftToPayment).IsRequired();
            builder.Property(o => o.CancellationReason).HasMaxLength(500).IsRequired(false);

            builder.HasIndex(o => o.Code).IsUnique();

            // The money amounts and the derived totals are both handled by the value object
            // convention: amounts become owned columns, and computed totals are left unmapped.

            builder.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(o => o.Payments).WithOne(p => p.Order).HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(o => o.ShipmentStatusHistories).WithOne(h => h.Order).HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
