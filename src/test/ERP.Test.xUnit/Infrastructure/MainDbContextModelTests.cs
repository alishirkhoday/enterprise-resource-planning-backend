using ERP.Infrastructure.MainDatabase.Context;
using Microsoft.EntityFrameworkCore;

namespace ERP.Test.xUnit.Infrastructure
{
    /// <summary>
    /// Builds the Entity Framework model without touching a database. Constructing the model is what
    /// forces every mapping and configuration to be evaluated, so a value object that has not been
    /// told how to persist itself fails here rather than at the first request in production.
    /// </summary>
    public sealed class MainDbContextModelTests
    {
        private static MainDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlServer("Server=localhost;Database=ModelValidation;Trusted_Connection=True;")
                .Options;

            return new MainDbContext(options);
        }

        [Fact]
        public void Model_builds_without_error()
        {
            using var context = CreateContext();

            var model = context.Model;

            Assert.NotNull(model);
            Assert.NotEmpty(model.GetEntityTypes());
        }

        [Fact]
        public void Every_aggregate_is_mapped_to_a_table()
        {
            using var context = CreateContext();

            var unmapped = context.Model
                .GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType))
                .Where(e => e.GetTableName() is null)
                .Select(e => e.ClrType.Name)
                .ToList();

            Assert.True(unmapped.Count == 0, $"These entities are not mapped to a table: {string.Join(", ", unmapped)}");
        }

        [Fact]
        public void Every_aggregate_carries_the_audit_and_soft_delete_columns()
        {
            using var context = CreateContext();

            var missing = new List<string>();

            foreach (var entityType in context.Model.GetEntityTypes()
                         .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType) && e.GetTableName() is not null))
            {
                foreach (var propertyName in new[]
                         {
                             nameof(BaseEntity.CreationDateTime),
                             nameof(BaseEntity.CreatedBy),
                             nameof(BaseEntity.ModifiedBy),
                             nameof(BaseEntity.ModificationDateTime),
                             nameof(BaseEntity.IsDeleted),
                             nameof(BaseEntity.DeletedBy),
                             nameof(BaseEntity.DeletionDateTime),
                             nameof(BaseEntity.ConcurrencyStamp)
                         })
                {
                    if (entityType.FindProperty(propertyName) is null)
                        missing.Add($"{entityType.ClrType.Name}.{propertyName}");
                }
            }

            Assert.True(missing.Count == 0, $"Missing audit columns: {string.Join(", ", missing)}");
        }

        [Fact]
        public void Concurrency_stamp_is_a_concurrency_token()
        {
            using var context = CreateContext();

            var notTokens = context.Model
                .GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType) && e.GetTableName() is not null)
                .Where(e => e.FindProperty(nameof(BaseEntity.ConcurrencyStamp))?.IsConcurrencyToken != true)
                .Select(e => e.ClrType.Name)
                .ToList();

            Assert.True(notTokens.Count == 0, $"ConcurrencyStamp is not a concurrency token on: {string.Join(", ", notTokens)}");
        }

        [Fact]
        public void Soft_deleted_rows_are_filtered_out_by_default()
        {
            using var context = CreateContext();

            var withoutFilter = context.Model
                .GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType) && e.GetTableName() is not null)
                .Where(e => e.GetDeclaredQueryFilters().Count == 0)
                .Select(e => e.ClrType.Name)
                .ToList();

            Assert.True(withoutFilter.Count == 0, $"No soft delete query filter on: {string.Join(", ", withoutFilter)}");
        }

        [Fact]
        public void Domain_events_are_not_persisted()
        {
            using var context = CreateContext();

            var persisted = context.Model
                .GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType))
                .Where(e => e.FindNavigation(nameof(BaseEntity.DomainEvents)) is not null
                            || e.FindProperty(nameof(BaseEntity.DomainEvents)) is not null)
                .Select(e => e.ClrType.Name)
                .ToList();

            Assert.True(persisted.Count == 0, $"DomainEvents is being persisted on: {string.Join(", ", persisted)}");
        }
    }
}
