using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace DddHexagonal.Api.Endpoints;

internal static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Products");

        group.MapPost("/", async (CreateProductCommand command, IUseCase<CreateProductCommand, ProductResponse> useCase, CancellationToken ct) =>
        {
            var created = await useCase.ExecuteAsync(command, ct);
            return Results.Created($"/products/{created.Id}", created);
        })
        .Produces<ProductResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async ([FromQuery] int? page, [FromQuery] int? pageSize, IUseCase<PageQuery, PagedResult<ProductResponse>> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new PageQuery(page ?? 1, pageSize ?? 20), ct)))
        .Produces<PagedResult<ProductResponse>>();

        group.MapGet("/{id:guid}", async (Guid id, IUseCase<GetProductQuery, ProductResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new GetProductQuery(id), ct)))
        .Produces<ProductResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductCommand command, IUseCase<UpdateProductCommand, ProductResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(command with { Id = id }, ct)))
        .Produces<ProductResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/stock", async (Guid id, AdjustProductStockCommand command, IUseCase<AdjustProductStockCommand, ProductResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(command with { Id = id }, ct)))
        .Produces<ProductResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (Guid id, IUseCase<DeleteProductCommand, Unit> useCase, CancellationToken ct) =>
        {
            await useCase.ExecuteAsync(new DeleteProductCommand(id), ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
