using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace DddHexagonal.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        await db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public Task<PagedResult<Product>> ListAsync(PageQuery page, CancellationToken cancellationToken = default) =>
        db.Products.AsNoTracking().OrderBy(p => p.Name).ThenBy(p => p.Id).ToPagedAsync(page, cancellationToken);

    public Task<bool> SkuExistsAsync(Sku sku, Guid? exceptProductId, CancellationToken cancellationToken = default) =>
        db.Products.AnyAsync(p => p.Sku == sku && p.Id != exceptProductId, cancellationToken);

    public void Add(Product product) => db.Products.Add(product);

    public void Remove(Product product) => db.Products.Remove(product);
}
