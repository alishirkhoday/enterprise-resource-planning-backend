using ERP.Application.Common.Interfaces.Authentication;
using ERP.Domain.Entities.Users;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ERP.WebApi.Services
{
    public class TokenService(IConfiguration configuration) : ITokenService
    {
        private readonly IConfiguration _configuration = configuration;

        public string GenerateJsonWebToken(User user, DateTimeOffset expireDate)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            var jwtKey = _configuration["JWT:Key"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? throw new ArgumentNullException(jwtKey, "JWT Key is null.")));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claimsIdentity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
            claimsIdentity.AddClaim(new Claim("uid", user.Id.ToString()));
            claimsIdentity.AddClaim(new Claim("ss", user.SecurityStamp));
            var principal = new ClaimsPrincipal(claimsIdentity);
            var claims = principal.Claims;
            var tokenGen = new JwtSecurityToken(
                _configuration["Jwt:Issuer"] ?? null,
                _configuration["Jwt:Audience"] ?? null,
                claims,
                null,
                expireDate.DateTime,
                credentials);
            var newToken = new JwtSecurityTokenHandler().WriteToken(tokenGen);
            return newToken;
        }
    }
}
