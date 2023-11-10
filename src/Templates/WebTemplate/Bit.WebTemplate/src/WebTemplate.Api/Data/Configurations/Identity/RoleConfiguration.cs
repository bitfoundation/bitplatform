using WebTemplate.Api.Models.Identity;

namespace WebTemplate.Api.Data.Configurations.Identity;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(role => role.Name).HasMaxLength(50);
    }
}

