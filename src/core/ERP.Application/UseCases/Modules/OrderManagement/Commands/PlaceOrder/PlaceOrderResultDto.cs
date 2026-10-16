namespace ERP.Application.UseCases.Modules.OrderManagement.Commands.PlaceOrder
{
    /// <summary>
    /// What the caller needs next: the code to quote, what they owe, and when the order stops
    /// waiting for it.
    /// </summary>
    public sealed record PlaceOrderResultDto(
        Guid OrderId,
        string Code,
        decimal AmountPayable,
        string CurrencyCode,
        DateTimeOffset PayBefore);
}
