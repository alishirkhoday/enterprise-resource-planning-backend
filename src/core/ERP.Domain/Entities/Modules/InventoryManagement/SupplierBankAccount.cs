namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a supplier is paid - شماره شبا.
/// </summary>
public sealed class SupplierBankAccount : BaseEntity
{
    public Supplier Supplier { get; private set; } = default!;
    public Guid SupplierId { get; private set; }

    public string BankName { get; private set; } = default!;
    public string AccountNumber { get; private set; } = default!;
    public string? InternationalBankAccountNumber { get; private set; }
    public string? AccountHolderName { get; private set; }
    public string CurrencyCode { get; private set; } = default!;
    public bool IsDefault { get; private set; }

    private SupplierBankAccount() { }

    public SupplierBankAccount(Supplier supplier, string bankName, string accountNumber, string currencyCode = "IRR")
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName, nameof(bankName));
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber, nameof(accountNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Supplier = supplier;
        SupplierId = supplier.Id;
        BankName = bankName.Trim();
        AccountNumber = accountNumber.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
    }

    internal void SetInternationalBankAccountNumber(string? iban) =>
        InternationalBankAccountNumber = string.IsNullOrWhiteSpace(iban) ? null : iban.Trim().ToUpperInvariant();

    internal void SetAccountHolderName(string? holderName) =>
        AccountHolderName = string.IsNullOrWhiteSpace(holderName) ? null : holderName.Trim();

    internal void SetDefault() => IsDefault = true;
    internal void UnsetDefault() => IsDefault = false;
}
