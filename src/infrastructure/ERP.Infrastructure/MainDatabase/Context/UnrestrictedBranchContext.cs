using ERP.Application.Common.Interfaces.Branches;

namespace ERP.Infrastructure.MainDatabase.Context;

/// <summary>
/// The branch context used when there is no user: migrations, design-time tooling and background
/// jobs that legitimately work across the whole organization.
/// <para>
/// It is deliberately a named type rather than a null check buried in the context. "No caller" and
/// "a caller allowed to see everything" are the same thing to the filter, but only one of them is
/// safe to assume by accident, so the assumption is written down where it can be reviewed.
/// </para>
/// </summary>
public sealed class UnrestrictedBranchContext : IBranchContext
{
    public static readonly UnrestrictedBranchContext Instance = new();

    public Guid? CurrentBranchId => null;

    public IReadOnlyCollection<Guid> AccessibleBranchIds => [];

    public bool CanAccessAllBranches => true;
}
