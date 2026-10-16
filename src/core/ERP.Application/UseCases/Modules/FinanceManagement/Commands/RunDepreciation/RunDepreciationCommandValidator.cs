using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.RunDepreciation
{
    public class RunDepreciationCommandValidator : AbstractValidator<RunDepreciationCommand>
    {
        public RunDepreciationCommandValidator()
        {
            RuleFor(r => r.PeriodStart).NotEqual(default(DateOnly)).WithMessage("When does the period start?");
            RuleFor(r => r.PeriodEnd).NotEqual(default(DateOnly)).WithMessage("When does it end?");

            RuleFor(r => r.PeriodEnd)
                .GreaterThanOrEqualTo(r => r.PeriodStart)
                .WithMessage("A period cannot end before it starts.");

            // A run covering more than a year is almost always a typed date, and charging a year
            // of depreciation in one entry is not something to do by accident.
            RuleFor(r => r)
                .Must(r => r.PeriodEnd.DayNumber - r.PeriodStart.DayNumber <= 366)
                .WithMessage("A depreciation run covers at most one year; check the dates.");

            RuleFor(r => r.Production)
                .Must(p => p.Select(x => x.FixedAssetId).Distinct().Count() == p.Count)
                .WithMessage("The same asset reports production twice.");

            RuleForEach(r => r.Production).SetValidator(new AssetProductionValidator());
        }
    }

    public class AssetProductionValidator : AbstractValidator<AssetProductionDto>
    {
        public AssetProductionValidator()
        {
            RuleFor(p => p.FixedAssetId).NotEmpty().WithMessage("Which asset?");
            RuleFor(p => p.UnitsProduced).GreaterThanOrEqualTo(0m).WithMessage("Production cannot be negative.");
        }
    }
}
