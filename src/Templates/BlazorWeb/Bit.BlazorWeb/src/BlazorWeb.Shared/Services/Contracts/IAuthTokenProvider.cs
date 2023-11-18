namespace BlazorWeb.Shared.Services.Contracts;

public interface IAuthTokenProvider
{
    Task<string?> GetAccessTokenAsync();

    Task<string?> GetRefreshTokenAsync();
}
