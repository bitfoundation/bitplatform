namespace AdminPanel.Server.Api.Services.Implementations;

public partial class UserInformationProvider : IUserInformationProvider
{
    [AutoInject]
    public IHttpContextAccessor _httpContextAccessor = default!;

    public IEnumerable<Claim> GetClaims()
    {
        if (IsAuthenticated() is false)
        {
            throw new InvalidOperationException();
        }

        return GetClaimsIdentity().Claims;
    }

    public ClaimsIdentity GetClaimsIdentity()
    {
        if (IsAuthenticated() is false)
        {
            throw new InvalidOperationException();
        }

        return (ClaimsIdentity)_httpContextAccessor.HttpContext!.User.Identity!;
    }

    public int GetUserId()
    {
        if (IsAuthenticated() is false)
        {
            throw new InvalidOperationException();
        }

        return _httpContextAccessor.HttpContext!.User.GetUserId();
    }

    public string GetUserName()
    {
        if (IsAuthenticated() is false)
        {
            throw new InvalidOperationException();
        }

        return _httpContextAccessor.HttpContext!.User.GetUserName();
    }

    public bool IsAuthenticated()
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            throw new InvalidOperationException();
        }

        return _httpContextAccessor.HttpContext.User?.Identity?.IsAuthenticated is true;
    }
}
