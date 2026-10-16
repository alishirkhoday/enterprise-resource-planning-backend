namespace ERP.Domain.Primitives.Pagination;

/// <summary>
/// One page of results together with the size of the whole set it came from.
/// <para>
/// The total is carried alongside the rows because a page on its own cannot answer the two
/// questions every list screen asks: how many pages are there, and is this the last one.
/// </para>
/// </summary>
/// <typeparam name="T">The projection being paged, not the entity: a page is for reading.</typeparam>
public class PaginatedItems<T>(int pageNumber, int pageSize, long totalCount, IEnumerable<T> items) where T : class
{
    /// <summary>Which page this is, counted from one.</summary>
    public int PageNumber { get; private set; } = pageNumber;

    /// <summary>How many rows a full page holds.</summary>
    public int PageSize { get; private set; } = pageSize;

    /// <summary>How many rows the filter matched in total, not how many are on this page.</summary>
    public long TotalCount { get; private set; } = totalCount;

    /// <summary>The rows on this page.</summary>
    public IEnumerable<T> Items { get; private set; } = items;

    /// <summary>How many pages the filter produces at this page size.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether there is a page before this one.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Whether there is a page after this one.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// A page with nothing on it. Returned when a filter matched nothing, so callers get an
    /// empty page rather than a null they have to remember to check.
    /// </summary>
    public static PaginatedItems<T> Empty(int pageNumber, int pageSize) => new(pageNumber, pageSize, 0, []);
}
