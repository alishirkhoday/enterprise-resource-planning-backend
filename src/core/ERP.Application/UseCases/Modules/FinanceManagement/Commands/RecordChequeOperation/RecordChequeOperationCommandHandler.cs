using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Mapping;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.RecordChequeOperation
{
    /// <summary>
    /// Moves a cheque to its next state.
    /// <para>
    /// Which moves are legal from which state lives on the cheque, not here. A cheque that has
    /// already bounced cannot then clear, and the entity is the only place that rule can be kept
    /// in one piece - so this translates the request into a call and translates a refusal back.
    /// </para>
    /// </summary>
    public class RecordChequeOperationCommandHandler(IChequeRepository cheques)
        : IRequestHandler<RecordChequeOperationCommand, Result<ChequeDto, Error>>
    {
        public async Task<Result<ChequeDto, Error>> Handle(RecordChequeOperationCommand request, CancellationToken cancellationToken)
        {
            var cheque = await cheques.GetByIdAsync(request.ChequeId, cancellationToken);
            if (cheque is null)
                return Result.Failure<ChequeDto, Error>(Errors.General.NotFound(request.ChequeId.ToString(), nameof(Cheque)));

            try
            {
                switch (request.Operation)
                {
                    case ChequeOperationKind.Deposit:
                        cheque.Deposit(request.BankAccountId!.Value, request.OccurredOn);
                        break;

                    case ChequeOperationKind.Clear:
                        cheque.Clear(request.OccurredOn);
                        break;

                    case ChequeOperationKind.Bounce:
                        cheque.Bounce(request.OccurredOn, request.Reason!);
                        break;

                    case ChequeOperationKind.Endorse:
                        cheque.Endorse(request.OccurredOn, request.EndorsedTo!);
                        break;

                    case ChequeOperationKind.Issue:
                        cheque.Issue(request.OccurredOn);
                        break;

                    case ChequeOperationKind.Return:
                        cheque.ReturnToHolder(request.OccurredOn);
                        break;

                    case ChequeOperationKind.Cancel:
                        cheque.Cancel(request.OccurredOn, request.Reason);
                        break;

                    default:
                        return Result.Failure<ChequeDto, Error>(Errors.General.AnErrorHasOccurred());
                }
            }
            catch (InvalidOperationException exception)
            {
                // The cheque refused the move. Its message says which state it is in and why that
                // is not a state the move can be made from, which is exactly what the caller needs.
                return Result.Failure<ChequeDto, Error>(Errors.Finance.ChequeOperationIsNotAllowed(exception.Message));
            }

            await cheques.UpdateAsync(cheque, cancellationToken);

            return Result.Success<ChequeDto, Error>(cheque.ToDto());
        }
    }
}
