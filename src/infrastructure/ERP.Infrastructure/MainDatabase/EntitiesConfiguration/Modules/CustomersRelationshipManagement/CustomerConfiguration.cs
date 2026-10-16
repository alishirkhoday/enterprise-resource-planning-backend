using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.CustomersRelationshipManagement
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");

            builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
            builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
            builder.Property(c => c.Family).HasMaxLength(100).IsRequired(false);
            builder.Property(c => c.MobilePhoneNumber).HasMaxLength(20).IsRequired(false);
            builder.Property(c => c.Email).HasMaxLength(150).IsRequired(false);
            builder.Property(c => c.StaticPhoneNumber).HasMaxLength(20).IsRequired(false);
            builder.Property(c => c.FaxNumber).HasMaxLength(20).IsRequired(false);
            builder.Property(c => c.Website).HasMaxLength(200).IsRequired(false);
            builder.Property(c => c.RegistrationNumber).HasMaxLength(50).IsRequired(false);

            builder.HasIndex(c => c.Code).IsUnique();

            builder.Ignore(c => c.DisplayName);
            builder.Ignore(c => c.CanBeSoldTo);

            builder.OwnsOne(c => c.NationalId, n =>
            {
                n.Property(v => v.Value).HasColumnName("NationalId").HasMaxLength(20).IsRequired(false);
            });

            builder.OwnsOne(c => c.PassportId, p =>
            {
                p.Property(v => v.Value).HasColumnName("PassportId").HasMaxLength(20).IsRequired(false);
            });

            builder.OwnsOne(c => c.EconomicCode, e =>
            {
                e.Property(v => v.Value).HasColumnName("EconomicCode").HasMaxLength(20).IsRequired(false);
            });

            // CreditLimit is a Money, so the value object convention maps it to an amount and a
            // currency column of its own rather than a bare decimal.

            builder.HasMany(c => c.Addresses).WithOne(a => a.Customer).HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(c => c.Contacts).WithOne(a => a.Customer).HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
