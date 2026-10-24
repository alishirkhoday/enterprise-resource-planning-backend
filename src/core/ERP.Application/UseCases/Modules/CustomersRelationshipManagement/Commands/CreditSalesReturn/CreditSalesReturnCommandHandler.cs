using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Mapping;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CreditSalesReturn
{
    /// <summary>
    /// Credits a sale that came back - برگشت از فروش.
    /// <para>
    /// A credit note is a new invoice of its own type, not an edit to the original. The original
    /// has already been filed with the tax authority and reported in a period that may be closed;
    /// changing it would rewrite a number somebody else has already acted on.
    /// </para>
    /// </summary>
    public class CreditSalesReturnCommandHandler(
        IInvoiceRepository invoices,
        INumberingSeriesRepository numberingSeries)
        : IRequestHandler<CreditSalesReturnCommand, Result<InvoiceDto, Error>>
    {
        private const string CreditNoteNumberingKey = "CreditNote";

        public async Task<Result<InvoiceDto, Error>> Handle(CreditSalesReturnCommand request, CancellationToken cancellationToken)
        {
            var original = await invoices.GetWithItemsAsync(request.OriginalInvoiceId, cancellationToken);
            if (original is null)
                return Result.Failure<InvoiceDto, Error>(Errors.General.NotFound(request.OriginalInvoiceId.ToString(), nameof(Invoice)));

            if (original.Type != InvoiceType.Sales)
                return Result.Failure<InvoiceDto, Error>(Errors.Sales.OnlyASalesInvoiceCanBeCredited(original.Type.ToString()));

            // Crediting something that was never issued is correcting a draft, which is done by
            // editing it. Crediting a cancelled invoice credits money that was never owed.
            if (original.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
                return Result.Failure<InvoiceDto, Error>(Errors.Sales.InvoiceCannotBeCredited(original.Status.ToString()));

            if (request.IssueDate < original.IssueDate)
                return Result.Failure<InvoiceDto, Error>(Errors.Sales.CreditPredatesTheInvoice(original.IssueDate));

            var lines = ResolveLines(original, request);
            if (lines.IsFailure)
                return Result.Failure<InvoiceDto, Error>(lines.Error);

            var series = await numberingSeries.GetForUpdateAsync(CreditNoteNumberingKey, original.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<InvoiceDto, Error>(Errors.Order.NumberingSeriesMissing(CreditNoteNumberingKey));

            var creditNote = new Invoice(
                original.Customer,
                series.Next(request.IssueDate),
                InvoiceType.CreditNote,
                request.IssueDate,
                request.IssueDate,
                original.CurrencyCode);

            creditNote.LinkToOriginalInvoice(original.Id);
            creditNote.SetDescription(request.Reason.Trim());

            if (original.BranchId is not null)
                creditNote.AssignToBranch(original.BranchId.Value);

            if (original.SalesRepHumanId is not null)
                creditNote.AssignSalesRep(original.SalesRepHumanId.Value);

            short lineNumber = 1;
            foreach (var (source, quantity) in lines.Value)
            {
                // Priced and taxed exactly as the original was. A credit at today's price would
                // return the customer a different amount from the one they paid.
                var line = new InvoiceItem(creditNote, source.Item, lineNumber++, source.ItemUnitPrice, quantity, source.UnitOfMeasureId);
                creditNote.AddItem(line);

                if (source.TaxId is not null)
                    creditNote.SetInvoiceItemTax(line.Id, source.TaxId.Value, source.TaxPercentage);

                if (source.ItemBatchId is not null)
                    creditNote.SetInvoiceItemBatch(line.Id, source.ItemBatchId.Value);

                creditNote.SetInvoiceItemDescription(line.Id, source.Description);
            }

            // The header discount comes across too, so a partly discounted invoice is credited at
            // what the customer actually paid rather than at list price.
            creditNote.SetHeaderDiscount(original.HeaderDiscountPercentage);
            creditNote.Issue();

            await invoices.CreateAsync(creditNote, cancellationToken);

            return Result.Success<InvoiceDto, Error>(creditNote.ToDto());
        }

        /// <summary>
        /// Named lines, or the whole invoice when none are named. Returning more than went out is
        /// refused: it is either the wrong invoice or the wrong quantity, and both need a person.
        /// </summary>
        private static Result<List<(InvoiceItem Source, decimal Quantity)>, Error> ResolveLines(
            Invoice original,
            CreditSalesReturnCommand request)
        {
            if (request.Lines.Count == 0)
                return Result.Success<List<(InvoiceItem, decimal)>, Error>(
                    [.. original.Items.Select(i => (i, i.Quantity))]);

            var byId = original.Items.ToDictionary(i => i.Id);
            var resolved = new List<(InvoiceItem, decimal)>(request.Lines.Count);

            foreach (var line in request.Lines)
            {
                if (!byId.TryGetValue(line.InvoiceItemId, out var source))
                    return Result.Failure<List<(InvoiceItem, decimal)>, Error>(
                        Errors.Sales.LineIsNotOnTheInvoice(line.InvoiceItemId));

                if (line.Quantity > source.Quantity)
                    return Result.Failure<List<(InvoiceItem, decimal)>, Error>(
                        Errors.Sales.MoreReturnedThanSold(source.Item.Name ?? source.ItemId.ToString(), line.Quantity, source.Quantity));

                resolved.Add((source, line.Quantity));
            }

            return Result.Success<List<(InvoiceItem, decimal)>, Error>(resolved);
        }
    }
}
