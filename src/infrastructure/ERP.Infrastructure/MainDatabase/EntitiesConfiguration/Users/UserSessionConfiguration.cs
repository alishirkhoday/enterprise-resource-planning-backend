using ERP.Domain.Entities.Users;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Users
{
    public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.ToTable("UsersSessions");

            builder.Property(us => us.InternetProtocolAddress).HasMaxLength(50).IsRequired();
            builder.Property(us => us.UserAgent).HasMaxLength(400).IsRequired();
            builder.Property(us => us.DeviceName).HasMaxLength(200).IsRequired();
            builder.Property(us => us.OperatingSystem).HasMaxLength(100).IsRequired();
            builder.Property(u => u.StartedAt).IsRequired();
            builder.Property(u => u.ExpiresAt).IsRequired(false);
            builder.Property(u => u.RevokedAt).IsRequired(false);
        }
    }
}
