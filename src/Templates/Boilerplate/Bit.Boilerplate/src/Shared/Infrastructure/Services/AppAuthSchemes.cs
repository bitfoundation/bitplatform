//+:cnd:noEmit
namespace Boilerplate.Shared.Infrastructure.Services;

public class AppAuthSchemes
{
    /// <summary>
    /// Validates the tokens this app issues to <b>external</b> apps over OAuth. A scheme of its own so the two never
    /// validate each other's tokens: handing one to somebody else's software is only safe while the app's own scheme
    /// refuses it everywhere except the resource it names. Endpoints that accept it say so alongside
    /// <c>IdentityConstants.BearerScheme</c>; the rest of the api stays first-party only.
    /// </summary>
    public const string OAUTH_BEARER = "OAuthBearer";
}
