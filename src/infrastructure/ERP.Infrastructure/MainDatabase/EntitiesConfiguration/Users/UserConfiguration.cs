using ERP.Domain.Entities.Users;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Users
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.OwnsOne(u => u.Username, u =>
            {
                u.WithOwner();
                u.Property(u => u.Value).HasMaxLength(100).IsRequired();
            });

            builder.OwnsOne(u => u.PasswordHash, u =>
            {
                u.WithOwner();
                u.Property(u => u.Value).HasMaxLength(100).IsRequired();
            });

            builder.OwnsOne(u => u.MobilePhoneNumberRegionCode, u =>
            {
                u.WithOwner();
                u.Property(u => u.Value).HasMaxLength(50).IsRequired(false);
            });

            builder.OwnsOne(u => u.MobilePhoneNumber, u =>
            {
                u.WithOwner();
                u.Property(u => u.Value).HasMaxLength(50).IsRequired(false);
            });

            builder.OwnsOne(u => u.Email, u =>
            {
                u.WithOwner();
                u.Property(u => u.Value).HasMaxLength(300).IsRequired(false);
            });

            builder.Property(u => u.NormalizedUsername).HasMaxLength(150).IsRequired();
            builder.HasIndex(u => u.NormalizedUsername).IsUnique();

            builder.Property(u => u.NormalizedMobilePhoneNumber).HasMaxLength(50).IsRequired(false);
            builder.HasIndex(u => u.NormalizedMobilePhoneNumber).IsUnique();

            builder.Property(u => u.NormalizedEmail).HasMaxLength(300).IsRequired(false);
            builder.HasIndex(u => u.NormalizedEmail).IsUnique();

            builder.Property(u => u.SecurityStamp).HasMaxLength(50).IsRequired();
            builder.Property(u => u.LockoutEnd).IsRequired(false);

            builder.HasMany(u => u.Sessions).WithOne(us => us.User).HasForeignKey(us => us.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(u => u.Tokens).WithOne(ut => ut.User).HasForeignKey(ut => ut.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
