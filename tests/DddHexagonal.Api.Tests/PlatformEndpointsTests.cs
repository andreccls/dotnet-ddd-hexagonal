using System.Net;
using System.Text;
using DddHexagonal.Api.Tests.Support;

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
