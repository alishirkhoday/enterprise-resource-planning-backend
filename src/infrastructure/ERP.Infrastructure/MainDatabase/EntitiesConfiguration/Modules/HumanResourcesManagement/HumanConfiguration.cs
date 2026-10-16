using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.HumanResourcesManagement
{
    public class HumanConfiguration : IEntityTypeConfiguration<Human>
    {
        public void Configure(EntityTypeBuilder<Human> builder)
        {
            builder.ToTable("Humans");

            builder.Property(h => h.Name).HasMaxLength(50).IsRequired();
            builder.Property(h => h.Family).HasMaxLength(100).IsRequired();

            builder.OwnsOne(h => h.NationalId, h =>
            {
                h.WithOwner();
                h.HasIndex(h => h.Value).IsUnique();
                h.Property(h => h.Value).HasMaxLength(50).IsRequired();
            });

            builder.OwnsOne(h => h.PassportId, h =>
            {
                h.WithOwner();
                h.HasIndex(h => h.Value).IsUnique();
                h.Property(h => h.Value).HasMaxLength(50).IsRequired(false);
            });

            builder.HasOne(h => h.User).WithOne().HasForeignKey<Human>(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(h => h.ContactInformation).WithOne(hci => hci.Human).HasForeignKey(hci => hci.HumanId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(h => h.Addresses).WithOne(ha => ha.Human).HasForeignKey(ha => ha.HumanId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(h => h.BankAccounts).WithOne(hba => hba.Human).HasForeignKey(hba => hba.HumanId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
