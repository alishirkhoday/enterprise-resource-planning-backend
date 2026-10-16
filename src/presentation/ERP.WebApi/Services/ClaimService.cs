using System.Security.Claims;

namespace ERP.WebApi.Services
{
    public static class ClaimService
    {
        public static string GetUserID(this ClaimsPrincipal claimsPrincipal)
        {
            var claim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == "uid");
            if (claim == null)
            {
                return "";
            }
            var claimValue = claim.Value;
            return claimValue;
        }
    }
}
