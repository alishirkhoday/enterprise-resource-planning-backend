using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Queries.GetCustomerStatement
{
    /// <summary>
    /// A customer statement - صورت‌حساب مشتری. The window is optional: left open it reports
    /// everything, which is what somebody means when they ask what a customer owes.
    /// </summary>
    public sealed record GetCustomerStatementQuery : IQuery<Result<CustomerStatementDto, Error>>
    {
        public Guid CustomerId { get; init; }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }

        /// <summary>
        /// The date the ageing is measured from. Defaults to today; set it to reproduce the
        /// statement somebody was looking at last month.
        /// </summary>
        public DateOnly? AsOf { get; init; }
    }
}
