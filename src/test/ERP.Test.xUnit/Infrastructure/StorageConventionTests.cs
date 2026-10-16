using ERP.Infrastructure.MainDatabase.Context;
using Microsoft.EntityFrameworkCore;

namespace ERP.Test.xUnit.Infrastructure
{
    /// <summary>
    /// Guards the column shapes. A decimal that falls back to the Entity Framework default rounds
    /// money and quantities silently, and an unbounded string cannot be indexed.
    /// </summary>
    public sealed class StorageConventionTests
    {
        private static MainDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlServer("Server=localhost;Database=ModelValidation;Trusted_Connection=True;")
                .Options;

            return new MainDbContext(options);
        }

        [Fact]
        public void Every_decimal_column_states_its_precision()
        {
            using var context = CreateContext();

            var withoutPrecision = context.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
                .Where(p => p.GetPrecision() is null)
                .Select(p => $"{p.DeclaringType.ClrType.Name}.{p.Name}")
                .Distinct()
                .ToList();

            Assert.True(withoutPrecision.Count == 0,
                $"These decimal columns would fall back to the default precision: {string.Join(", ", withoutPrecision)}");
        }

        [Fact]
        public void Money_is_stored_with_four_decimal_places()
        {
            using var context = CreateContext();

            var amounts = context.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.DeclaringType.ClrType == typeof(Money) && p.Name == nameof(Money.Amount))
                .ToList();

            Assert.NotEmpty(amounts);
            Assert.All(amounts, p =>
            {
                Assert.Equal(18, p.GetPrecision());
                Assert.Equal(4, p.GetScale());
            });
        }

        [Fact]
        public void Quantities_keep_more_scale_than_money()
        {
            using var context = CreateContext();

            var quantityValues = context.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.DeclaringType.ClrType == typeof(Quantity) && p.Name == nameof(Quantity.Value))
                .ToList();

            Assert.All(quantityValues, p => Assert.Equal(6, p.GetScale()));
        }

        [Fact]
        public void Text_columns_are_bounded_unless_they_hold_a_document()
        {
            using var context = CreateContext();

            var unbounded = context.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.ClrType == typeof(string) && p.GetMaxLength() is null)
                .Select(p => $"{p.DeclaringType.ClrType.Name}.{p.Name}")
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            // Only the deliberately unbounded ones should be left.
            var allowed = new[]
            {
                "Payload", "ContentJson", "QueryDefinition", "ResultSnapshot", "CodeHash", "TokenHash",
                "TrackingUrlTemplate", "UserAgent", "FilePath", "ContentHash"
            };

            var unexpected = unbounded
                .Where(n => !allowed.Any(a => n.EndsWith($".{a}", StringComparison.Ordinal)))
                .ToList();

            Assert.True(unexpected.Count == 0,
                $"These text columns would become nvarchar(max): {string.Join(", ", unexpected)}");
        }

        [Fact]
        public void A_currency_code_is_stored_as_three_non_unicode_characters()
        {
            using var context = CreateContext();

            var currencyCodes = context.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.Name == "CurrencyCode" && p.ClrType == typeof(string))
                .ToList();

            Assert.NotEmpty(currencyCodes);
            Assert.All(currencyCodes, p =>
            {
                Assert.Equal(3, p.GetMaxLength());
                Assert.False(p.IsUnicode());
            });
        }
    }
}
