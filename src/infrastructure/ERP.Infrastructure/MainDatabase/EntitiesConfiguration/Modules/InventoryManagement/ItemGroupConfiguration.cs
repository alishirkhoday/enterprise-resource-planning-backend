using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.EntitiesConfiguration.Modules.InventoryManagement
{
    public class ItemGroupConfiguration : IEntityTypeConfiguration<ItemGroup>
    {
        public void Configure(EntityTypeBuilder<ItemGroup> builder)
        {
            builder.ToTable("ItemGroups");

            builder.HasKey(ig => ig.Id);

            builder.Property(ig => ig.Code).HasMaxLength(50).IsRequired();
            builder.Property(ig => ig.Name).HasMaxLength(150).IsRequired();
            builder.Property(ig => ig.Description).HasMaxLength(500).IsRequired(false);

            builder.HasIndex(ig => ig.Code).IsUnique();

            builder.HasMany(ig => ig.Attributes).WithOne(ia => ia.Group).HasForeignKey(ia => ia.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(ig => ig.Items).WithOne(i => i.Group).HasForeignKey(i => i.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(ig => ig.UnitOfMeasures).WithOne(iguof => iguof.ItemGroup).HasForeignKey(iguof => iguof.ItemGroupId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
