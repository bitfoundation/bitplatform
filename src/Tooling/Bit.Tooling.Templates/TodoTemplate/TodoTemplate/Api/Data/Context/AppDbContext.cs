using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoTemplate.Api.Data.Models.Account;
using TodoTemplate.Api.Utilities;

namespace TodoTemplate.Api.Data.Context
{
    public class AppDbContext : IdentityDbContext<User, Role, int>
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.RegisterEntityTypeConfiguration(typeof(AppDbContext).Assembly);
        }
    }
}
