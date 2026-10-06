using System.Net;
using System.Net.Http.Json;
using DddHexagonal.Api.Tests.Support;
using DddHexagonal.Application.Common;
using DddHexagonal.Application.Orders;
using DddHexagonal.Application.Products;

namespace DddHexagonal.Api.Tests;

[Collection(ApiFixtureDefinition.Name)]
public sealed class OrderEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<int> StockOf(Guid productId) =>
        (await (await _client.GetAsync($"/products/{productId}")).Read<ProductResponse>(HttpStatusCode.OK)).Stock;

    [Fact]
    public async Task FullLifecycle_Create_Update_Confirm_Cancel_Delete()
    {
        var customer = await Seed.Customer(_client);
        var keyboard = await Seed.Product(_client, 10m, 10);
        var mouse = await Seed.Product(_client, 4.5m, 10);

        var order = await Seed.Order(_client, customer.Id, (keyboard.Id, 2));
        Assert.Equal("Pending", order.Status);
        Assert.Equal(20m, order.Total);
        Assert.Equal(8, await StockOf(keyboard.Id));

        var fetched = await (await _client.GetAsync($"/orders/{order.Id}")).Read<OrderResponse>(HttpStatusCode.OK);
        Assert.Equal(order.Id, fetched.Id);

        var updated = await (await _client.PutAsJsonAsync(
            $"/orders/{order.Id}/items",
            new UpdateOrderItemsCommand([new(keyboard.Id, 1), new(mouse.Id, 2)]))).Read<OrderResponse>(HttpStatusCode.OK);
        Assert.Equal(19m, updated.Total);
        Assert.Equal(9, await StockOf(keyboard.Id));
        Assert.Equal(8, await StockOf(mouse.Id));

        var confirmed = await (await _client.PostAsync($"/orders/{order.Id}/confirm", null)).Read<OrderResponse>(HttpStatusCode.OK);
        Assert.Equal("Confirmed", confirmed.Status);

        // Confirmed orders are frozen.
        var frozen = await _client.PutAsJsonAsync($"/orders/{order.Id}/items", new UpdateOrderItemsCommand([new(mouse.Id, 1)]));
        Assert.Equal(HttpStatusCode.BadRequest, frozen.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.DeleteAsync($"/orders/{order.Id}")).StatusCode);

        var cancelled = await (await _client.PostAsync($"/orders/{order.Id}/cancel", null)).Read<OrderResponse>(HttpStatusCode.OK);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(10, await StockOf(keyboard.Id));
        Assert.Equal(10, await StockOf(mouse.Id));

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/orders/{order.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/orders/{order.Id}")).StatusCode);
    }

    [Fact]
    public async Task List_IsPaginated_AndIncludesItems()
    {
        var customer = await Seed.Customer(_client);
        var product = await Seed.Product(_client, stock: 10);
        await Seed.Order(_client, customer.Id, (product.Id, 1));
        await Seed.Order(_client, customer.Id, (product.Id, 1));

        var page = await (await _client.GetAsync("/orders?page=1&pageSize=1")).Read<PagedResult<OrderResponse>>(HttpStatusCode.OK);

        Assert.Single(page.Items);
        Assert.Single(page.Items[0].Items);
        Assert.True(page.TotalCount >= 2);
    }

    [Fact]
    public async Task InactiveCustomer_CannotCreateOrder()
    {
        var customer = await Seed.Customer(_client);
        var product = await Seed.Product(_client);
        await _client.PostAsync($"/customers/{customer.Id}/deactivate", null);

        var response = await _client.PostAsJsonAsync("/orders", new CreateOrderCommand(customer.Id, [new(product.Id, 1)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(10, await StockOf(product.Id));
    }

    [Fact]
    public async Task InsufficientStock_AndInvalidQuantity_Return400_AndKeepStock()
    {
        var customer = await Seed.Customer(_client);
        var product = await Seed.Product(_client, stock: 2);

        var tooMany = await _client.PostAsJsonAsync("/orders", new CreateOrderCommand(customer.Id, [new(product.Id, 3)]));
        var zero = await _client.PostAsJsonAsync("/orders", new CreateOrderCommand(customer.Id, [new(product.Id, 0)]));

        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal(2, await StockOf(product.Id));
    }

    [Fact]
    public async Task UnknownCustomerOrProduct_Returns404()
    {
        var customer = await Seed.Customer(_client);
        var product = await Seed.Product(_client);

        var noCustomer = await _client.PostAsJsonAsync("/orders", new CreateOrderCommand(Guid.NewGuid(), [new(product.Id, 1)]));
        var noProduct = await _client.PostAsJsonAsync("/orders", new CreateOrderCommand(customer.Id, [new(Guid.NewGuid(), 1)]));

        Assert.Equal(HttpStatusCode.NotFound, noCustomer.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, noProduct.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync($"/orders/{Guid.NewGuid()}/confirm", null)).StatusCode);
    }

    [Fact]
    public async Task CustomerWithOrders_CannotBeDeleted()
    {
        var customer = await Seed.Customer(_client);
        var product = await Seed.Product(_client);
        await Seed.Order(_client, customer.Id, (product.Id, 1));

        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/customers/{customer.Id}")).StatusCode);
    }
}
