using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.RunDepreciation
{
    /// <summary>
    /// Charges depreciation for a period - محاسبه استهلاک.
    /// <para>
    /// A period close activity, not a per asset one: everything in service is charged together,
    /// because a month with one machine missed is wrong for the whole company, not for that
    /// machine. Assets the run passes over are reported rather than dropped, since an asset
    /// silently missing from a depreciation run is what nobody notices for a year.
    /// </para>
    /// <para>
    /// The charge itself is the domain's - <c>CalculateDepreciationForPeriod</c> knows about
    /// salvage floors and the three methods. This decides what is in scope and what to do when
    /// an asset refuses the charge.
    /// </para>
    /// </summary>
    public class RunDepreciationCommandHandler(IFixedAssetRepository fixedAssets)
        : IRequestHandler<RunDepreciationCommand, Result<DepreciationRunDto, Error>>
    {
        public async Task<Result<DepreciationRunDto, Error>> Handle(RunDepreciationCommand request, CancellationToken cancellationToken)
        {
            var period = DateRange.Create(request.PeriodStart, request.PeriodEnd);

            var due = await fixedAssets.GetDueForDepreciationAsync(request.PeriodEnd, cancellationToken);

            var scoped = due
                .Where(a => request.CategoryId is null || a.CategoryId == request.CategoryId)
                .Where(a => request.BranchId is null || a.BranchId == request.BranchId)
                .ToArray();

            if (scoped.Length == 0)
                return Result.Failure<DepreciationRunDto, Error>(Errors.Finance.NothingToDepreciate(request.PeriodEnd));

            var unitsByAsset = request.Production.ToDictionary(p => p.FixedAssetId, p => p.UnitsProduced);

            var charges = new List<DepreciationChargeDto>(scoped.Length);
            var skipped = new List<DepreciationSkipDto>();
            var currencyCode = scoped[0].AcquisitionCost.CurrencyCode;

            foreach (var asset in scoped)
            {
                // Assets in another currency are left out rather than added into a total that
                // would then mean nothing. They are reported, so somebody runs them separately.
                if (asset.AcquisitionCost.CurrencyCode != currencyCode)
                {
                    skipped.Add(new DepreciationSkipDto(asset.Id, asset.Code,
                        $"Carried in {asset.AcquisitionCost.CurrencyCode}, not {currencyCode}."));
                    continue;
                }

                if (await fixedAssets.HasDepreciationForPeriodAsync(asset.Id, request.PeriodStart, request.PeriodEnd, cancellationToken))
                {
                    skipped.Add(new DepreciationSkipDto(asset.Id, asset.Code, "Already charged for this period."));
                    continue;
                }

                var units = unitsByAsset.GetValueOrDefault(asset.Id);
                var charge = asset.CalculateDepreciationForPeriod(units);

                // A fully depreciated asset computes to zero and the domain refuses a zero entry,
                // so it is passed over here with a reason rather than blowing up the run.
                if (!charge.IsPositive)
                {
                    skipped.Add(new DepreciationSkipDto(asset.Id, asset.Code, "Nothing left to depreciate in this period."));
                    continue;
                }

                if (!request.PreviewOnly)
                {
                    var recorded = RecordCharge(asset, period, charge, units, request);
                    if (recorded.IsFailure)
                    {
                        skipped.Add(new DepreciationSkipDto(asset.Id, asset.Code, recorded.Error.Message));
                        continue;
                    }

                    await fixedAssets.UpdateAsync(asset, cancellationToken);
                }

                charges.Add(new DepreciationChargeDto(
                    asset.Id,
                    asset.Code,
                    asset.Name,
                    asset.DepreciationMethod,
                    charge.Amount,
                    // On a preview the asset was not touched, so the "after" figures are worked
                    // out rather than read: they are what would happen, not what did.
                    request.PreviewOnly ? asset.AccumulatedDepreciation.Amount + charge.Amount : asset.AccumulatedDepreciation.Amount,
                    request.PreviewOnly ? asset.BookValue.Amount - charge.Amount : asset.BookValue.Amount,
                    asset.Status == FixedAssetStatus.FullyDepreciated));
            }

            return Result.Success<DepreciationRunDto, Error>(new DepreciationRunDto(
                request.PeriodStart,
                request.PeriodEnd,
                request.PreviewOnly,
                currencyCode,
                charges.Count,
                charges.Sum(c => c.Charge),
                skipped.Count,
                charges,
                skipped));
        }

        private static Result<bool, Error> RecordCharge(
            FixedAsset asset,
            DateRange period,
            Money charge,
            decimal units,
            RunDepreciationCommand request)
        {
            try
            {
                var entry = new DepreciationEntry(asset, period, charge);

                if (units > 0m)
                    entry.SetUnitsProduced(units);

                if (request.DocumentId is not null)
                    entry.LinkToDocument(request.DocumentId.Value);

                if (request.AccountingPeriodId is not null)
                    entry.LinkToAccountingPeriod(request.AccountingPeriodId.Value);

                // This is what moves the accumulated figure and flips the asset to fully
                // depreciated once the book value reaches salvage.
                asset.AddDepreciationEntry(entry);

                return Result.Success<bool, Error>(true);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
            {
                return Result.Failure<bool, Error>(Errors.Finance.DepreciationCannotBeCharged(exception.Message));
            }
        }
    }
}
