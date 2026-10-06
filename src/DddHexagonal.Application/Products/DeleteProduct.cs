using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Products;

public sealed record DeleteProductCommand(Guid Id);

public sealed class DeleteProductUseCase(IProductRepository products, IOrderRepository orders, IUnitOfWork unitOfWork)
    : IUseCase<DeleteProductCommand, Unit>
{
    public async Task<Unit> ExecuteAsync(DeleteProductCommand request, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Product", request.Id);

        if (await orders.ExistsForProductAsync(product.Id, cancellationToken))
        {
            throw new ConflictException("Product is referenced by orders and cannot be deleted.");
        }

        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
