namespace BlazorWeb.Shared.Services.Contracts;

public interface IAuthTokenProvider
{
    bool IsInitialized { get; }
    Task<string?> GetAccessTokenAsync();
}
