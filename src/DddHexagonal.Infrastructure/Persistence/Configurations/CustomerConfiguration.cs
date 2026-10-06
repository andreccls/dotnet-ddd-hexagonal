using DddHexagonal.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DddHexagonal.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasConversion(e => e.Value, v => Email.Create(v)).HasMaxLength(254).IsRequired();
        builder.Property(c => c.Document).HasConversion(d => d.Value, v => Cpf.Create(v)).HasMaxLength(11).IsFixedLength().IsRequired();
        builder.Property(c => c.Phone).HasConversion(p => p!.Value, v => Phone.Create(v)).HasMaxLength(15);
        builder.Property(c => c.Active).IsRequired();

        // Last line of defence for "e-mail is unique"; the use case checks first to give a friendly 409.
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
