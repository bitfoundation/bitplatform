namespace Boilerplate.Client.Core.Services.Contracts;

public interface IAuthTokenProvider
{
    Task<string?> GetAccessTokenAsync();
}
