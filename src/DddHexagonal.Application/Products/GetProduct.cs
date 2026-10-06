using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Products;

public sealed record GetProductQuery(Guid Id);

public sealed class GetProductUseCase(IProductRepository products) : IUseCase<GetProductQuery, ProductResponse>
{
    public async Task<ProductResponse> ExecuteAsync(GetProductQuery request, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Product", request.Id);
        return ProductResponse.From(product);
    }
}
