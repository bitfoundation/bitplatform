using Microsoft.EntityFrameworkCore;
using TodoTemplate.Api.Context;
using TodoTemplate.Api.Repository.Contracts;
using TodoTemplate.Shared.Models.Account;

namespace TodoTemplate.Api.Repository.Implementations
{
    public class RoleRepository : IRoleRepository
    {
        protected readonly AppDbContext DbContext;

        public RoleRepository(AppDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public async Task<List<Role>> GetAllRole()
        {
            return await DbContext.Roles.ToListAsync();
        }

        public async Task<Role> GetByIdRole(int roleId)
        {
            return await DbContext.Roles.FirstOrDefaultAsync(user => user.Id == roleId);
        }

        public async void AddRole(Role role)
        {
            await DbContext.AddAsync(role);
            await DbContext.SaveChangesAsync();
        }

        public async void UpdateRole(Role role)
        {
            DbContext.Update(role);
            await DbContext.SaveChangesAsync();
        }

        public async void DeleteRole(Role role)
        {
            DbContext.Remove(role);
            await DbContext.SaveChangesAsync();
        }
    }
}
