using ERP.Infrastructure.MainDatabase.Context;
using Microsoft.EntityFrameworkCore;

namespace ERP.Test.xUnit.Infrastructure
{
    /// <summary>
    /// Keeps the model portable between SQL engines.
    /// <para>
    /// The target database is PostgreSQL, but the model is written so that the provider is a
    /// configuration choice rather than a rewrite. That only holds while nothing states a column
    /// type in a particular dialect: <c>nvarchar</c>, <c>datetimeoffset</c> and <c>uniqueidentifier</c>
    /// mean nothing to PostgreSQL, and <c>timestamptz</c> means nothing to SQL Server. Stating
    /// precision and length instead lets each provider render its own type.
    /// </para>
    /// </summary>
    public sealed class ProviderPortabilityTests
    {
        private static readonly string[] DialectSpecificTypeFragments =
        [
            "nvarchar", "varchar", "datetimeoffset", "datetime2", "uniqueidentifier", "money",
            "smalldatetime", "ntext", "image", "timestamptz", "timestamp with", "jsonb", "bytea",
            "int4", "int8", "float8", "numeric("
        ];

        private static MainDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlServer("Server=localhost;Database=ModelValidation;Trusted_Connection=True;")
                .Options;

            return new MainDbContext(options);
        }

        [Fact]
        public void No_property_hard_codes_a_dialect_specific_column_type()
        {
            using var context = CreateContext();

            var offenders = context.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Select(p => new
                {
                    Owner = p.DeclaringType.ClrType.Name,
                    p.Name,
                    // The annotation is what a configuration set by hand; the resolved column type
                    // is whatever the provider chose, which is allowed to be dialect specific.
                    ConfiguredType = (string?)p.FindAnnotation("Relational:ColumnType")?.Value
                })
                .Where(p => p.ConfiguredType is not null)
                .Where(p => DialectSpecificTypeFragments.Any(f =>
                    p.ConfiguredType!.Contains(f, StringComparison.OrdinalIgnoreCase)))
                .Select(p => $"{p.Owner}.{p.Name} => {p.ConfiguredType}")
                .Distinct()
                .ToList();

            Assert.True(offenders.Count == 0,
                $"These properties pin a database specific column type and would not port: {string.Join(", ", offenders)}");
        }

        [Fact]
        public void Timestamps_state_a_precision_so_each_provider_can_render_its_own_type()
        {
            using var context = CreateContext();

            var withoutPrecision = context.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?))
                .Where(p => p.GetPrecision() is null)
                .Select(p => $"{p.DeclaringType.ClrType.Name}.{p.Name}")
                .Distinct()
                .ToList();

            Assert.True(withoutPrecision.Count == 0,
                $"These timestamps have no stated precision: {string.Join(", ", withoutPrecision)}");
        }

        [Fact]
        public void Business_timestamps_are_recorded_to_the_second()
        {
            using var context = CreateContext();

            var creationTimestamps = context.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.Name == "CreationDateTime")
                .ToList();

            Assert.NotEmpty(creationTimestamps);
            Assert.All(creationTimestamps, p => Assert.Equal(0, p.GetPrecision()));
        }

        [Fact]
        public void High_frequency_readings_keep_sub_second_precision()
        {
            using var context = CreateContext();

            var sensorReading = context.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .FirstOrDefault(p => p.Name == "CapturedAt");

            Assert.NotNull(sensorReading);
            Assert.Equal(3, sensorReading.GetPrecision());
        }
    }
}
