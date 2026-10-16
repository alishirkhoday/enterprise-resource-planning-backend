using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Mapping;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.CreateJournalEntry
{
    /// <summary>
    /// Opens a journal entry as a draft. It is not posted here: an entry that could be written and
    /// made final in one step would leave no moment in which somebody looks at it, and a posted
    /// entry can only be reversed afterwards, never corrected.
    /// </summary>
    public class CreateJournalEntryCommandHandler(
        IFinancialYearRepository financialYears,
        IAccountRepository accounts,
        IDetailAccountRepository detailAccounts,
        IDocumentRepository documents,
        INumberingSeriesRepository numberingSeries)
        : IRequestHandler<CreateJournalEntryCommand, Result<JournalEntryDto, Error>>
    {
        private const string JournalNumberingKey = "JournalEntry";

        public async Task<Result<JournalEntryDto, Error>> Handle(CreateJournalEntryCommand request, CancellationToken cancellationToken)
        {
            var eventDate = DateOnly.FromDateTime(request.FinancialEventDateTime.UtcDateTime);

            var financialYear = await financialYears.GetByDateAsync(eventDate, request.BranchId, cancellationToken);
            if (financialYear is null)
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.NoFinancialYearForDate(eventDate));

            if (!financialYear.AcceptsPostings)
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.FinancialYearIsClosed(financialYear.Title));

            // The period is checked even though the entry is only a draft. Writing a draft into a
            // month that is already closed simply moves the argument to posting time, by which
            // point somebody has done the work of entering it.
            var period = await financialYears.GetPeriodByDateAsync(eventDate, request.BranchId, cancellationToken);
            if (period is not null && period.Status != AccountingPeriodStatus.Open)
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.AccountingPeriodIsClosed(period.Title));

            var document = new Document(
                financialYear,
                request.Title,
                request.FinancialEventDateTime,
                request.Description,
                request.Type);

            if (request.BranchId is not null)
                document.AssignToBranch(request.BranchId.Value);

            if (!string.IsNullOrWhiteSpace(request.ReferenceType) && !string.IsNullOrWhiteSpace(request.ReferenceId))
                document.SetReference(request.ReferenceType, request.ReferenceId);

            var lines = await BuildLinesAsync(document, request, cancellationToken);
            if (lines.IsFailure)
                return Result.Failure<JournalEntryDto, Error>(lines.Error);

            foreach (var line in lines.Value)
            {
                document.AddItem(line);
            }

            var series = await numberingSeries.GetForUpdateAsync(
                JournalNumberingKey,
                request.BranchId,
                financialYear.Id,
                cancellationToken);

            // A draft may go without a number. Numbers come from a series that has to run without
            // gaps, and spending one on an entry that might be abandoned is what creates them.
            if (series is not null)
                document.SetNumber(series.Next(eventDate));

            await documents.CreateAsync(document, cancellationToken);

            return Result.Success<JournalEntryDto, Error>(document.ToDto());
        }

        /// <summary>
        /// Builds every line before any is attached. A document half filled with lines, abandoned
        /// because the seventh account turned out to be locked, is harder to explain than a clean
        /// refusal.
        /// </summary>
        private async Task<Result<List<DocumentItem>, Error>> BuildLinesAsync(
            Document document,
            CreateJournalEntryCommand request,
            CancellationToken cancellationToken)
        {
            var built = new List<DocumentItem>(request.Lines.Count);
            short priority = 1;

            foreach (var line in request.Lines)
            {
                var account = await accounts.GetWithDetailGroupsAsync(line.AccountId, cancellationToken);
                if (account is null)
                    return Result.Failure<List<DocumentItem>, Error>(Errors.Account.AccountNotFound());

                if (!account.AcceptsPostings)
                    return Result.Failure<List<DocumentItem>, Error>(Errors.Finance.AccountDoesNotAcceptPostings(account.Code));

                var item = new DocumentItem(document, account, priority++, line.Debtor, line.Creditor);

                var subsidiaries = await ResolveDetailAccountsAsync(line, cancellationToken);
                if (subsidiaries.IsFailure)
                    return Result.Failure<List<DocumentItem>, Error>(subsidiaries.Error);

                try
                {
                    // The account declares which subsidiary groups its lines carry, and the domain
                    // is what knows whether these satisfy that.
                    item.SetDetailAccounts(subsidiaries.Value.Level1, subsidiaries.Value.Level2, subsidiaries.Value.Level3);
                }
                catch (InvalidOperationException exception)
                {
                    return Result.Failure<List<DocumentItem>, Error>(Errors.Finance.SubsidiaryAccountsAreInvalid(account.Code, exception.Message));
                }

                if (line.CostCenterId is not null)
                    item.AssignCostCenter(line.CostCenterId.Value);

                if (line.ProjectId is not null)
                    item.AssignProject(line.ProjectId.Value);

                item.SetDescription(line.Description);
                built.Add(item);
            }

            return Result.Success<List<DocumentItem>, Error>(built);
        }

        private async Task<Result<(DetailAccount? Level1, DetailAccount? Level2, DetailAccount? Level3), Error>> ResolveDetailAccountsAsync(
            JournalEntryLineDto line,
            CancellationToken cancellationToken)
        {
            var level1 = await LoadAsync(line.DetailAccount1Id, cancellationToken);
            if (level1.IsFailure)
                return Result.Failure<(DetailAccount?, DetailAccount?, DetailAccount?), Error>(level1.Error);

            var level2 = await LoadAsync(line.DetailAccount2Id, cancellationToken);
            if (level2.IsFailure)
                return Result.Failure<(DetailAccount?, DetailAccount?, DetailAccount?), Error>(level2.Error);

            var level3 = await LoadAsync(line.DetailAccount3Id, cancellationToken);
            if (level3.IsFailure)
                return Result.Failure<(DetailAccount?, DetailAccount?, DetailAccount?), Error>(level3.Error);

            return Result.Success<(DetailAccount?, DetailAccount?, DetailAccount?), Error>(
                (level1.Value, level2.Value, level3.Value));
        }

        private async Task<Result<DetailAccount?, Error>> LoadAsync(Guid? id, CancellationToken cancellationToken)
        {
            if (id is null)
                return Result.Success<DetailAccount?, Error>(null);

            var detailAccount = await detailAccounts.GetByIdAsync(id.Value, cancellationToken);

            return detailAccount is null
                ? Result.Failure<DetailAccount?, Error>(Errors.General.NotFound(id.Value.ToString(), nameof(DetailAccount)))
                : Result.Success<DetailAccount?, Error>(detailAccount);
        }
    }
}
