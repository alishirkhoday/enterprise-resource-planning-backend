using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Users;

namespace ERP.Infrastructure.MainDatabase.Repositories.Users
{
    public class UserProfileRepository(IMainDbContext context) : Repository<UserProfile>(context), IUserProfileRepository
    {
        public async Task<string?> GetImageByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var result = await _entity.AsQueryable().AsNoTracking()
                .Where(up => up.User.Id.ToString() == userId)
                .Select(up => up.Image.FilePath)
                .FirstOrDefaultAsync(cancellationToken);
            return result;
        }
    }
}
