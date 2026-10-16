using ERP.Domain.Entities.Users;

namespace ERP.Application.Common.Interfaces.Authentication
{
    public interface IOneTimePasswordService
    {
        Task<Tuple<string?, bool>> GenerateAsync(string normalizeMobilePhoneNumberOrEmail, OneTimePasswordChannel channel, CancellationToken cancellationToken = default);
        Task<OneTimePassword?> GetByMobilePhoneNumberOrEmailAsync(string normalizeMobilePhoneNumberOrEmail, CancellationToken cancellationToken = default);
        Task<bool> VerifyAsync(string normalizeMobilePhoneNumberOrEmail, string inputCode, CancellationToken cancellationToken = default);
        Task<int?> GetUserOTPCountAsync(string normalizeMobilePhoneNumberOrEmail, CancellationToken cancellationToken = default);
    }
}
