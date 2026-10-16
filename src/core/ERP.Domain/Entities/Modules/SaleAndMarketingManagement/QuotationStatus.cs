namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Where a quotation has got to. Expired is a real state and not just a date comparison:
/// a sent quotation stays sent until somebody or something closes it out.
/// </summary>
public enum QuotationStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4
}
