using DddHexagonal.Application.Common;
using DddHexagonal.Application.Orders;
using DddHexagonal.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace DddHexagonal.Api.Endpoints;

internal static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");

        group.MapPost("/", async (CreateOrderCommand command, IUseCase<CreateOrderCommand, OrderResponse> useCase, CancellationToken ct) =>
        {
            var created = await useCase.ExecuteAsync(command, ct);
            return Results.Created($"/orders/{created.Id}", created);
        })
        .Produces<OrderResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async ([FromQuery] int? page, [FromQuery] int? pageSize, IUseCase<PageQuery, PagedResult<OrderResponse>> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new PageQuery(page ?? 1, pageSize ?? 20), ct)))
        .Produces<PagedResult<OrderResponse>>();

        group.MapGet("/{id:guid}", async (Guid id, IUseCase<GetOrderQuery, OrderResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new GetOrderQuery(id), ct)))
        .Produces<OrderResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/items", async (Guid id, UpdateOrderItemsCommand command, IUseCase<UpdateOrderItemsCommand, OrderResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(command with { Id = id }, ct)))
        .Produces<OrderResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/confirm", async (Guid id, IUseCase<ConfirmOrderCommand, OrderResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new ConfirmOrderCommand(id), ct)))
        .Produces<OrderResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/cancel", async (Guid id, IUseCase<CancelOrderCommand, OrderResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new CancelOrderCommand(id), ct)))
        .Produces<OrderResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, IUseCase<DeleteOrderCommand, Unit> useCase, CancellationToken ct) =>
        {
            await useCase.ExecuteAsync(new DeleteOrderCommand(id), ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
