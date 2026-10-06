using System.Text.Json.Serialization;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Products;

/// <summary>Positive delta adds units, negative removes. The aggregate refuses negative stock.</summary>
public sealed record AdjustProductStockCommand(int Delta)
{
    [JsonIgnore]
    public Guid Id { get; init; }
}

public sealed class AdjustProductStockUseCase(IProductRepository products, IUnitOfWork unitOfWork)
    : IUseCase<AdjustProductStockCommand, ProductResponse>
{
    public async Task<ProductResponse> ExecuteAsync(AdjustProductStockCommand request, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Product", request.Id);

        product.AdjustStock(request.Delta);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductResponse.From(product);
    }
}
