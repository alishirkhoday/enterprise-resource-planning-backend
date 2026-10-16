namespace ERP.Domain.Primitives.Pagination;

/// <summary>
/// The paging and search parameters a list endpoint accepts, with the limits already applied.
/// <para>
/// The setters clamp rather than validate: a caller asking for ten thousand rows gets the default
/// page instead of an error, because a bad page size is a request that can still be served and a
/// refusal here would only teach clients to guess.
/// </para>
/// </summary>
public record SearchablePaginationQueryDto : IPaginationQuery
{
    /// <summary>The page size used when a request does not ask for one, or asks for a silly one.</summary>
    public const int DefaultPageSize = 10;

    /// <summary>The ceiling on a page, so one request cannot pull a whole table.</summary>
    public const int MaximumPageSize = 50;

    /// <summary>Pages are counted from one, the way a person counts them.</summary>
    public const int FirstPageNumber = 1;

    /// <summary>Free text to filter on. What it searches is up to each repository.</summary>
    public string? SearchValue { get; set; }

    // The fields start at the defaults rather than at zero. A request that simply omits the
    // paging parameters never calls the setter, and an unset backing field would otherwise
    // return a page size of nothing.
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// How many rows to return. Anything outside one to <see cref="MaximumPageSize"/> is replaced
    /// by <see cref="DefaultPageSize"/>.
    /// </summary>
    public int PageSize
    {
        get
        {
            return _pageSize;
        }
        set
        {
            _pageSize = value < 1 || value > MaximumPageSize ? DefaultPageSize : value;
        }
    }

    private int _pageNumber = FirstPageNumber;

    /// <summary>Which page to return. Anything below one is treated as the first page.</summary>
    public int PageNumber
    {
        get
        {
            return _pageNumber;
        }
        set
        {
            _pageNumber = value < FirstPageNumber ? FirstPageNumber : value;
        }
    }
}
