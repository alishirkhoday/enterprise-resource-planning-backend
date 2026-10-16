using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Queries.GetCustomerStatement
{
    /// <summary>
    /// What a customer was invoiced, what is still owed, and how old it is.
    /// <para>
    /// The ageing is what the statement is for. A single outstanding total tells somebody to make
    /// a phone call; a total split by how late each part is tells them which call to make first.
    /// </para>
    /// </summary>
    public class GetCustomerStatementQueryHandler(
        ICustomerRepository customers,
        IInvoiceRepository invoices,
        IReceivableRepository receivables)
        : IRequestHandler<GetCustomerStatementQuery, Result<CustomerStatementDto, Error>>
    {
        /// <summary>
        /// The upper edge of each bucket in days. The last bucket is everything beyond, which is
        /// the one that usually needs a decision rather than a reminder.
        /// </summary>
        private static readonly (string Label, int UpperBound)[] Buckets =
        [
            ("Not yet due", 0),
            ("1-30 days", 30),
            ("31-60 days", 60),
            ("61-90 days", 90),
            ("Over 90 days", int.MaxValue)
        ];

        public async Task<Result<CustomerStatementDto, Error>> Handle(GetCustomerStatementQuery request, CancellationToken cancellationToken)
        {
            var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer is null)
                return Result.Failure<CustomerStatementDto, Error>(Errors.General.NotFound(request.CustomerId.ToString(), nameof(Customer)));

            var asOf = request.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var issued = await invoices.GetByCustomerAsync(customer.Id, request.From, request.To, cancellationToken);

            // Cancelled invoices were never owed, and drafts were never issued. Neither belongs on
            // a statement a customer is expected to reconcile against their own records.
            var reportable = issued
                .Where(i => i.Status is not (InvoiceStatus.Draft or InvoiceStatus.Cancelled))
                .OrderBy(i => i.IssueDate)
                .ToArray();

            var currencyCode = reportable.Length > 0
                ? reportable[0].CurrencyCode
                : customer.CreditLimit?.CurrencyCode ?? "IRR";

            // A credit note reduces what is owed, so its value is subtracted rather than added.
            var totalInvoiced = reportable.Sum(i => Signed(i));

            var outstanding = customer.DetailAccountId is not null
                ? await receivables.GetOutstandingTotalAsync(customer.DetailAccountId.Value, cancellationToken)
                : 0m;

            var ageing = customer.DetailAccountId is not null
                ? await BuildAgeingAsync(customer.DetailAccountId.Value, asOf, cancellationToken)
                : [];

            var creditAvailable = customer.CreditLimit is not null
                ? customer.CreditLimit.Amount - outstanding
                : (decimal?)null;

            return Result.Success<CustomerStatementDto, Error>(new CustomerStatementDto(
                customer.Id,
                customer.DisplayName,
                request.From ?? reportable.FirstOrDefault()?.IssueDate ?? asOf,
                request.To ?? asOf,
                currencyCode,
                totalInvoiced,
                outstanding,
                customer.CreditLimit?.Amount,
                creditAvailable,
                creditAvailable < 0m,
                [.. reportable.Select(i => new CustomerStatementLineDto(
                    i.Id,
                    i.Number,
                    i.Type,
                    i.Status,
                    i.IssueDate,
                    i.DueDate,
                    Signed(i),
                    i.DueDate < asOf ? asOf.DayNumber - i.DueDate.DayNumber : 0))],
                ageing));
        }

        private static decimal Signed(Invoice invoice) =>
            invoice.Type == InvoiceType.CreditNote ? -invoice.AmountPayable.Amount : invoice.AmountPayable.Amount;

        private async Task<IReadOnlyList<CustomerAgeingBucketDto>> BuildAgeingAsync(
            Guid detailAccountId,
            DateOnly asOf,
            CancellationToken cancellationToken)
        {
            var rows = await receivables.GetAgeingRowsAsync(detailAccountId, cancellationToken);

            var totals = new decimal[Buckets.Length];

            foreach (var row in rows)
            {
                var daysOverdue = row.DaysOverdueOn(asOf);
                var index = Array.FindIndex(Buckets, b => daysOverdue <= b.UpperBound);
                totals[index] += row.OutstandingAmount;
            }

            // Every bucket is returned, including the empty ones, so a report reads the same shape
            // every time and a zero says "nothing here" rather than "not asked".
            return [.. Buckets.Select((bucket, index) => new CustomerAgeingBucketDto(bucket.Label, totals[index]))];
        }
    }
}
