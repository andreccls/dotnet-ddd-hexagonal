using System.Net;
using System.Net.Http.Json;
using DddHexagonal.Api.Tests.Support;
using DddHexagonal.Application.Common;
using DddHexagonal.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace DddHexagonal.Api.Tests;

[Collection(ApiFixtureDefinition.Name)]
public sealed class CustomerEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task FullCrudLifecycle()
    {
        var created = await Seed.Customer(_client);
        Assert.True(created.Active);
        Assert.Equal("52998224725", created.Document);

        var fetched = await (await _client.GetAsync($"/customers/{created.Id}")).Read<CustomerResponse>(HttpStatusCode.OK);
        Assert.Equal(created, fetched);

        var update = new UpdateCustomerCommand("Maria S.", created.Email, "111.444.777-35", null);
        var updated = await (await _client.PutAsJsonAsync($"/customers/{created.Id}", update)).Read<CustomerResponse>(HttpStatusCode.OK);
        Assert.Equal("Maria S.", updated.Name);
        Assert.Null(updated.Phone);

        var deactivated = await (await _client.PostAsync($"/customers/{created.Id}/deactivate", null)).Read<CustomerResponse>(HttpStatusCode.OK);
        Assert.False(deactivated.Active);
        var activated = await (await _client.PostAsync($"/customers/{created.Id}/activate", null)).Read<CustomerResponse>(HttpStatusCode.OK);
        Assert.True(activated.Active);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/customers/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/customers/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task List_IsPaginated()
    {
        await Seed.Customer(_client);
        await Seed.Customer(_client);

        var page = await (await _client.GetAsync("/customers?page=1&pageSize=1")).Read<PagedResult<CustomerResponse>>(HttpStatusCode.OK);

        Assert.Single(page.Items);
        Assert.True(page.TotalCount >= 2);
        Assert.Equal(1, page.PageSize);
        var defaults = await (await _client.GetAsync("/customers")).Read<PagedResult<CustomerResponse>>(HttpStatusCode.OK);
        Assert.Equal(20, defaults.PageSize);
    }

    [Fact]
    public async Task DuplicatedEmail_Returns409ProblemDetails()
    {
        var first = await Seed.Customer(_client);
        var response = await _client.PostAsJsonAsync("/customers", new CreateCustomerCommand("Other", first.Email, "11144477735", null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
    }

    [Fact]
    public async Task InvalidCpf_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/customers", new CreateCustomerCommand("X", "x@example.com", "111.111.111-11", null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CPF", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownCustomer_Returns404_OnEveryRoute()
    {
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/customers/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync($"/customers/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/customers/{id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.PutAsJsonAsync($"/customers/{id}", new UpdateCustomerCommand("X", "x@example.com", "52998224725", null))).StatusCode);
    }
}
