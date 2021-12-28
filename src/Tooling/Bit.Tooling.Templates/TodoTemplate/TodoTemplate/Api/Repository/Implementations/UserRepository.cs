using TodoTemplate.Api.Context;
using TodoTemplate.Api.Repository.Contracts;
using TodoTemplate.Shared.Models.Account;

namespace TodoTemplate.Api.Repository.Implementations
{
    public class UserRepository : IUserRepository
    {
        protected readonly AppDbContext DbContext;

        public UserRepository(AppDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public IEnumerable<User> GetAll()
        {
            return DbContext.Users;
        }
    }
}
