using ERP.Domain.Entities.Users;

namespace ERP.Domain.Repositories.Users;
/// <summary>
/// User accounts - identity, lockout and credentials.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);
    Task<User?> GetByMobilePhoneNumberAsync(string normalizedMobilePhoneNumber, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> CheckUserExistsByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);
    Task<bool> CheckUserExistsByMobilePhoneNumberAsync(string normalizedMobilePhoneNumber, CancellationToken cancellationToken = default);
    Task<bool> CheckUserExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
