using TodoTemplate.Shared.Models.Account;

namespace TodoTemplate.Api.Repository.Contracts
{
    public interface IRoleRepository
    {
        Task<List<Role>> GetAllRole();

        Task<Role> GetByIdRole(int roleId);

        void AddRole(Role role);

        void UpdateRole(Role role);

        void DeleteRole(Role role);
    }
}
