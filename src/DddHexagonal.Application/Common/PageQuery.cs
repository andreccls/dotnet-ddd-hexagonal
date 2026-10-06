namespace DddHexagonal.Application.Common;

/// <summary>Pagination input. Out-of-range values are clamped instead of rejected (friendly defaults).</summary>
public sealed record PageQuery
{
    public const int MaxPageSize = 100;

    public PageQuery(int page = 1, int pageSize = 20)
    {
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, MaxPageSize);
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;
}
