namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Where somebody's pay is sent - شماره شبا.
/// </summary>
public sealed class HumanBankAccount : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }
    public string BankName { get; private set; } = default!;
    public string AccountNumber { get; private set; } = default!;
    public string CardNumber { get; private set; } = default!;
    public string InternationalBankAccountNumber { get; private set; } = default!;
    public bool IsDefault { get; private set; }

    private HumanBankAccount()
    {
    }

    public HumanBankAccount(Human human, string bankName, string accountNumber, string cardNumber, string internationalBankAccountNumber)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName, nameof(bankName));
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber, nameof(accountNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(cardNumber, nameof(cardNumber));
        Human = human;
        HumanId = human.Id;
        BankName = bankName.Trim();
        AccountNumber = accountNumber.Trim();
        CardNumber = cardNumber.Trim();
        InternationalBankAccountNumber = internationalBankAccountNumber.Trim();
    }

    internal void Update(string bankName, string accountNumber, string cardNumber, string internationalBankAccountNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName, nameof(bankName));
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber, nameof(accountNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(cardNumber, nameof(cardNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(internationalBankAccountNumber, nameof(internationalBankAccountNumber));
        BankName = bankName.Trim();
        AccountNumber = accountNumber.Trim();
        CardNumber = cardNumber.Trim();
        InternationalBankAccountNumber = internationalBankAccountNumber.Trim();
    }

    internal void SetIsDefault() => IsDefault = true;
}
