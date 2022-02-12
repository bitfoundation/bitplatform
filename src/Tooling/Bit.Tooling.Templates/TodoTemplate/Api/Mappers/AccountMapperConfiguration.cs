using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Mappers;

public class AccountMapperConfiguration : Profile
{
    public AccountMapperConfiguration()
    {
        CreateMap<Role, RoleDto>().ReverseMap();

        CreateMap<User, UserDto>().ReverseMap();
    }
}
