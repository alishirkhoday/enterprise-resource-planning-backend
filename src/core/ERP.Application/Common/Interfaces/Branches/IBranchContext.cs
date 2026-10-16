namespace ERP.Application.Common.Interfaces.Branches;

/// <summary>
/// Which branches the caller is allowed to see. Read by the global query filter, so every query
/// in the system is scoped by it without any call site having to remember.
/// <para>
/// Access is a set rather than a single current branch, and that is deliberate. A regional manager
/// covers several branches and head office covers all of them; expressing that as "one branch, or
/// else bypass the filter" would mean calling <c>IgnoreQueryFilters()</c> for a cross-branch view,
/// which switches off the soft-delete filter at the same time and brings deleted rows back.
/// </para>
/// </summary>
public interface IBranchContext
{
    /// <summary>The branch the caller is working in, used to stamp new records.</summary>
    Guid? CurrentBranchId { get; }

    /// <summary>Every branch the caller may read. Empty means only organization-wide records.</summary>
    IReadOnlyCollection<Guid> AccessibleBranchIds { get; }

    /// <summary>
    /// True for head office and for background processes that legitimately work across the whole
    /// organization. It short-circuits the filter rather than bypassing it.
    /// </summary>
    bool CanAccessAllBranches { get; }
}
