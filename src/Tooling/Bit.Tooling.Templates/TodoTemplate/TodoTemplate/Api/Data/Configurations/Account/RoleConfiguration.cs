
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoTemplate.Api.Data.Models.Account;

namespace TodoTemplate.Api.Data.Configurations.Account
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.Property(role => role.Name).HasMaxLength(50);
        }
    }
}
