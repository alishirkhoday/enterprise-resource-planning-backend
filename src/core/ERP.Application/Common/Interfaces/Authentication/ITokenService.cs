using ERP.Domain.Entities.Users;

namespace ERP.Application.Common.Interfaces.Authentication
{
    public interface ITokenService
    {
        string GenerateJsonWebToken(User user, DateTimeOffset expireDate);
    }
}
