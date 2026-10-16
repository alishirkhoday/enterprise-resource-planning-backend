using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.SettleAccount
{
    public class SettleAccountCommandValidator : AbstractValidator<SettleAccountCommand>
    {
        public SettleAccountCommandValidator()
        {
            RuleFor(r => r.Direction).IsInEnum();
            RuleFor(r => r.Method).IsInEnum();
            RuleFor(r => r.PartyDetailAccountId).NotEmpty().WithMessage("Whose account does it move?");
            RuleFor(r => r.Amount).GreaterThan(0m).WithMessage("A settlement of nothing settles nothing.");

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.SettlementDate).NotEqual(default(DateOnly)).WithMessage("When did the money move?");

            // Money received in cash with no cash box, or by transfer with no bank account, is
            // money nobody can trace to where it actually landed.
            RuleFor(r => r)
                .Must(r => r.Method != SettlementMethod.Cash || r.CashBoxId is not null)
                .WithMessage("Cash has to name the cash box it went into or out of.");

            RuleFor(r => r)
                .Must(r => r.Method != SettlementMethod.Cheque || r.ChequeId is not null)
                .WithMessage("A cheque settlement has to name the cheque.");

            RuleFor(r => r)
                .Must(r => r.Method != SettlementMethod.BankTransfer || r.BankBranchAccountId is not null)
                .WithMessage("A transfer has to name the bank account it went into or out of.");

            // The domain refuses this too, as an exception. Saying it here names the two ways out.
            RuleFor(r => r)
                .Must(r => r.Allocations.Count > 0 || r.Method == SettlementMethod.Advance)
                .WithMessage("Say what the money pays off, or record it as an advance.");

            RuleFor(r => r.Allocations)
                .Must(a => a.Where(x => x.ReceivableId is not null).Select(x => x.ReceivableId).Distinct().Count()
                           == a.Count(x => x.ReceivableId is not null))
                .WithMessage("The same receivable is settled twice in one request; add the amounts together.");

            RuleFor(r => r.Allocations)
                .Must(a => a.Where(x => x.PayableId is not null).Select(x => x.PayableId).Distinct().Count()
                           == a.Count(x => x.PayableId is not null))
                .WithMessage("The same payable is settled twice in one request; add the amounts together.");

            RuleForEach(r => r.Allocations).SetValidator(new SettlementAllocationValidator());
        }
    }

    public class SettlementAllocationValidator : AbstractValidator<SettlementAllocationDto>
    {
        public SettlementAllocationValidator()
        {
            RuleFor(a => a.Amount).GreaterThan(0m).WithMessage("Applying nothing against an item is not an allocation.");

            // One or the other, never both and never neither: money goes against a debt in one
            // direction, and a line naming both cannot be applied to either.
            RuleFor(a => a)
                .Must(a => a.ReceivableId is not null ^ a.PayableId is not null)
                .WithMessage("An allocation names either a receivable or a payable, not both and not neither.");

            RuleFor(a => a.DiscountTaken)
                .GreaterThan(0m).WithMessage("A discount of nothing is not a discount.")
                .When(a => a.DiscountTaken is not null);

            RuleFor(a => a.Description).MaximumLength(500);
        }
    }
}
