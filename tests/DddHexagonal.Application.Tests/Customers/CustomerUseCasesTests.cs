using DddHexagonal.Application.Common;
using DddHexagonal.Application.Customers;
using DddHexagonal.Application.Tests.Fakes;
using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Application.Tests.Customers;

public sealed class CustomerUseCasesTests
{
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryOrderRepository _orders = new();
    private readonly FakeUnitOfWork _uow = new();

    private static CreateCustomerCommand Valid(string email = "maria@example.com", string? phone = null) =>
        new("Maria", email, "529.982.247-25", phone);

    private async Task<CustomerResponse> Seed(string email = "maria@example.com") =>
        await new CreateCustomerUseCase(_customers, _uow).ExecuteAsync(Valid(email));

    [Fact]
    public async Task Create_PersistsAndReturnsResponse()
    {
        var response = await new CreateCustomerUseCase(_customers, _uow).ExecuteAsync(Valid(phone: "(31) 99999-8888"));

        Assert.Equal("maria@example.com", response.Email);
        Assert.Equal("52998224725", response.Document);
        Assert.Equal("31999998888", response.Phone);
        Assert.True(response.Active);
        Assert.Single(_customers.Items);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Create_WithBlankPhone_HasNoPhone()
    {
        var response = await new CreateCustomerUseCase(_customers, _uow).ExecuteAsync(Valid(phone: "  "));
        Assert.Null(response.Phone);
    }

    [Fact]
    public async Task Create_WithDuplicatedEmail_ThrowsConflict()
    {
        await Seed();
        await Assert.ThrowsAsync<ConflictException>(() => Seed());
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Create_WithInvalidData_ThrowsDomainException() =>
        await Assert.ThrowsAsync<DomainException>(() =>
            new CreateCustomerUseCase(_customers, _uow).ExecuteAsync(Valid() with { Document = "123" }));

    [Fact]
    public async Task Get_ReturnsCustomer()
    {
        var created = await Seed();
        var response = await new GetCustomerUseCase(_customers).ExecuteAsync(new GetCustomerQuery(created.Id));
        Assert.Equal(created, response);
    }

    [Fact]
    public async Task Get_Unknown_ThrowsNotFound() =>
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetCustomerUseCase(_customers).ExecuteAsync(new GetCustomerQuery(Guid.NewGuid())));

    [Fact]
    public async Task List_IsPaginated()
    {
        await Seed("a@example.com");
        await Seed("b@example.com");
        await Seed("c@example.com");

        var page = await new ListCustomersUseCase(_customers).ExecuteAsync(new PageQuery(2, 2));

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Single(page.Items);
        Assert.Equal("c@example.com", page.Items[0].Email);
    }

    [Fact]
    public async Task Update_ChangesData_AndAllowsKeepingOwnEmail()
    {
        var created = await Seed();
        var cmd = new UpdateCustomerCommand("Maria S.", "maria@example.com", "111.444.777-35", null) { Id = created.Id };

        var response = await new UpdateCustomerUseCase(_customers, _uow).ExecuteAsync(cmd);

        Assert.Equal("Maria S.", response.Name);
        Assert.Equal("11144477735", response.Document);
    }

    [Fact]
    public async Task Update_WithEmailOfAnotherCustomer_ThrowsConflict()
    {
        await Seed("a@example.com");
        var other = await Seed("b@example.com");
        var cmd = new UpdateCustomerCommand("X", "a@example.com", "52998224725", null) { Id = other.Id };

        await Assert.ThrowsAsync<ConflictException>(() => new UpdateCustomerUseCase(_customers, _uow).ExecuteAsync(cmd));
    }

    [Fact]
    public async Task Update_Unknown_ThrowsNotFound() =>
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateCustomerUseCase(_customers, _uow).ExecuteAsync(
                new UpdateCustomerCommand("X", "a@example.com", "52998224725", null) { Id = Guid.NewGuid() }));

    [Fact]
    public async Task SetStatus_DeactivatesAndActivates()
    {
        var created = await Seed();
        var useCase = new SetCustomerStatusUseCase(_customers, _uow);

        Assert.False((await useCase.ExecuteAsync(new SetCustomerStatusCommand(created.Id, false))).Active);
        Assert.True((await useCase.ExecuteAsync(new SetCustomerStatusCommand(created.Id, true))).Active);
    }

    [Fact]
    public async Task SetStatus_RepeatedOrUnknown_Throws()
    {
        var created = await Seed();
        var useCase = new SetCustomerStatusUseCase(_customers, _uow);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new SetCustomerStatusCommand(created.Id, true)));
        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new SetCustomerStatusCommand(Guid.NewGuid(), true)));
    }

    [Fact]
    public async Task Delete_RemovesCustomer()
    {
        var created = await Seed();
        await new DeleteCustomerUseCase(_customers, _orders, _uow).ExecuteAsync(new DeleteCustomerCommand(created.Id));
        Assert.Empty(_customers.Items);
    }

    [Fact]
    public async Task Delete_Unknown_ThrowsNotFound() =>
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteCustomerUseCase(_customers, _orders, _uow).ExecuteAsync(new DeleteCustomerCommand(Guid.NewGuid())));

    [Fact]
    public async Task Delete_WithOrders_ThrowsConflict()
    {
        var created = await Seed();
        _orders.Add(Domain.Orders.Order.Create(
            created.Id,
            [new Domain.Orders.OrderLine(Guid.NewGuid(), Domain.Products.Money.Zero, 1)],
            DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<ConflictException>(() =>
            new DeleteCustomerUseCase(_customers, _orders, _uow).ExecuteAsync(new DeleteCustomerCommand(created.Id)));
        Assert.Single(_customers.Items);
    }
}
