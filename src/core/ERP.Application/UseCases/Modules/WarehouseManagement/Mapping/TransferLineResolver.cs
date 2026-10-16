using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;
using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Mapping
{
    /// <summary>One of a transfer's lines, matched to the quantity a request asked to move.</summary>
    public sealed record ResolvedTransferLine(StockTransferOrderItem Line, decimal Quantity, string? DiscrepancyReason);

    /// <summary>
    /// Shared by dispatch and receipt, which ask the same question of a request and would drift
    /// apart if each answered it separately.
    /// </summary>
    public static class TransferLineResolver
    {
        /// <summary>
        /// Matches a request to the transfer's lines. An empty request means the whole transfer,
        /// which is what happens most of the time and is worth not making people type out.
        /// </summary>
        public static Result<List<ResolvedTransferLine>, Error> Resolve(
            StockTransferOrder transfer,
            IReadOnlyList<StockTransferMovementLineDto> requested,
            Func<StockTransferOrderItem, decimal> fullQuantity)
        {
            if (requested.Count == 0)
                return Result.Success<List<ResolvedTransferLine>, Error>(
                    [.. transfer.Items.Select(i => new ResolvedTransferLine(i, fullQuantity(i), null))]);

            var byId = transfer.Items.ToDictionary(i => i.Id);
            var resolved = new List<ResolvedTransferLine>(requested.Count);

            foreach (var line in requested)
            {
                if (!byId.TryGetValue(line.TransferItemId, out var transferLine))
                    return Result.Failure<List<ResolvedTransferLine>, Error>(
                        Errors.Warehouse.LineIsNotOnTheTransfer(line.TransferItemId));

                resolved.Add(new ResolvedTransferLine(transferLine, line.Quantity, line.DiscrepancyReason));
            }

            return Result.Success<List<ResolvedTransferLine>, Error>(resolved);
        }

        /// <summary>
        /// The ledger wants a moment, a transfer carries a date. Today's movements are stamped
        /// with the real time so the card reads in the order things happened; a backdated one is
        /// stamped at the start of its day, which is the most anybody can honestly claim.
        /// </summary>
        public static DateTimeOffset MovementMoment(TimeProvider timeProvider, DateOnly date)
        {
            var now = timeProvider.GetUtcNow();

            return DateOnly.FromDateTime(now.UtcDateTime) == date
                ? now
                : new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }
    }
}
