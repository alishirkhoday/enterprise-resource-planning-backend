using Microsoft.EntityFrameworkCore.Metadata;

namespace ERP.Infrastructure.MainDatabase.Context
{
    /// <summary>
    /// Gives every column a sensible shape.
    /// <para>
    /// Without this, Entity Framework would store every decimal as <c>decimal(18,2)</c> and every
    /// string as <c>nvarchar(max)</c>. The first silently rounds quantities and exchange rates, and
    /// the second cannot be indexed and wastes a great deal of space. The rules below are applied by
    /// name and by owning type, so a hand written configuration always overrides them.
    /// </para>
    /// </summary>
    internal static class StorageConvention
    {
        // Money keeps four decimal places: enough for a foreign currency, and exact for the rial.
        private const int MoneyPrecision = 18;
        private const int MoneyScale = 4;

        // Quantities carry more scale because they are divided by conversion factors.
        private const int QuantityPrecision = 18;
        private const int QuantityScale = 6;

        // A rate can be very large in rial terms and still needs fractional precision.
        private const int RatePrecision = 18;
        private const int RateScale = 8;

        private const int PercentagePrecision = 9;
        private const int PercentageScale = 4;

        private const int DefaultTextLength = 256;
        private const int ShortCodeLength = 50;
        private const int LongTextLength = 2000;

        /// <summary>An ISO 4217 code is exactly three letters.</summary>
        private const int CurrencyCodeLength = 3;

        /// <summary>
        /// Business events are recorded to the second. Stating the precision rather than a column
        /// type keeps this portable: SQL Server renders it as <c>datetimeoffset(0)</c> and
        /// PostgreSQL as <c>timestamptz(0)</c>, from the same model.
        /// </summary>
        private const int TimestampPrecision = 0;

        /// <summary>
        /// Readings that arrive faster than once a second, where truncating to the second would
        /// collapse separate events onto the same instant.
        /// </summary>
        private const int HighResolutionTimestampPrecision = 3;

        private static readonly HashSet<string> HighResolutionTimestampNames = new(StringComparer.Ordinal)
        {
            "CapturedAt", "OccurredOn", "MovementDateTime", "ActedOn", "CheckedAt", "ReportedAt"
        };

        /// <summary>Identifiers and codes, which stay short so they index well.</summary>
        private static readonly HashSet<string> ShortCodeNames = new(StringComparer.Ordinal)
        {
            "Code", "Number", "Key", "PostalCode", "BarCode", "QRCode", "SerialNumber",
            "BatchNumber", "TagNumber", "AccountNumber", "CardNumber", "TrackingCode", "TrackingNumber",
            "DocumentNumber", "ContractNumber", "CertificateNumber", "SayadNumber", "NationalIdentifier",
            "RegistrationNumber", "EmployeeNumber", "PersonnelCode", "VehicleNumber", "ReferenceId",
            "ReferenceNumber", "SupplierInvoiceNumber", "CreditNoteNumber", "BankReference",
            "PhoneRegionCode", "InsurerBranchCode", "WorkshopCode", "SupplierItemCode", "CronExpression",
            "ConcurrencyStamp", "SecurityStamp", "CorrelationId", "TransactionId", "AuthorityCode"
        };

        /// <summary>Free text that people write into, which needs room but is still bounded.</summary>
        private static readonly HashSet<string> LongTextNames = new(StringComparer.Ordinal)
        {
            "Description", "Notes", "Note", "Comment", "EmployeeComment", "Reason", "RejectionReason",
            "CancellationReason", "TerminationReason", "TerminationDescription", "BounceReason",
            "DiscrepancyReason", "HoldReason", "FailureReason", "Terms", "Requirements", "Justification",
            "Instructions", "WorkPerformed", "ProblemDescription", "Resolution", "CoverLetter",
            "Feedback", "Message", "AcceptanceCriteria", "Specification", "Recipients", "Strengths",
            "AreasForImprovement", "InspectionNotes", "Outcome", "Decisions", "Subject", "Body",
            "DeliveryInstruction", "Error", "Purpose", "Location"
        };

