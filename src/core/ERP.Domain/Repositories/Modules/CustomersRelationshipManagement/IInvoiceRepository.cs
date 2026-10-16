using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
/// <summary>
/// Sales invoices and credit notes - فاکتور فروش و برگشت از فروش.
/// </summary>
public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    /// <summary>An invoice with its lines, which is the only form in which its total is real.</summary>
    Task<Invoice?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> GetByCustomerAsync(Guid customerId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> GetUnpaidAsync(Guid? customerId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Posted invoices in a period that carry tax, which is what a value added tax return is
    /// assembled from.
    /// </summary>
    Task<IReadOnlyList<Invoice>> GetForTaxReturnAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<string?> GetLastNumberAsync(InvoiceType type, CancellationToken cancellationToken = default);
}
