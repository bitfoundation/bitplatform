//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasIndex(role => role.Name).IsUnique();
        builder.Property(role => role.Name).HasMaxLength(50);

        builder.HasData(new { Id = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7"), Name = AppRoles.SUPER_ADMIN, NormalizedName = AppRoles.SUPER_ADMIN.ToUpperInvariant(), ConcurrencyStamp = "8ff71671-a1d6-5f97-abb9-d87d7b47d6e7" });
    }
}
