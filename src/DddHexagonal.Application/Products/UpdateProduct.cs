using System.Text.Json.Serialization;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Application.Products;

public sealed record UpdateProductCommand(string Name, string Sku, decimal Price)
{
    /// <summary>Comes from the route, never from the body.</summary>
    [JsonIgnore]
    public Guid Id { get; init; }
}

public sealed class UpdateProductUseCase(IProductRepository products, IUnitOfWork unitOfWork)
    : IUseCase<UpdateProductCommand, ProductResponse>
{
    public async Task<ProductResponse> ExecuteAsync(UpdateProductCommand request, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Product", request.Id);

        var sku = Sku.Create(request.Sku);
        if (await products.SkuExistsAsync(sku, product.Id, cancellationToken))
        {
            throw new ConflictException($"SKU '{sku}' is already in use.");
        }

        product.Update(request.Name, sku, Money.Create(request.Price));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductResponse.From(product);
    }
}
