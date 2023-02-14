namespace BlazorWeb.Api.Services.Contracts;

public interface IUserInformationProvider
{
    bool IsAuthenticated();

    IEnumerable<Claim> GetClaims();

    ClaimsIdentity GetClaimsIdentity();

    int GetUserId();

    string GetUserName();
}
