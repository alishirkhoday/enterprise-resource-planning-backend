using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Users;

namespace ERP.Infrastructure.MainDatabase.Repositories.Users
{
    public class UserRepository(IMainDbContext context) : Repository<User>(context), IUserRepository
    {
        public async Task<bool> CheckUserExistsByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
        {
            var result = await _entity.AnyAsync(u => u.NormalizedUsername == normalizedUsername, cancellationToken);
            return result;
        }

        public async Task<bool> CheckUserExistsByMobilePhoneNumberAsync(string normalizedMobilePhoneNumber, CancellationToken cancellationToken = default)
        {
            var result = await _entity.AnyAsync(u => u.NormalizedMobilePhoneNumber == normalizedMobilePhoneNumber, cancellationToken);
            return result;
        }

        public async Task<bool> CheckUserExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            var result = await _entity.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
            return result;
        }

        public async Task<User?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
        {
            var result = await _entity.FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername, cancellationToken);
            return result;
        }

        public async Task<User?> GetByMobilePhoneNumberAsync(string normalizedMobilePhoneNumber, CancellationToken cancellationToken = default)
        {
            var result = await _entity.FirstOrDefaultAsync(u => u.NormalizedMobilePhoneNumber == normalizedMobilePhoneNumber, cancellationToken);
            return result;
        }

        public async Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            var result = await _entity.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
            return result;
        }
    }
}
