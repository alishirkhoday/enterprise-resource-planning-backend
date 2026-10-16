using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Mapping
{
    /// <summary>
    /// One place that turns a journal entry into what a caller sees, so the create, post and
    /// reverse endpoints cannot drift into describing the same document differently.
    /// </summary>
    public static class JournalEntryMapper
    {
        public static JournalEntryDto ToDto(this Document document)
        {
            ArgumentNullException.ThrowIfNull(document, nameof(document));

            return new JournalEntryDto(
                document.Id,
                document.Number,
                document.Title,
                document.Type,
                document.Status,
                document.FinancialEventDateTime,
                document.CurrencyCode,
                document.TotalDebtor,
                document.TotalCreditor,
                document.IsBalanced(),
                document.PostedOn,
                document.PostedBy,
                document.ReversedByDocumentId,
                [.. document.Items
                    .OrderBy(i => i.Priority)
                    .Select(i => new JournalEntryLineViewDto(
                        i.Id,
                        i.Priority,
                        i.AccountId,
                        i.Account.Code,
                        i.Account.Name,
                        i.Debtor,
                        i.Creditor,
                        i.Description))]);
        }
    }
}
