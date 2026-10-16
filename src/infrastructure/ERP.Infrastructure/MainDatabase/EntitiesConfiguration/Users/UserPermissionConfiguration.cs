using ERP.Domain.Entities.Security;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Users
{
    public class UserPermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("UsersPermissions");

            builder.Property(up => up.Title).HasMaxLength(200).IsRequired();
            builder.Property(up => up.Value).HasMaxLength(200).IsRequired();
        }
    }
}
