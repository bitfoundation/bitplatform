//+:cnd:noEmit
using System.Text;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// <c>IAuthTokenProvider.ParseAccessToken</c> has to be total. The value it is handed is not always one the app
/// minted: on the web client it comes from browser storage, and on Server.Web <c>ServerSideAuthTokenProvider</c>
/// returns the request's own <c>access_token</c> cookie verbatim during pre-rendering. Its callers include
/// <c>AuthDelegatingHandler</c>, whose catch filter only covers <c>ForbiddenException</c> and
/// <c>UnauthorizedException</c>, so anything else it throws surfaces as a generic error dialog - or, during
/// server-side rendering, somewhere worse.
/// </summary>
[TestClass, TestCategory("UnitTest"), TestCategory("Identity")]
public partial class AccessTokenParsingTests
{
    /// <summary>
    /// The payload <c>null</c> is the one malformed shape that does not throw on the way in: it deserializes to a
    /// null dictionary, which then throws <see cref="NullReferenceException"/> on first use - a type the guard in
    /// <c>ParseAccessToken</c> does not catch, because every other malformed input fails as a
    /// <c>FormatException</c> or a <c>JsonException</c> first.
    /// </summary>
    [TestMethod]
    // "bnVsbA" is base64url for the four characters `null`.
    [DataRow("header.bnVsbA.signature", DisplayName = "payload is the JSON literal null")]
    [DataRow("not-a-jwt", DisplayName = "no dots at all")]
    [DataRow("header..signature", DisplayName = "empty payload")]
    [DataRow("header.!!!not-base64!!!.signature", DisplayName = "payload is not base64")]
    [DataRow("header.eyJhIjox.signature", DisplayName = "payload is truncated json")]
    [DataRow("header.WyJhbiIsImFycmF5Il0.signature", DisplayName = "payload is a json array, not an object")]
    public void AMalformedAccessToken_Should_ParseAsAnonymous(string accessToken)
    {
        foreach (var validateExpiry in new[] { true, false })
        {
            var user = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry);

            Assert.IsFalse(user.IsAuthenticated(),
                $"'{accessToken}' (validateExpiry: {validateExpiry}) must yield an anonymous principal, not throw and " +
                $"not authenticate.");
        }
    }

    /// <summary>
    /// The control. Without it every assertion above would pass against a method that returned
    /// <c>Anonymous()</c> unconditionally.
    /// </summary>
    [TestMethod]
    public void AWellFormedAccessToken_Should_StillParse()
    {
        var user = IAuthTokenProvider.ParseAccessToken(BuildToken(expiresOn: DateTimeOffset.UtcNow.AddMinutes(5)), validateExpiry: true);

        Assert.IsTrue(user.IsAuthenticated());
        var name = user.FindFirst("name");

        Assert.IsNotNull(name);
        Assert.AreEqual("the-user", name.Value);
    }

    /// <summary>
    /// Expiry has to fail closed. The check used to be skipped entirely when <c>exp</c> could not be read, which
    /// would have turned an unreadable claim into a session that never expires on the client - and
    /// <c>AuthDelegatingHandler</c> and <c>GetFreshAccessToken</c> both treat "still valid" as permission to keep
    /// using the token without refreshing it.
    /// </summary>
    [TestMethod]
    [DataRow("\"not-a-number\"", DisplayName = "exp is a string")]
    [DataRow("null", DisplayName = "exp is null")]
    [DataRow("absent", DisplayName = "exp is missing entirely")]
    public void AnUnreadableExpiry_Should_CountAsExpired(string rawExp)
    {
        var payload = rawExp is "absent"
            ? """{"name":"the-user"}"""
            : $$"""{"name":"the-user","exp":{{rawExp}}}""";

        var accessToken = $"header.{ToBase64Url(payload)}.signature";

        Assert.IsFalse(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated(),
            "A token whose expiry cannot be read must be treated as expired, never as valid forever.");

        // ...while the caller that deliberately ignores expiry (GetAuthenticationStateAsync, so the app can render
        // before it has refreshed) still gets the claims.
        Assert.IsTrue(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false).IsAuthenticated());
    }

    [TestMethod]
    public void AnExpiredAccessToken_Should_ParseAsAnonymous_OnlyWhenExpiryIsValidated()
    {
        var accessToken = BuildToken(expiresOn: DateTimeOffset.UtcNow.AddMinutes(-5));

        Assert.IsFalse(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated());
        Assert.IsTrue(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false).IsAuthenticated());
    }

    private static string BuildToken(DateTimeOffset expiresOn)
    {
        return $"header.{ToBase64Url($$"""{"name":"the-user","exp":{{expiresOn.ToUnixTimeSeconds()}}}""")}.signature";
    }

    private static string ToBase64Url(string json)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
