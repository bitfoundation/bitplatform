namespace TodoTemplate.Shared.Services.Contracts;

public interface ITokenProvider
{
    Task<string?> GetAcccessToken();
}
