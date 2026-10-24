using ERP.Application.UseCases.Modules.PricingManagement.Commands.CreatePriceList;
using ERP.Application.UseCases.Modules.PricingManagement.DTOs;
using ERP.Application.UseCases.Modules.PricingManagement.Mapping;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;

namespace ERP.Application.UseCases.Modules.PricingManagement.Commands.SetPriceListItems
{
    /// <summary>
    /// Puts prices on a list - به‌روزرسانی قیمت‌ها.
    /// <para>
    /// By default this merges: a line for an item at a quantity break that already exists is
    /// restated, and anything not mentioned is left alone. That is what a price change from a
    /// supplier normally looks like - a handful of items, not the whole catalogue.
    /// </para>
    /// <para>
    /// <c>ReplaceAll</c> is for the other case: a complete price file, where an item that has
    /// dropped off should stop being priced rather than quietly keep its old figure.
    /// </para>
    /// </summary>
    public class SetPriceListItemsCommandHandler(
        IPriceListRepository priceLists,
        IItemRepository items)
        : IRequestHandler<SetPriceListItemsCommand, Result<PriceListDto, Error>>
    {
        public async Task<Result<PriceListDto, Error>> Handle(SetPriceListItemsCommand request, CancellationToken cancellationToken)
        {
            var priceList = await priceLists.GetWithItemsAsync(request.PriceListId, cancellationToken);
            if (priceList is null)
                return Result.Failure<PriceListDto, Error>(Errors.General.NotFound(request.PriceListId.ToString(), nameof(PriceList)));

            // Every line is built and validated before anything on the list is touched, so a bad
            // request cannot leave a list half priced.
            var built = await CreatePriceListCommandHandler.BuildLinesInternalAsync(
                items, priceList, request.Lines, cancellationToken);

            if (built.IsFailure)
                return Result.Failure<PriceListDto, Error>(built.Error);

            try
            {
                priceList.SetItems(built.Value, request.ReplaceAll);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<PriceListDto, Error>(Errors.Pricing.PriceListCannotBeChanged(exception.Message));
            }

            await priceLists.UpdateAsync(priceList, cancellationToken);

            return Result.Success<PriceListDto, Error>(priceList.ToDto());
        }
    }
}
