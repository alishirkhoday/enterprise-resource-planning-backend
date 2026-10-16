using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.SettleAccount
{
    /// <summary>
    /// Records money in or out and applies it against what it pays off - تسویه حساب.
    /// <para>
    /// Receiving the money and deciding which invoices it settles are one act. Split into two
    /// steps, cash sits unapplied and a customer who has paid still appears overdue - which is
    /// how a collections list ends up chasing people who already paid.
    /// </para>
    /// <para>
    /// The open items are settled here too, in the same transaction. A settlement whose
    /// allocations did not reach the receivables would leave two records of the same money
    /// disagreeing, and the ageing report reads the receivables.
    /// </para>
    /// </summary>
    public class SettleAccountCommandHandler(
        ISettlementRepository settlements,
        IReceivableRepository receivables,
        IPayableRepository payables)
        : IRequestHandler<SettleAccountCommand, Result<SettlementDto, Error>>
    {
        public async Task<Result<SettlementDto, Error>> Handle(SettleAccountCommand request, CancellationToken cancellationToken)
        {
            var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
            var amount = Money.Create(request.Amount, currencyCode);

            var allocatedTotal = request.Allocations.Sum(a => a.Amount);

            // Checked before anything is created: applying more than was received would settle
            // debts with money that never arrived.
            if (allocatedTotal > request.Amount)
                return Result.Failure<SettlementDto, Error>(Errors.Finance.OverAllocated(allocatedTotal, request.Amount));

            Settlement settlement;

            try
            {
                settlement = new Settlement(
                    request.Direction,
                    request.Method,
                    request.PartyDetailAccountId,
                    amount,
                    request.SettlementDate);
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result.Failure<SettlementDto, Error>(Errors.Finance.SettlementCannotBeChanged(exception.Message));
            }

            var described = Describe(settlement, request);
            if (described.IsFailure)
                return Result.Failure<SettlementDto, Error>(described.Error);

            var applied = await ApplyAllocationsAsync(settlement, request, currencyCode, cancellationToken);
            if (applied.IsFailure)
                return Result.Failure<SettlementDto, Error>(applied.Error);

            try
            {
                settlement.Confirm();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<SettlementDto, Error>(Errors.Finance.SettlementCannotBeChanged(exception.Message));
            }

            await settlements.CreateAsync(settlement, cancellationToken);

            return Result.Success<SettlementDto, Error>(ToDto(settlement));
        }

        private static Result<bool, Error> Describe(Settlement settlement, SettleAccountCommand request)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(request.Number))
                    settlement.SetNumber(request.Number);

                if (request.CashBoxId is not null)
                    settlement.SetCashBox(request.CashBoxId.Value);

                if (request.BankBranchAccountId is not null)
                    settlement.SetBankAccount(request.BankBranchAccountId.Value);

                if (request.ChequeId is not null)
                    settlement.SetCheque(request.ChequeId.Value);

                if (request.DocumentId is not null)
                    settlement.LinkToDocument(request.DocumentId.Value);

                if (request.BranchId is not null)
                    settlement.AssignToBranch(request.BranchId.Value);

                settlement.SetTrackingCode(request.TrackingCode);
                settlement.SetDescription(request.Description);

                return Result.Success<bool, Error>(true);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return Result.Failure<bool, Error>(Errors.Finance.SettlementCannotBeChanged(exception.Message));
            }
        }

        /// <summary>
        /// Each allocation settles its open item as it is attached, so the settlement and the
        /// receivable can never end up disagreeing about how much of a debt is paid.
        /// </summary>
        private async Task<Result<bool, Error>> ApplyAllocationsAsync(
            Settlement settlement,
            SettleAccountCommand request,
            string currencyCode,
            CancellationToken cancellationToken)
        {
            foreach (var line in request.Allocations)
            {
                var lineAmount = Money.Create(line.Amount, currencyCode);

                try
                {
                    var allocation = new SettlementAllocation(settlement, lineAmount, line.ReceivableId, line.PayableId);

                    if (line.DiscountTaken is not null)
                        allocation.SetDiscountTaken(Money.Create(line.DiscountTaken.Value, currencyCode));

                    allocation.SetDescription(line.Description);
                    settlement.Allocate(allocation);
                }
                catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
                {
                    return Result.Failure<bool, Error>(Errors.Finance.SettlementCannotBeChanged(exception.Message));
                }

                var settled = line.ReceivableId is not null
                    ? await SettleReceivableAsync(line, lineAmount, request.SettlementDate, cancellationToken)
                    : await SettlePayableAsync(line, lineAmount, request.SettlementDate, cancellationToken);

                if (settled.IsFailure)
                    return settled;
            }

            return Result.Success<bool, Error>(true);
        }

        private async Task<Result<bool, Error>> SettleReceivableAsync(
            SettlementAllocationDto line,
            Money amount,
            DateOnly settledOn,
            CancellationToken cancellationToken)
        {
            var receivable = await receivables.GetByIdAsync(line.ReceivableId!.Value, cancellationToken);
            if (receivable is null)
                return Result.Failure<bool, Error>(Errors.General.NotFound(line.ReceivableId.Value.ToString(), nameof(Receivable)));

            if (receivable.Status is ReceivableStatus.Settled or ReceivableStatus.WrittenOff)
                return Result.Failure<bool, Error>(Errors.Finance.ItemIsNotOpen($"Receivable {receivable.Id}"));

            try
            {
                // The discount is treated as settling part of the debt too, so an invoice paid
                // early closes rather than sitting open for the discounted remainder.
                var toSettle = line.DiscountTaken is not null
                    ? amount.Add(Money.Create(line.DiscountTaken.Value, amount.CurrencyCode))
                    : amount;

                receivable.Settle(toSettle, settledOn);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
            {
                return Result.Failure<bool, Error>(Errors.Finance.SettlementCannotBeChanged(exception.Message));
            }

            await receivables.UpdateAsync(receivable, cancellationToken);

            return Result.Success<bool, Error>(true);
        }

        private async Task<Result<bool, Error>> SettlePayableAsync(
            SettlementAllocationDto line,
            Money amount,
            DateOnly settledOn,
            CancellationToken cancellationToken)
        {
            var payable = await payables.GetByIdAsync(line.PayableId!.Value, cancellationToken);
            if (payable is null)
                return Result.Failure<bool, Error>(Errors.General.NotFound(line.PayableId.Value.ToString(), nameof(Payable)));

            if (payable.IsOnHold)
                return Result.Failure<bool, Error>(Errors.Finance.ItemIsNotOpen($"Payable {payable.Id}, which is on hold"));

            try
            {
                var toSettle = line.DiscountTaken is not null
                    ? amount.Add(Money.Create(line.DiscountTaken.Value, amount.CurrencyCode))
                    : amount;

                payable.Settle(toSettle, settledOn);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
            {
                return Result.Failure<bool, Error>(Errors.Finance.SettlementCannotBeChanged(exception.Message));
            }

            await payables.UpdateAsync(payable, cancellationToken);

            return Result.Success<bool, Error>(true);
        }

        internal static SettlementDto ToDto(Settlement settlement) => new(
            settlement.Id,
            settlement.Number,
            settlement.Direction,
            settlement.Method,
            settlement.Status,
            settlement.PartyDetailAccountId,
            settlement.SettlementDate,
            settlement.Amount.CurrencyCode,
            settlement.Amount.Amount,
            settlement.AllocatedAmount().Amount,
            settlement.UnallocatedAmount().Amount,
            [.. settlement.Allocations.Select(a => new SettlementAllocationViewDto(
                a.Id,
                a.ReceivableId,
                a.PayableId,
                a.Amount.Amount,
                a.DiscountTaken?.Amount))]);
    }
}
