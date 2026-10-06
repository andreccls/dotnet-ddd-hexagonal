using DddHexagonal.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DddHexagonal.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Sku).HasConversion(s => s.Value, v => Sku.Create(v)).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Price).HasConversion(m => m.Amount, v => Money.Create(v)).HasPrecision(18, 2).IsRequired();

        // Optimistic concurrency: two requests changing stock at the same time can't overwrite each other.
        builder.Property(p => p.Stock).IsConcurrencyToken();

        builder.HasIndex(p => p.Sku).IsUnique();
    }
}
