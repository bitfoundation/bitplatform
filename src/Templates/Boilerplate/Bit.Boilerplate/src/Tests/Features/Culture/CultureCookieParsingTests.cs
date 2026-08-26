//+:cnd:noEmit
namespace Boilerplate.Tests.Features.Culture;

/// <summary>
/// The WASM head reads the UI culture out of the <c>.AspNetCore.Culture</c> cookie before <c>host.RunAsync()</c>, on
/// the way to its very first render. The parse used to slice from <c>IndexOf("|uic=") + 5</c> without checking the
/// result, so a value that is not the shape it expects took the app somewhere it could not come back from: at best a
/// silently wrong culture, at worst an <see cref="ArgumentOutOfRangeException"/> thrown out of <c>Main</c> on every
/// single load, leaving the app stuck on its loading screen until the user cleared cookies.
/// <para>
/// No writer in the product emits a malformed value - <c>CultureService</c> and ASP.NET Core's own
/// <c>CookieRequestCultureProvider.MakeCookieValue</c> both always write <c>c=x|uic=y</c> - which is exactly why this
/// needs a test rather than a run: the inputs that break it are the ones the app never produces for itself.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest"), TestCategory("Culture")]
public class CultureCookieParsingTests
{
    [TestMethod]
    [DataRow("c=en-US|uic=en-US", "en-US")]
    [DataRow("c=fa-IR|uic=fa-IR", "fa-IR")]
    [DataRow("c=en-US|uic=fa-IR", "fa-IR", DisplayName = "the UI culture is the one that is read, not the formatting culture")]
    [DataRow("c%3Den-US%7Cuic%3Den-US", "en-US", DisplayName = "a percent-encoded cookie value")]
    public void AWellFormedCultureCookie_Should_YieldItsUiCulture(string cookieValue, string expected)
    {
        Assert.AreEqual(expected, Boilerplate.Client.Web.Program.ExtractUiCulture(cookieValue));
    }

    /// <summary>
    /// Each of these used to produce a wrong answer or an exception. <c>"c=fa-IR"</c> is the illustrative one: with no
    /// <c>|uic=</c>, <c>IndexOf</c> returned -1, the slice started at index 4, and the caller received the string
    /// <c>"-IR"</c> - which is not a culture, but IS non-null, so it also shadowed the browser-language fallback
    /// behind it. <c>""</c> is the one that crashed: Butil's cookie reader returns an empty string, not null, for a
    /// cookie that is present with no value, so <c>""[4..]</c> threw.
    /// </summary>
    [TestMethod]
    [DataRow(null, DisplayName = "no cookie at all")]
    [DataRow("", DisplayName = "present but empty - the shape that threw out of Main")]
    [DataRow("x", DisplayName = "shorter than the old slice offset")]
    [DataRow("c=fa-IR", DisplayName = "a formatting culture with no UI culture")]
    [DataRow("nonsense", DisplayName = "not a culture cookie at all")]
    [DataRow("c=en-US|uic=", DisplayName = "an empty UI culture")]
    [DataRow("c=en-US|uic=   ", DisplayName = "a whitespace UI culture")]
    public void AMalformedCultureCookie_Should_BeIgnoredRatherThanGuessedAt(string? cookieValue)
    {
        Assert.IsNull(Boilerplate.Client.Web.Program.ExtractUiCulture(cookieValue),
            "Returning anything non-null here shadows the browser's own language, which is the fallback the user " +
            "would otherwise get; throwing aborts Main before RunAsync and the app never renders.");
    }
}
