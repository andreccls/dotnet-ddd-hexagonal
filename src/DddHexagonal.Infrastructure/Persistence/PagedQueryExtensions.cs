using DddHexagonal.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace DddHexagonal.Infrastructure.Persistence;

internal static class PagedQueryExtensions
{
    /// <summary>The query must already be ordered, otherwise pages are not stable.</summary>
    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        this IOrderedQueryable<T> query,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page.Page, page.PageSize, total);
    }
}
