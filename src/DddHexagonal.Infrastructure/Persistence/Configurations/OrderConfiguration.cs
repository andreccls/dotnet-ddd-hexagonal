using DddHexagonal.Domain.Orders;
using DddHexagonal.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DddHexagonal.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>, IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        // No navigation to Customer: aggregates reference each other by id only.
        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.HasIndex(o => o.CustomerId);

        builder.HasMany(o => o.Items).WithOne().HasForeignKey("OrderId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.UnitPrice).HasConversion(m => m.Amount, v => Money.Create(v)).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();
        builder.HasIndex(i => i.ProductId);
    }
}
