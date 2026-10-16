using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Mapping;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.RegisterCheque
{
    /// <summary>
    /// Records a cheque, received or issued. It only enters the books; moving it - depositing,
    /// clearing, bouncing - is a separate act with its own endpoint, because each one is a
    /// different fact about money that has or has not arrived.
    /// </summary>
    public class RegisterChequeCommandHandler(IChequeRepository cheques)
        : IRequestHandler<RegisterChequeCommand, Result<ChequeDto, Error>>
    {
        public async Task<Result<ChequeDto, Error>> Handle(RegisterChequeCommand request, CancellationToken cancellationToken)
        {
            // The Sayad number is the one identifier that is unique nationwide, so a duplicate
            // means this cheque is already in the books - probably entered by somebody else.
            if (!string.IsNullOrWhiteSpace(request.SayadNumber))
            {
                var existing = await cheques.GetBySayadNumberAsync(request.SayadNumber, cancellationToken);
                if (existing is not null)
                    return Result.Failure<ChequeDto, Error>(Errors.Finance.ChequeNumberIsUsed(request.SayadNumber));
            }

            var cheque = new Cheque(
                request.Type,
                request.Number,
                Money.Create(request.Amount, request.CurrencyCode),
                request.IssueDate,
                request.DueDate);

            if (!string.IsNullOrWhiteSpace(request.SayadNumber))
                cheque.SetSayadNumber(request.SayadNumber);

            if (request.Type == ChequeType.Received)
                cheque.SetDrawer(request.BankName, request.DrawerName, request.AccountNumber);
            else if (!string.IsNullOrWhiteSpace(request.PayeeName))
                cheque.SetPayee(request.PayeeName);

            if (request.DetailAccountId is not null)
                cheque.LinkToDetailAccount(request.DetailAccountId.Value);

            if (request.BranchId is not null)
                cheque.AssignToBranch(request.BranchId.Value);

            cheque.SetDescription(request.Description);

            await cheques.CreateAsync(cheque, cancellationToken);

            return Result.Success<ChequeDto, Error>(cheque.ToDto());
        }
    }
}
