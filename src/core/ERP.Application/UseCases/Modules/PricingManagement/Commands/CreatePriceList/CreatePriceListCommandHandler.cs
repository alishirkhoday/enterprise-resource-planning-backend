using ERP.Application.UseCases.Modules.PricingManagement.DTOs;
using ERP.Application.UseCases.Modules.PricingManagement.Mapping;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;

namespace ERP.Application.UseCases.Modules.PricingManagement.Commands.CreatePriceList
{
    /// <summary>
    /// Sets up a price list - لیست قیمت.
    /// <para>
    /// Price lists are the reason an invoice can refuse to let a salesperson quietly undercut an
    /// agreed price. Until now they could only be seeded by hand, which meant the safeguard was
    /// only as good as whoever last touched the database.
    /// </para>
    /// </summary>
    public class CreatePriceListCommandHandler(
        IPriceListRepository priceLists,
        IItemRepository items)
        : IRequestHandler<CreatePriceListCommand, Result<PriceListDto, Error>>
    {
        public async Task<Result<PriceListDto, Error>> Handle(CreatePriceListCommand request, CancellationToken cancellationToken)
        {
            var code = request.Code.Trim();

            var existing = await priceLists.GetByCodeAsync(code, cancellationToken);
            if (existing is not null)
                return Result.Failure<PriceListDto, Error>(Errors.Pricing.PriceListCodeIsUsed(code));

            try
            {
                var priceList = new PriceList(
                    code,
                    request.Name,
                    request.Type,
                    request.CurrencyCode.Trim().ToUpperInvariant(),
                    DateRange.Create(request.StartDate, request.EndDate));

                if (request.CustomerGroupId is not null)
                    priceList.RestrictToCustomerGroup(request.CustomerGroupId.Value);

                if (request.BranchId is not null)
                    priceList.AssignToBranch(request.BranchId.Value);

                if (request.IsTaxInclusive)
                    priceList.MarkAsTaxInclusive();

                priceList.SetDescription(request.Description);

                var built = await BuildLinesAsync(priceList, request.Lines, cancellationToken);
                if (built.IsFailure)
                    return Result.Failure<PriceListDto, Error>(built.Error);

                foreach (var line in built.Value)
                {
                    priceList.AddItem(line);
                }

                if (request.IsDefault)
                {
                    // Only one list can be the fallback. Taking it off whichever holds it now is
                    // what stops two defaults existing and the resolution picking arbitrarily.
                    var cleared = await ClearExistingDefaultAsync(request.StartDate, cancellationToken);
                    if (cleared.IsFailure)
                        return Result.Failure<PriceListDto, Error>(cleared.Error);

                    priceList.MarkAsDefault();
                }

                await priceLists.CreateAsync(priceList, cancellationToken);

                return Result.Success<PriceListDto, Error>(priceList.ToDto());
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
            {
                return Result.Failure<PriceListDto, Error>(Errors.Pricing.PriceListCannotBeChanged(exception.Message));
            }
        }

        internal static async Task<Result<List<PriceListItem>, Error>> BuildLinesInternalAsync(
            IItemRepository items,
            PriceList priceList,
            IReadOnlyList<PriceListLineDto> lines,
            CancellationToken cancellationToken)
        {
            var built = new List<PriceListItem>(lines.Count);
            var found = await items.GetByIdsAsync([.. lines.Select(l => l.ItemId)], cancellationToken);
            var byId = found.ToDictionary(i => i.Id);

            foreach (var line in lines)
            {
                if (!byId.TryGetValue(line.ItemId, out var item))
                    return Result.Failure<List<PriceListItem>, Error>(Errors.General.NotFound(line.ItemId.ToString(), nameof(Item)));

                built.Add(line.ToEntity(priceList, item));
            }

            return Result.Success<List<PriceListItem>, Error>(built);
        }

        private Task<Result<List<PriceListItem>, Error>> BuildLinesAsync(
            PriceList priceList,
            IReadOnlyList<PriceListLineDto> lines,
            CancellationToken cancellationToken) =>
            BuildLinesInternalAsync(items, priceList, lines, cancellationToken);

        private async Task<Result<bool, Error>> ClearExistingDefaultAsync(DateOnly onDate, CancellationToken cancellationToken)
        {
            var active = await priceLists.GetActiveOnAsync(onDate, cancellationToken);

            foreach (var other in active.Where(p => p.IsDefault))
            {
                other.UnmarkAsDefault();
                await priceLists.UpdateAsync(other, cancellationToken);
            }

            return Result.Success<bool, Error>(true);
        }
    }
}
