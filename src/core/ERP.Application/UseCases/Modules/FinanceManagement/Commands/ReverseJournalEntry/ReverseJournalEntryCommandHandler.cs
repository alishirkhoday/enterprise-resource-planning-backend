using ERP.Application.Common.Interfaces.Identity;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Mapping;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.ReverseJournalEntry
{
    /// <summary>
    /// Writes the mirror image of a posted entry and links the two together.
    /// <para>
    /// The reversal is posted immediately rather than left as a draft. A half-undone correction
    /// sitting in someone's queue is the worst of both states: the original is still in the ledger
    /// and the books say something nobody believes.
    /// </para>
    /// </summary>
    public class ReverseJournalEntryCommandHandler(
        IDocumentRepository documents,
        IFinancialYearRepository financialYears,
        INumberingSeriesRepository numberingSeries,
        ICurrentUser currentUser)
        : IRequestHandler<ReverseJournalEntryCommand, Result<JournalEntryDto, Error>>
    {
        private const string JournalNumberingKey = "JournalEntry";

        public async Task<Result<JournalEntryDto, Error>> Handle(ReverseJournalEntryCommand request, CancellationToken cancellationToken)
        {
            var original = await documents.GetWithItemsAsync(request.DocumentId, cancellationToken);
            if (original is null)
                return Result.Failure<JournalEntryDto, Error>(Errors.General.NotFound(request.DocumentId.ToString(), nameof(Document)));

            if (original.Status != DocumentStatus.Definite)
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.OnlyAPostedEntryCanBeReversed(original.Status.ToString()));

            if (original.ReversedByDocumentId is not null)
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.EntryIsAlreadyReversed());

            var reverseOn = request.ReverseOn ?? original.FinancialEventDateTime;
            var reverseDate = DateOnly.FromDateTime(reverseOn.UtcDateTime);

            var financialYear = await financialYears.GetByDateAsync(reverseDate, original.BranchId, cancellationToken);
            if (financialYear is null)
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.NoFinancialYearForDate(reverseDate));

            if (!financialYear.AcceptsPostings)
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.FinancialYearIsClosed(financialYear.Title));

            var period = await financialYears.GetPeriodByDateAsync(reverseDate, original.BranchId, cancellationToken);
            if (period is not null && period.Status != AccountingPeriodStatus.Open)
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.AccountingPeriodIsClosed(period.Title));

            var reversal = new Document(
                financialYear,
                $"Reversal of {original.Number ?? original.Title}",
                reverseOn,
                request.Reason.Trim(),
                original.Type);

            if (original.BranchId is not null)
                reversal.AssignToBranch(original.BranchId.Value);

            reversal.MarkAsReversalOf(original.Id);

            // Debits become credits and credits become debits, line for line. Subsidiary accounts,
            // cost centres and projects come across unchanged: the reversal has to land on exactly
            // the same analysis as the original, or the two will not cancel in any report.
            short priority = 1;
            foreach (var line in original.Items.OrderBy(i => i.Priority))
            {
                var mirrored = new DocumentItem(reversal, line.Account, priority++, line.Creditor, line.Debtor);
                mirrored.SetDetailAccounts(line.DetailAccount1, line.DetailAccount2, line.DetailAccount3);

                if (line.CostCenterId is not null)
                    mirrored.AssignCostCenter(line.CostCenterId.Value);

                if (line.ProjectId is not null)
                    mirrored.AssignProject(line.ProjectId.Value);

                mirrored.SetDescription(line.Description);
                reversal.AddItem(mirrored);
            }

            var series = await numberingSeries.GetForUpdateAsync(
                JournalNumberingKey,
                original.BranchId,
                financialYear.Id,
                cancellationToken);

            if (series is not null)
                reversal.SetNumber(series.Next(reverseDate));

            reversal.Post(currentUser.UserName);
            original.MarkAsReversed(reversal.Id);

            await documents.CreateAsync(reversal, cancellationToken);
            await documents.UpdateAsync(original, cancellationToken);

            return Result.Success<JournalEntryDto, Error>(reversal.ToDto());
        }
    }
}
