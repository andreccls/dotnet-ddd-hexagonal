using DddHexagonal.Application.Common;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Application.Ports.Out;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<PagedResult<Product>> ListAsync(PageQuery page, CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(Sku sku, Guid? exceptProductId, CancellationToken cancellationToken = default);

    void Add(Product product);

    void Remove(Product product);
}
