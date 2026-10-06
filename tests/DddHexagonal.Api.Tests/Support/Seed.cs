using System.Net.Http.Json;
using DddHexagonal.Application.Customers;
using DddHexagonal.Application.Orders;
using DddHexagonal.Application.Products;

namespace DddHexagonal.Api.Tests.Support;

/// <summary>Helpers that create data through the public HTTP API (no database shortcuts).</summary>
internal static class Seed
{
    public static async Task<T> Read<T>(this HttpResponseMessage response, System.Net.HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task<CustomerResponse> Customer(HttpClient client, string? email = null)
    {
        var response = await client.PostAsJsonAsync("/customers", new CreateCustomerCommand(
            "Maria Silva", email ?? $"{Guid.NewGuid():N}@example.com", "529.982.247-25", "(31) 99999-8888"));
        return await response.Read<CustomerResponse>(System.Net.HttpStatusCode.Created);
    }

    public static async Task<ProductResponse> Product(HttpClient client, decimal price = 10m, int stock = 10, string? sku = null)
    {
        var response = await client.PostAsJsonAsync("/products", new CreateProductCommand(
            "Keyboard", sku ?? $"K-{Guid.NewGuid():N}"[..16], price, stock));
        return await response.Read<ProductResponse>(System.Net.HttpStatusCode.Created);
    }

    public static async Task<OrderResponse> Order(HttpClient client, Guid customerId, params (Guid Product, int Qty)[] items)
    {
        var response = await client.PostAsJsonAsync("/orders", new CreateOrderCommand(
            customerId, [.. items.Select(i => new OrderItemRequest(i.Product, i.Qty))]));
        return await response.Read<OrderResponse>(System.Net.HttpStatusCode.Created);
    }
}
