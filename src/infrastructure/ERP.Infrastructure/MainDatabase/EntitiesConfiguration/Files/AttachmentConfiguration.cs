using ERP.Domain.Entities.Files;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Files
{
    public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.ToTable("Attachments");

            builder.Property(a => a.ReferenceId).HasMaxLength(50).IsRequired();
            builder.Property(a => a.ReferenceType).HasMaxLength(50).IsRequired();
            builder.Property(a => a.FileName).HasMaxLength(200).IsRequired();
            builder.Property(a => a.FilePath).HasMaxLength(800).IsRequired();
            builder.Property(a => a.UploadedBy).HasMaxLength(50).IsRequired();
            builder.Property(a => a.UploadDateTime).IsRequired();
        }
    }
}
