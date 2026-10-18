using Bit.Bsqlite.Demo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bit.Bsqlite.Demo.Data.Configuration;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.PublicId).IsRequired();
        builder.HasIndex(c => c.PublicId).IsUnique();
        builder.Property(c => c.DisplayName).IsRequired().HasMaxLength(120);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(c => c.Email).IsUnique();
        builder.Property(c => c.CreditLimit).HasPrecision(18, 2);
        builder.Property(c => c.Notes).HasMaxLength(500);
    }
}
