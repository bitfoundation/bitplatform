using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoTemplate.Api.Data.Models.Account;

namespace TodoTemplate.Api.Data.Context
{
    public class TodoTemplateDbContext : IdentityDbContext<User, Role, int>
    {
        public TodoTemplateDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(TodoTemplateDbContext).Assembly);
        }
    }
}
