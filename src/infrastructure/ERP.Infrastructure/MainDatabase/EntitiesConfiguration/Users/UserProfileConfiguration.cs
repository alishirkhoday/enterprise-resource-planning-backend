using ERP.Domain.Entities.Users;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Users
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("UsersProfiles");

            builder.OwnsOne(up => up.Image, up =>
            {
                up.WithOwner();
                up.Property(i => i.FileName).HasMaxLength(400).IsRequired(false);
                up.Property(i => i.FilePath).HasMaxLength(800).IsRequired(false);
                up.Property(i => i.Alt).HasMaxLength(400).IsRequired(false);
            });

            builder.HasOne(up => up.User).WithOne().HasForeignKey<UserProfile>(up => up.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
