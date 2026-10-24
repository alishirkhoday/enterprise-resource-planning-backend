using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Application.UseCases.Modules.InventoryManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.RecordStockCount
{
    /// <summary>
    /// Records what was found on the shelves.
    /// <para>
    /// Nothing about the books changes here. A count sheet is evidence, and evidence is collected
    /// before it is acted on - which is why approving the count is a separate step somebody else
    /// can be made to take.
    /// </para>
    /// </summary>
    public class RecordStockCountCommandHandler(IStockCountRepository stockCounts)
        : IRequestHandler<RecordStockCountCommand, Result<StockCountDto, Error>>
    {
        public async Task<Result<StockCountDto, Error>> Handle(RecordStockCountCommand request, CancellationToken cancellationToken)
        {
            var count = await stockCounts.GetWithItemsAsync(request.StockCountId, cancellationToken);
            if (count is null)
                return Result.Failure<StockCountDto, Error>(Errors.General.NotFound(request.StockCountId.ToString(), nameof(StockCount)));

            if (count.Status != StockCountStatus.InProgress)
                return Result.Failure<StockCountDto, Error>(
                    Errors.Warehouse.CountCannotBeChanged($"Counts can only be entered while the sheet is in progress; this one is {count.Status}."));

            var byId = count.Items.ToDictionary(i => i.Id);

            foreach (var line in request.Lines)
            {
                if (!byId.TryGetValue(line.StockCountItemId, out var sheetLine))
                    return Result.Failure<StockCountDto, Error>(Errors.Warehouse.LineIsNotOnTheCount(line.StockCountItemId));

                try
                {
                    if (line.IsRecount)
                        count.RecordStockCountItemRecount(sheetLine.Id, line.CountedQuantity);
                    else
                        count.RecordStockCountItemCount(sheetLine.Id, line.CountedQuantity);
                }
                catch (InvalidOperationException exception)
                {
                    return Result.Failure<StockCountDto, Error>(Errors.Warehouse.CountCannotBeChanged(exception.Message));
                }

                if (!string.IsNullOrWhiteSpace(line.DiscrepancyReason))
                    count.ExplainStockCountItemDiscrepancy(sheetLine.Id, line.DiscrepancyReason);
            }

            // Submitted only once every line has been counted, which is what the flag asks for.
            // Failing instead would discard the counts entered in this request along with the
            // rest of the transaction, and a sheet counted over three days would never survive.
            // The status comes back on the response, so a caller expecting a submission that did
            // not happen can see that it did not.
            if (request.SubmitWhenComplete && count.Items.All(i => i.IsCounted))
                count.SubmitForReview();

            await stockCounts.UpdateAsync(count, cancellationToken);

            return Result.Success<StockCountDto, Error>(count.ToDto());
        }
    }
}
