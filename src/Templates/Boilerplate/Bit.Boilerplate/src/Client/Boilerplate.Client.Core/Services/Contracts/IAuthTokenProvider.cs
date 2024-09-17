namespace Boilerplate.Client.Core.Services.Contracts;

public interface IAuthTokenProvider
{
    bool IsInitialized { get; }
    Task<string?> GetAccessTokenAsync();
}
