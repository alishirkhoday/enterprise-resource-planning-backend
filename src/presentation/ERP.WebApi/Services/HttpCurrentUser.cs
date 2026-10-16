using System.Security.Claims;
using ERP.Application.Common.Interfaces.Branches;
using ERP.Application.Common.Interfaces.Identity;

namespace ERP.WebApi.Services;

/// <summary>
/// The caller of the current request, read from their token.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor, IConfiguration configuration)
    : ICurrentUser, IBranchContext
{
    /// <summary>The claim the sign-in token already issues for the user identifier.</summary>
    private const string UserIdClaim = "uid";

    /// <summary>One claim per branch the caller may read.</summary>
    private const string BranchClaim = "branch";

    /// <summary>Present on a head office token.</summary>
    private const string AllBranchesClaim = "all_branches";

    /// <summary>
    /// Branch scoping stays off until the token issuer actually puts branch claims in the token.
    /// <para>
    /// Turning it on before then would silently hide every branch scoped row from everybody, since
    /// a token with no branch claims grants no branches. Defaulting the other way - treating a
    /// token with no claims as head office - would be worse: the restriction would look enabled
    /// and enforce nothing. So it is one setting, off, that somebody turns on deliberately once
    /// the claims exist.
    /// </para>
    /// </summary>
    private bool IsEnforced => configuration.GetValue("BranchScoping:Enforced", false);

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    /// <summary>
    /// No request at all, which means in-process work: a background job, a scheduled scan. That is
    /// not an anonymous caller to be restricted, it is trusted code with no person behind it, and
    /// the distinction matters - treating it as a caller with no branches would leave the outbox
    /// processor and the nightly scan unable to see the rows they exist to work on.
    /// </summary>
    private bool HasNoRequest => accessor.HttpContext is null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst(UserIdClaim)?.Value, out var id) ? id : null;

    public string? UserName =>
        HasNoRequest
            ? "system"
            : IsAuthenticated
                ? Principal?.Identity?.Name
                  ?? Principal?.FindFirst(ClaimTypes.Name)?.Value
                  ?? Principal?.FindFirst(UserIdClaim)?.Value
                : null;

    public Guid? CurrentBranchId => AccessibleBranchIds.FirstOrDefault() is var first && first != Guid.Empty ? first : null;

    public IReadOnlyCollection<Guid> AccessibleBranchIds
    {
        get
        {
            if (!IsEnforced || HasNoRequest)
                return [];

            var principal = Principal;
            if (principal is null)
                return [];

            return [.. principal.FindAll(BranchClaim)
                .Select(claim => Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()];
        }
    }

    public bool CanAccessAllBranches
    {
        get
        {
            if (!IsEnforced || HasNoRequest)
                return true;

            return Principal?.FindFirst(AllBranchesClaim)?.Value is "true";
        }
    }
}
