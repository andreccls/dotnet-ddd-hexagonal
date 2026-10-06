using DddHexagonal.Application.Common;
using DddHexagonal.Application.Customers;
using DddHexagonal.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace DddHexagonal.Api.Endpoints;

/// <summary>Driving adapter: translates HTTP into use case calls. No business logic here.</summary>
internal static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers").WithTags("Customers");

        group.MapPost("/", async (CreateCustomerCommand command, IUseCase<CreateCustomerCommand, CustomerResponse> useCase, CancellationToken ct) =>
        {
            var created = await useCase.ExecuteAsync(command, ct);
            return Results.Created($"/customers/{created.Id}", created);
        })
        .Produces<CustomerResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async ([FromQuery] int? page, [FromQuery] int? pageSize, IUseCase<PageQuery, PagedResult<CustomerResponse>> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new PageQuery(page ?? 1, pageSize ?? 20), ct)))
        .Produces<PagedResult<CustomerResponse>>();

        group.MapGet("/{id:guid}", async (Guid id, IUseCase<GetCustomerQuery, CustomerResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new GetCustomerQuery(id), ct)))
        .Produces<CustomerResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCustomerCommand command, IUseCase<UpdateCustomerCommand, CustomerResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(command with { Id = id }, ct)))
        .Produces<CustomerResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/activate", async (Guid id, IUseCase<SetCustomerStatusCommand, CustomerResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new SetCustomerStatusCommand(id, true), ct)))
        .Produces<CustomerResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IUseCase<SetCustomerStatusCommand, CustomerResponse> useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(new SetCustomerStatusCommand(id, false), ct)))
        .Produces<CustomerResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, IUseCase<DeleteCustomerCommand, Unit> useCase, CancellationToken ct) =>
        {
            await useCase.ExecuteAsync(new DeleteCustomerCommand(id), ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
