//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasIndex(userRole => new { userRole.RoleId, userRole.UserId }).IsUnique();

        builder.HasData(new UserRole { RoleId = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7"), UserId = Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7") });
    }
}
