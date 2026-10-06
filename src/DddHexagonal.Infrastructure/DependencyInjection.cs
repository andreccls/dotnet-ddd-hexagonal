using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Infrastructure.Persistence;
using DddHexagonal.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DddHexagonal.Infrastructure;

/// <summary>Registers the outbound adapters. Called once, by the composition root (Api).</summary>
public static class DependencyInjection
{
    // Explicit version => no connection is opened at startup just to detect it.
    internal static readonly ServerVersion ServerVersion = new MySqlServerVersion(new Version(8, 4, 0));

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(o => o.UseMySql(connectionString, ServerVersion));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("mysql");
        return services;
    }

    /// <summary>Applies pending EF migrations. Convenient for dev/demo; run it as a separate job in real deployments.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);
    }
}
