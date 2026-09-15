using Microsoft.AspNetCore.Http;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Browsers can't set headers on a WebSocket, so SignalR's browser client (Blazor WebAssembly) sends the token as the
/// access_token query string. Unread, the hub sees an anonymous user and never records the session's SignalRConnectionId.
/// The .NET client outside a browser sends a header, so the SignalR integration tests can't catch this.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class GetAccessTokenTests
{
    [TestMethod]
    public void TheAppHub_Should_AcceptTheTokenFromTheQueryString()
    {
        Assert.AreEqual("hub-token", Request("/app-hub", "?id=connection&access_token=hub-token").GetAccessToken());
    }

    [TestMethod]
    public void TheAppHub_Should_PreferTheQueryStringOverAStaleCookie()
    {
        Assert.AreEqual("hub-token", Request("/app-hub", "?access_token=hub-token", cookie: "access_token=stale-token").GetAccessToken());
    }

    [TestMethod]
    public void TheHeader_Should_WinOverTheQueryString()
    {
        Assert.AreEqual("header-token", Request("/app-hub", "?access_token=hub-token", authorization: "Bearer header-token").GetAccessToken());
    }

    [TestMethod]
    public void OtherEndpoints_Should_IgnoreTheQueryString()
    {
        Assert.IsNull(Request("/api/v1/User/GetCurrentUser", "?access_token=leaked-token").GetAccessToken(),
            "A token in a url lands in server and proxy logs, so only the hub may accept one.");
    }


    private static HttpContext Request(string path, string query, string? authorization = null, string? cookie = null)
    {
        var context = new DefaultHttpContext();

        context.Request.Path = path;
        context.Request.QueryString = new(query);

        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        if (cookie is not null)
        {
            context.Request.Headers.Cookie = cookie;
        }

        return context;
    }
}
