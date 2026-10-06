using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;

namespace DddHexagonal.Application.Products;

public sealed class ListProductsUseCase(IProductRepository products)
    : IUseCase<PageQuery, PagedResult<ProductResponse>>
{
    public async Task<PagedResult<ProductResponse>> ExecuteAsync(PageQuery request, CancellationToken cancellationToken = default) =>
        (await products.ListAsync(request, cancellationToken)).Map(ProductResponse.From);
}
