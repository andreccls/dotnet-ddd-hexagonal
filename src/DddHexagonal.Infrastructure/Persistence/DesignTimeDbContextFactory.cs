using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DddHexagonal.Infrastructure.Persistence;

/// <summary>Used only by `dotnet ef` (migrations). Never opens a connection: the server version is explicit.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=ddd_hexagonal;User=root;Password=unused", DependencyInjection.ServerVersion)
            .Options;
        return new AppDbContext(options);
    }
}
