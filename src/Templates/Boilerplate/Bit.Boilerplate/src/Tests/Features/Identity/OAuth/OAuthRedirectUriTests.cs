//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.OAuth.Services;

namespace Boilerplate.Tests.Features.Identity.OAuth;

/// <summary>
/// What a client may be sent back to. The uri comes from a document on the client's own host, and the consent page
/// navigates to whatever it says.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class OAuthRedirectUriTests
{
    /// <summary>A browser runs these rather than leaving for them - in this app's origin, with the code appended.</summary>
    [TestMethod]
    [DataRow("javascript:alert(document.domain)//")]
    [DataRow("JavaScript:void(0)")]
    [DataRow("data:text/html,<script>alert(1)</script>")]
    [DataRow("vbscript:msgbox(1)")]
    [DataRow("file:///etc/passwd")]
    [DataRow("blob:https://example.com/x")]
    [DataRow("about:blank")]
    public void ASchemeTheBrowserExecutes_Should_BeRefused(string redirectUri)
    {
        Assert.IsFalse(OAuthService.IsAcceptableRedirectUri(redirectUri),
            $"'{redirectUri}' was accepted. A redirect uri is for another app to open, not for the browser to run.");
    }

    [TestMethod]
    [DataRow("http://attacker.example/collect", false)]      // plaintext http off this machine
    [DataRow("https://client.example/callback#fragment", false)] // RFC 6749 §3.1.2
    [DataRow("http://127.0.0.1:1/a b", false)]               // whitespace splits the space-delimited storage in two
    [DataRow("/callback", false)]
    [DataRow("https://client.example/callback", true)]
    [DataRow("http://127.0.0.1:41999/callback", true)]
    [DataRow("http://localhost:41999/callback", true)]
    [DataRow("vscode://ms-vscode.folder/auth", true)]
    public void ARedirectUri_Should_BeAcceptedOnlyWhenItCanCarryACodeSafely(string redirectUri, bool expected)
    {
        Assert.AreEqual(expected, OAuthService.IsAcceptableRedirectUri(redirectUri),
            $"'{redirectUri}' was judged wrongly; this predicate is all that stands between a metadata document and where the code goes.");
    }

    [TestMethod]
    public void AnOverlongRedirectUri_Should_BeRefused()
    {
        Assert.IsFalse(OAuthService.IsAcceptableRedirectUri($"https://client.example/{new string('a', 2048)}"),
            "The column these are stored in is bounded; a uri that does not fit must be refused, not truncated into one that matches nothing.");
    }
}
