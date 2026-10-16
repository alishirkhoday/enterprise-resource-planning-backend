using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.InventoryManagement
{
    public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
    {
        public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
        {
            builder.ToTable("UnitOfMeasures");

            builder.Property(uof => uof.Code).HasMaxLength(50).IsRequired();
            builder.Property(uof => uof.Name).HasMaxLength(150).IsRequired();
            builder.Property(uof => uof.Description).HasMaxLength(500).IsRequired(false);

            builder.HasIndex(uof => uof.Code).IsUnique();

            builder.HasMany(uof => uof.ItemGroups).WithOne(iguof => iguof.UnitOfMeasure).HasForeignKey(iguof => iguof.UnitOfMeasureId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
