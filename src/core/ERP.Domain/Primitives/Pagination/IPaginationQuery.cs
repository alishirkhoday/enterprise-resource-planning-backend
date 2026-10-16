namespace ERP.Domain.Primitives.Pagination;

/// <summary>
/// What a request has to say for a repository to return one page instead of a whole table.
/// </summary>
public interface IPaginationQuery
{
    /// <summary>How many rows the caller can actually display.</summary>
    public int PageSize { get; set; }

    /// <summary>Which page is wanted, counted from one rather than from zero.</summary>
    public int PageNumber { get; set; }
}
