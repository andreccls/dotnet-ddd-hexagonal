using System.Net;
using System.Net.Http.Json;
using DddHexagonal.Api.Tests.Support;
using DddHexagonal.Application.Common;
using DddHexagonal.Application.Products;

namespace DddHexagonal.Api.Tests;

[Collection(ApiFixtureDefinition.Name)]
public sealed class ProductEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task FullCrudLifecycle()
    {
        var created = await Seed.Product(_client, 19.99m, 5);
        Assert.Equal(19.99m, created.Price);

        var fetched = await (await _client.GetAsync($"/products/{created.Id}")).Read<ProductResponse>(HttpStatusCode.OK);
        Assert.Equal(created, fetched);

        var updated = await (await _client.PutAsJsonAsync($"/products/{created.Id}", new UpdateProductCommand("Mouse", created.Sku, 5m)))
            .Read<ProductResponse>(HttpStatusCode.OK);
        Assert.Equal("Mouse", updated.Name);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/products/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/products/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task List_IsPaginated()
    {
        await Seed.Product(_client);
        await Seed.Product(_client);
        var page = await (await _client.GetAsync("/products?page=2&pageSize=1")).Read<PagedResult<ProductResponse>>(HttpStatusCode.OK);
        Assert.Single(page.Items);
        Assert.Equal(2, page.Page);
    }

    [Fact]
    public async Task DuplicatedSku_Returns409()
    {
        var first = await Seed.Product(_client);
        var response = await _client.PostAsJsonAsync("/products", new CreateProductCommand("Other", first.Sku, 1m, 1));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task NegativePrice_Returns400() =>
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync("/products", new CreateProductCommand("X", "ABC-1", -1m, 1))).StatusCode);

    [Fact]
    public async Task StockAdjustment_NeverGoesNegative()
    {
        var product = await Seed.Product(_client, stock: 5);

        var up = await (await _client.PostAsJsonAsync($"/products/{product.Id}/stock", new AdjustProductStockCommand(3)))
            .Read<ProductResponse>(HttpStatusCode.OK);
        Assert.Equal(8, up.Stock);

        var down = await _client.PostAsJsonAsync($"/products/{product.Id}/stock", new AdjustProductStockCommand(-9));
        Assert.Equal(HttpStatusCode.BadRequest, down.StatusCode);
        Assert.Equal(8, (await (await _client.GetAsync($"/products/{product.Id}")).Read<ProductResponse>(HttpStatusCode.OK)).Stock);
    }

    [Fact]
    public async Task ProductUsedByOrder_CannotBeDeleted()
    {
        var customer = await Seed.Customer(_client);
        var product = await Seed.Product(_client);
        await Seed.Order(_client, customer.Id, (product.Id, 1));

        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/products/{product.Id}")).StatusCode);
    }
}
