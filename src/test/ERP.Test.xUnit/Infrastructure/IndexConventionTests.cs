using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Security;
using ERP.Infrastructure.MainDatabase.Context;
using Microsoft.EntityFrameworkCore;

namespace ERP.Test.xUnit.Infrastructure
{
    public sealed class IndexConventionTests
    {
        private static MainDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlServer("Server=localhost;Database=ModelValidation;Trusted_Connection=True;")
                .Options;

            return new MainDbContext(options);
        }

        [Theory]
        [InlineData(typeof(Currency), "Code")]
        [InlineData(typeof(Account), "Code")]
        [InlineData(typeof(Permission), "Key")]
        [InlineData(typeof(Customer), "Code")]
        [InlineData(typeof(Supplier), "Code")]
        [InlineData(typeof(PurchaseOrder), "Number")]
        [InlineData(typeof(Invoice), "Number")]
        public void A_business_identifier_is_unique(Type entityClrType, string propertyName)
        {
            using var context = CreateContext();

            var entityType = context.Model.FindEntityType(entityClrType);
            Assert.NotNull(entityType);

            var hasUniqueIndex = entityType.GetIndexes()
                .Any(i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == propertyName);

            Assert.True(hasUniqueIndex, $"{entityClrType.Name}.{propertyName} is not covered by a unique index.");
        }

        [Fact]
        public void Stock_is_unique_per_item_warehouse_and_location()
        {
            using var context = CreateContext();

            var entityType = context.Model.FindEntityType(typeof(ItemInventory));
            Assert.NotNull(entityType);

            var hasIndex = entityType.GetIndexes().Any(i =>
                i.IsUnique
                && i.Properties.Select(p => p.Name).OrderBy(n => n)
                    .SequenceEqual(new[] { "ItemId", "StorageLocationId", "WarehouseId" }));

            Assert.True(hasIndex, "ItemInventory is not unique per item, warehouse and storage location.");
        }

        [Fact]
        public void Loose_branch_references_are_indexed()
        {
            using var context = CreateContext();

            var unindexed = context.Model
                .GetEntityTypes()
                .Where(e => !e.IsOwned() && e.GetTableName() is not null)
                .Where(e => e.FindProperty("BranchId") is not null)
                .Where(e => !e.GetIndexes().Any(i => i.Properties.Any(p => p.Name == "BranchId"))
                            && !e.GetForeignKeys().Any(fk => fk.Properties.Any(p => p.Name == "BranchId")))
                .Select(e => e.ClrType.Name)
                .ToList();

            Assert.True(unindexed.Count == 0, $"BranchId is not indexed on: {string.Join(", ", unindexed)}");
        }

        [Fact]
        public void Polymorphic_reference_pairs_are_indexed_together()
        {
            using var context = CreateContext();

            var unindexed = context.Model
                .GetEntityTypes()
                .Where(e => !e.IsOwned() && e.GetTableName() is not null)
                .Where(e => e.FindProperty("ReferenceType") is not null && e.FindProperty("ReferenceId") is not null)
                .Where(e => !e.GetIndexes().Any(i =>
                    i.Properties.Count == 2
                    && i.Properties.Any(p => p.Name == "ReferenceType")
                    && i.Properties.Any(p => p.Name == "ReferenceId")))
                .Select(e => e.ClrType.Name)
                .ToList();

            Assert.True(unindexed.Count == 0,
                $"ReferenceType and ReferenceId are not indexed together on: {string.Join(", ", unindexed)}");
        }
    }
}
