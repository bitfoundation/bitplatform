//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasIndex(role => role.Name).IsUnique();
        builder.Property(role => role.Name).HasMaxLength(50);

        builder.HasData(new { Id = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7"), Name = AppBuiltinRoles.SuperAdmin, NormalizedName = AppBuiltinRoles.SuperAdmin.ToUpperInvariant(), ConcurrencyStamp = "8ff71671-a1d6-5f97-abb9-d87d7b47d6e7" });

        builder.HasData(new { Id = Guid.Parse("9ff71672-a1d5-4f97-abb7-d87d6b47d5e8"), Name = AppBuiltinRoles.BasicUser, NormalizedName = AppBuiltinRoles.BasicUser.ToUpperInvariant(), ConcurrencyStamp = "9ff71672-a1d5-4f97-abb7-d87d6b47d5e8" });
    }
}
