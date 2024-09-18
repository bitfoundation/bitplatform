namespace Boilerplate.Client.Core.Services.Contracts;

public interface IAuthTokenProvider
{
    bool InPrerenderSession { get; }
    Task<string?> GetAccessTokenAsync();
}
