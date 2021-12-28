using TodoTemplate.Shared.Models.Account;

namespace TodoTemplate.Api.Repository.Contracts
{
    public interface IUserRepository
    {
       IEnumerable<User> GetAll();
    }
}
