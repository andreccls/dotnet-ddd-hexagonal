using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Application.Products;

public sealed record CreateProductCommand(string Name, string Sku, decimal Price, int Stock);

public sealed class CreateProductUseCase(IProductRepository products, IUnitOfWork unitOfWork)
    : IUseCase<CreateProductCommand, ProductResponse>
{
    public async Task<ProductResponse> ExecuteAsync(CreateProductCommand request, CancellationToken cancellationToken = default)
    {
        var sku = Sku.Create(request.Sku);
        if (await products.SkuExistsAsync(sku, null, cancellationToken))
        {
            throw new ConflictException($"SKU '{sku}' is already in use.");
        }

        var product = Product.Create(request.Name, sku, Money.Create(request.Price), request.Stock);
        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductResponse.From(product);
    }
}
