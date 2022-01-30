using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Contracts
{
    public interface IJwtService
    {
        Task<ResponseTokenDto> GenerateToken(RequestTokenDto dto);
    }
}
