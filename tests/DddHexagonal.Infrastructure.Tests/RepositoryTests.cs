using DddHexagonal.Application.Common;
using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Customers;
using DddHexagonal.Domain.Orders;
using DddHexagonal.Domain.Products;
using DddHexagonal.Infrastructure.Persistence;
using DddHexagonal.Infrastructure.Persistence.Repositories;
using DddHexagonal.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace DddHexagonal.Infrastructure.Tests;

public sealed class RepositoryTests(MySqlFixture mysql) : IClassFixture<MySqlFixture>
{
    private static Customer NewCustomer(string? email = null, string name = "Maria", Phone? phone = null) =>
        Customer.Create(name, Email.Create(email ?? $"{Guid.NewGuid():N}@example.com"), Cpf.Create("529.982.247-25"), phone);

    private static Product NewProduct(int stock = 10, string? sku = null, string name = "Item") =>
        Product.Create(name, Sku.Create(sku ?? $"S-{Guid.NewGuid():N}"[..16]), Money.Create(12.34m), stock);

    private static async Task Save(AppDbContext db) => await new UnitOfWork(db).SaveChangesAsync();

    [Fact]
    public async Task Customer_RoundTripsValueObjects_AndAnswersEmailExists()
    {
        var customer = NewCustomer(phone: Phone.Create("(31) 99999-8888"));
        await using (var db = mysql.NewContext())
        {
            new CustomerRepository(db).Add(customer);
            await Save(db);
        }

        await using var read = mysql.NewContext();
        var repo = new CustomerRepository(read);
        var loaded = await repo.GetByIdAsync(customer.Id);

        Assert.NotNull(loaded);
        Assert.Equal(customer.Email, loaded.Email);
        Assert.Equal("52998224725", loaded.Document.Value);
        Assert.Equal("31999998888", loaded.Phone?.Value);
        Assert.True(loaded.Active);
        Assert.True(await repo.EmailExistsAsync(customer.Email, null));
        Assert.False(await repo.EmailExistsAsync(customer.Email, customer.Id));
        Assert.False(await repo.EmailExistsAsync(Email.Create("nobody@example.com"), null));
        Assert.Null(await repo.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Customer_WithoutPhone_IsStoredAsNull()
    {
        var customer = NewCustomer();
        await using (var db = mysql.NewContext())
        {
            new CustomerRepository(db).Add(customer);
            await Save(db);
        }

        await using var read = mysql.NewContext();
        Assert.Null((await new CustomerRepository(read).GetByIdAsync(customer.Id))!.Phone);
    }

    [Fact]
    public async Task Customer_UpdateAndRemove_ArePersisted()
    {
        var customer = NewCustomer();
        await using (var db = mysql.NewContext())
        {
            new CustomerRepository(db).Add(customer);
            await Save(db);
        }

        await using (var db = mysql.NewContext())
        {
            var repo = new CustomerRepository(db);
            var loaded = (await repo.GetByIdAsync(customer.Id))!;
            loaded.Deactivate();
            await Save(db);
        }

        await using (var db = mysql.NewContext())
        {
            var repo = new CustomerRepository(db);
            var loaded = (await repo.GetByIdAsync(customer.Id))!;
            Assert.False(loaded.Active);
            repo.Remove(loaded);
            await Save(db);
        }

        await using var read = mysql.NewContext();
        Assert.Null(await new CustomerRepository(read).GetByIdAsync(customer.Id));
    }

    [Fact]
    public async Task Customer_DuplicatedEmail_IsRejectedByUniqueIndex_AsConflict()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await using (var db = mysql.NewContext())
        {
            new CustomerRepository(db).Add(NewCustomer(email));
            await Save(db);
        }

        await using var second = mysql.NewContext();
        new CustomerRepository(second).Add(NewCustomer(email));
        await Assert.ThrowsAsync<ConflictException>(() => Save(second));
    }

    [Fact]
    public async Task Customer_List_IsOrderedAndPaginated()
    {
        // Own database slice: names with a unique prefix keep this test independent of the others.
        var prefix = $"List{Guid.NewGuid():N}"[..12];
        await using (var db = mysql.NewContext())
        {
            var repo = new CustomerRepository(db);
            foreach (var suffix in new[] { "C", "A", "B" })
            {
                repo.Add(NewCustomer(name: prefix + suffix));
            }

            await Save(db);
        }

        await using var read = mysql.NewContext();
        var page = await new CustomerRepository(read).ListAsync(new PageQuery(1, 100));
        var mine = page.Items.Where(c => c.Name.StartsWith(prefix, StringComparison.Ordinal)).Select(c => c.Name).ToList();
        Assert.Equal([prefix + "A", prefix + "B", prefix + "C"], mine);

        var small = await new CustomerRepository(read).ListAsync(new PageQuery(2, 1));
        Assert.Single(small.Items);
        Assert.True(small.TotalCount >= 3);
    }

    [Fact]
    public async Task Product_RoundTrips_AndAnswersSkuExists_AndGetByIds()
    {
        var p1 = NewProduct();
        var p2 = NewProduct();
        await using (var db = mysql.NewContext())
        {
            var repo = new ProductRepository(db);
            repo.Add(p1);
            repo.Add(p2);
            await Save(db);
        }

        await using var read = mysql.NewContext();
        var products = new ProductRepository(read);
        var loaded = await products.GetByIdAsync(p1.Id);

        Assert.Equal(12.34m, loaded!.Price.Amount);
        Assert.Equal(p1.Sku, loaded.Sku);
        Assert.True(await products.SkuExistsAsync(p1.Sku, null));
        Assert.False(await products.SkuExistsAsync(p1.Sku, p1.Id));
        Assert.Equal(2, (await products.GetByIdsAsync([p1.Id, p2.Id, Guid.NewGuid()])).Count);
        Assert.Null(await products.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Product_DuplicatedSku_IsRejectedByUniqueIndex_AsConflict()
    {
        var sku = $"U-{Guid.NewGuid():N}"[..16];
        await using (var db = mysql.NewContext())
        {
            new ProductRepository(db).Add(NewProduct(sku: sku));
            await Save(db);
        }

        await using var second = mysql.NewContext();
        new ProductRepository(second).Add(NewProduct(sku: sku));
        await Assert.ThrowsAsync<ConflictException>(() => Save(second));
    }

    [Fact]
    public async Task Product_ConcurrentStockChange_IsDetected_AsConflict()
    {
        var product = NewProduct(stock: 10);
        await using (var db = mysql.NewContext())
        {
            new ProductRepository(db).Add(product);
            await Save(db);
        }

        await using var a = mysql.NewContext();
        await using var b = mysql.NewContext();
        var fromA = (await new ProductRepository(a).GetByIdAsync(product.Id))!;
        var fromB = (await new ProductRepository(b).GetByIdAsync(product.Id))!;

        fromA.AdjustStock(-6);
        fromB.AdjustStock(-6);
        await Save(a);

        await Assert.ThrowsAsync<ConflictException>(() => Save(b));
    }

    [Fact]
    public async Task Product_List_IsPaginated_AndRemoveWorks()
    {
        var product = NewProduct(name: $"Zed{Guid.NewGuid():N}"[..12]);
        await using (var db = mysql.NewContext())
        {
            new ProductRepository(db).Add(product);
            await Save(db);
        }

        await using (var db = mysql.NewContext())
        {
            var repo = new ProductRepository(db);
            var page = await repo.ListAsync(new PageQuery(1, 100));
            Assert.Contains(page.Items, p => p.Id == product.Id);

            repo.Remove((await repo.GetByIdAsync(product.Id))!);
            await Save(db);
        }

        await using var read = mysql.NewContext();
        Assert.Null(await new ProductRepository(read).GetByIdAsync(product.Id));
    }

    private static OrderLine Line(Guid productId, int qty = 2, decimal price = 5m) => new(productId, Money.Create(price), qty);

    [Fact]
    public async Task Order_PersistsItems_AndRecomputesTotal()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), [Line(productId, 3, 5m), Line(Guid.NewGuid(), 1, 1.5m)], DateTimeOffset.UtcNow);
        await using (var db = mysql.NewContext())
        {
            new OrderRepository(db).Add(order);
            await Save(db);
        }

        await using var read = mysql.NewContext();
        var repo = new OrderRepository(read);
        var loaded = (await repo.GetByIdAsync(order.Id))!;

        Assert.Equal(OrderStatus.Pending, loaded.Status);
        Assert.Equal(2, loaded.Items.Count);
        Assert.Equal(16.5m, loaded.Total.Amount);
        Assert.True(await repo.ExistsForCustomerAsync(order.CustomerId));
        Assert.False(await repo.ExistsForCustomerAsync(Guid.NewGuid()));
        Assert.True(await repo.ExistsForProductAsync(productId));
        Assert.False(await repo.ExistsForProductAsync(Guid.NewGuid()));
        Assert.Null(await repo.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Order_ReplaceItems_DeletesOldRowsAndInsertsNewOnes()
    {
        var oldProduct = Guid.NewGuid();
        var newProduct = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), [Line(oldProduct)], DateTimeOffset.UtcNow);
        await using (var db = mysql.NewContext())
        {
            new OrderRepository(db).Add(order);
            await Save(db);
        }

        await using (var db = mysql.NewContext())
        {
            var loaded = (await new OrderRepository(db).GetByIdAsync(order.Id))!;
            loaded.ReplaceItems([Line(newProduct, 4, 2m)]);
            loaded.Confirm();
            await Save(db);
        }

        await using var read = mysql.NewContext();
        var reloaded = (await new OrderRepository(read).GetByIdAsync(order.Id))!;
        Assert.Equal(OrderStatus.Confirmed, reloaded.Status);
        var item = Assert.Single(reloaded.Items);
        Assert.Equal(newProduct, item.ProductId);
        Assert.Equal(8m, reloaded.Total.Amount);
        Assert.Equal(1, await read.Database.SqlQuery<int>($"SELECT COUNT(*) AS `Value` FROM order_items WHERE OrderId = {order.Id}").SingleAsync());
    }

    [Fact]
    public async Task Order_Remove_CascadesToItems_AndListIncludesItems()
    {
        var order = Order.Create(Guid.NewGuid(), [Line(Guid.NewGuid()), Line(Guid.NewGuid())], DateTimeOffset.UtcNow);
        await using (var db = mysql.NewContext())
        {
            new OrderRepository(db).Add(order);
            await Save(db);
        }

        await using (var db = mysql.NewContext())
        {
            var page = await new OrderRepository(db).ListAsync(new PageQuery(1, 100));
            Assert.Equal(2, page.Items.Single(o => o.Id == order.Id).Items.Count);
        }

        await using (var db = mysql.NewContext())
        {
            var repo = new OrderRepository(db);
            repo.Remove((await repo.GetByIdAsync(order.Id))!);
            await Save(db);
        }

        await using var read = mysql.NewContext();
        Assert.Equal(0, await read.Database.SqlQuery<int>($"SELECT COUNT(*) AS `Value` FROM order_items WHERE OrderId = {order.Id}").SingleAsync());
    }

    [Fact]
    public async Task Migrations_AreFullyApplied()
    {
        await using var db = mysql.NewContext();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}
