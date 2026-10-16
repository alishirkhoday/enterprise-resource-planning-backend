using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.HumanResourcesManagement
{
    public class HumanAddressConfiguration : IEntityTypeConfiguration<HumanAddress>
    {
        public void Configure(EntityTypeBuilder<HumanAddress> builder)
        {
            builder.ToTable("HumansAddresses");

            builder.Property(ha => ha.Title).HasMaxLength(50).IsRequired();

            // The address itself is a value object, so its columns live on this table.
            builder.OwnsOne(ha => ha.Address, a =>
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

            builder.Navigation(ha => ha.Address).IsRequired();
        }
    }
}
