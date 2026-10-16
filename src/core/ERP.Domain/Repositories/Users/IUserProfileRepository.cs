using ERP.Domain.Entities.Users;

namespace ERP.Domain.Repositories.Users;
/// <summary>
/// The parts of a user that are about the person rather than about signing in.
/// </summary>
public interface IUserProfileRepository : IRepository<UserProfile>
{
    Task<string?> GetImageByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