        /// <summary>Columns that hold a document rather than a sentence, so they stay unbounded.</summary>
        private static readonly HashSet<string> UnboundedTextNames = new(StringComparer.Ordinal)
        {
            "Payload", "ContentJson", "QueryDefinition", "ResultSnapshot", "CodeHash", "TokenHash",
            "TrackingUrlTemplate", "UserAgent", "FilePath", "ContentHash"
        };

        public static void Apply(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    ApplyDecimalPrecision(property);
                    ApplyTextLength(property);
                    ApplyTimestampPrecision(property);
                }
            }
        }

        private static void ApplyDecimalPrecision(IMutableProperty property)
        {
            if (property.ClrType != typeof(decimal) && property.ClrType != typeof(decimal?))
                return;

            // Respect anything a configuration class already stated.
            if (property.GetPrecision() is not null)
                return;

            var declaringType = property.DeclaringType.ClrType;
            var name = property.Name;

            if (declaringType == typeof(Money) && name == nameof(Money.Amount))
            {
                property.SetPrecision(MoneyPrecision);
                property.SetScale(MoneyScale);
                return;
            }

            if (declaringType == typeof(Percentage) || name.EndsWith("Percentage", StringComparison.Ordinal))
            {
                property.SetPrecision(PercentagePrecision);
                property.SetScale(PercentageScale);
                return;
            }

            if (declaringType == typeof(Quantity) || IsQuantityName(name))
            {
                property.SetPrecision(QuantityPrecision);
                property.SetScale(QuantityScale);
                return;
            }

            if (IsRateName(name))
            {
                property.SetPrecision(RatePrecision);
                property.SetScale(RateScale);
                return;
            }

            property.SetPrecision(MoneyPrecision);
            property.SetScale(MoneyScale);
        }

        private static bool IsQuantityName(string name) =>
            name.EndsWith("Quantity", StringComparison.Ordinal)
            || name.EndsWith("Hours", StringComparison.Ordinal)
            || name.EndsWith("Days", StringComparison.Ordinal)
            || name.EndsWith("Units", StringComparison.Ordinal)
            || name is "Value" or "Weight" or "Volume" or "WeightInKilograms" or "VolumeInCubicMeters"
                    or "RunningHours" or "RunningHoursAtLastService" or "DailyCapacityHours"
                    or "DurationHours" or "AttendedHours" or "YearsOfExperience";

        private static bool IsRateName(string name) =>
            name is "Rate" or "ExchangeRate" or "Factor" or "RateMultiplier"
                 or "NominalCapacityPerHour" or "OrderMultiple";

        private static void ApplyTimestampPrecision(IMutableProperty property)
        {
            if (property.ClrType != typeof(DateTimeOffset) && property.ClrType != typeof(DateTimeOffset?))
                return;

            // Respect anything a configuration class already stated.
            if (property.GetPrecision() is not null || property.GetColumnType() is not null)
                return;

            property.SetPrecision(HighResolutionTimestampNames.Contains(property.Name)
                ? HighResolutionTimestampPrecision
                : TimestampPrecision);
        }

        private static void ApplyTextLength(IMutableProperty property)
        {
            if (property.ClrType != typeof(string))
                return;

            // Respect anything a configuration class already stated.
            if (property.GetMaxLength() is not null)
                return;

            var name = property.Name;

            if (UnboundedTextNames.Contains(name))
                return;

            if (name is "CurrencyCode" or "PreferredCurrencyCode")
            {
                property.SetMaxLength(CurrencyCodeLength);
                property.SetIsUnicode(false);
                return;
            }

            if (LongTextNames.Contains(name))
            {
                property.SetMaxLength(LongTextLength);
                return;
            }

            if (ShortCodeNames.Contains(name))
            {
                property.SetMaxLength(ShortCodeLength);
                return;
            }

            property.SetMaxLength(DefaultTextLength);
        }
    }
}
