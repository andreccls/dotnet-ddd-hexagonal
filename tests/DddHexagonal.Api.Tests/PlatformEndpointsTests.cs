using System.Net;
using System.Text;
using DddHexagonal.Api.Tests.Support;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace DddHexagonal.Api.Tests;

[Collection(ApiFixtureDefinition.Name)]
public sealed class PlatformEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReportsHealthy_WhenDatabaseIsReachable()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OpenApiDocument_ListsTheThreeResources()
    {
        var json = await _client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/customers", json, StringComparison.Ordinal);
        Assert.Contains("/products", json, StringComparison.Ordinal);
        Assert.Contains("/orders", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SwaggerUi_IsServed() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/swagger/index.html")).StatusCode);

    [Fact]
    public async Task MalformedJson_Returns400_NotA500()
    {
        using var body = new StringContent("{ not json", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/customers", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_Returns404() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/nope")).StatusCode);

    [Fact]
    public async Task InvalidGuidInRoute_Returns404() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/customers/not-a-guid")).StatusCode);
}

[Collection(ApiFixtureDefinition.Name)]
public sealed class UnexpectedErrorTests(ApiFactory factory)
{
    private sealed class ExplodingUseCase : IUseCase<GetProductQuery, ProductResponse>
    {
        public Task<ProductResponse> ExecuteAsync(GetProductQuery request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("secret internal detail");
    }

    [Fact]
    public async Task UnexpectedException_Returns500ProblemDetails_WithoutLeakingInternals()
    {
        using var app = factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddScoped<IUseCase<GetProductQuery, ProductResponse>, ExplodingUseCase>()));

        var response = await app.CreateClient().GetAsync($"/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("secret internal detail", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
