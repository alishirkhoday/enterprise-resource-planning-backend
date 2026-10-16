namespace ERP.Domain.Primitives;

/// <summary>
/// Implemented by records that belong to one branch of the organization, so infrastructure can
/// filter them by the branches the current user may see.
/// <para>
/// The identifier is nullable on purpose: a null branch means the record belongs to the
/// organization as a whole - a company-wide price list, a shared numbering series - and stays
/// visible from every branch.
/// </para>
/// <para>
/// The rows that <em>define</em> branch access are deliberately not scoped this way. Filtering a
/// user's branch assignments by branch would be circular: the filter needs them to know which
/// branches the user may see.
/// </para>
/// </summary>
public interface IBranchScoped
{
    Guid? BranchId { get; }

    void AssignToBranch(Guid branchId);
}
