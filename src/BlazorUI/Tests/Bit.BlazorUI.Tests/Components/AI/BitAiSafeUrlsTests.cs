using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI;

/// <summary>
/// Pins the one reading of the addresses the AI views are handed (BitAiSafeUrls): which schemes a link may follow and an
/// image may load, and what the image policy is asked about an image - the address the browser fetches, a relative one
/// resolved against the base address of the document, which is read only for a relative one.
/// </summary>
[TestClass]
public class BitAiSafeUrlsTests : BitAiTestBase
{
    // Internal to the package, so reached through reflection.
    private static readonly Type SafeUrls = typeof(BitAiImagePolicy).Assembly.GetType("Bit.BlazorUI.BitAiSafeUrls", throwOnError: true)!;

    private static string BlankImage => (string)SafeUrls.GetField("BlankImage", BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;

    private static string Link(string url) => (string)Call(nameof(Link), url)!;

    private static string Image(string url) => (string)Call(nameof(Image), url)!;

    // The views hand the helper a reader of the base address rather than the navigation manager, so the address is read
    // only when it is needed - and never where a view could not read it (see BitAiComponentBase).
    private static string Resolve(string source, NavigationManager navigation) => Resolve(source, () => navigation.BaseUri);

    private static string Resolve(string source, Func<string?> baseUri) => (string)Call(nameof(Resolve), source, baseUri)!;

    private static BitAiImageDecision Decide(BitAiImagePolicy policy, string source, NavigationManager navigation) =>
        (BitAiImageDecision)Call(nameof(Decide), policy, source, (Func<string?>)(() => navigation.BaseUri))!;

    private static string HostOf(string url) => (string)Call(nameof(HostOf), url)!;

    private static object? Call(string name, params object?[] arguments)
    {
        try
        {
            return SafeUrls.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments);
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
    }

    // The navigation manager of the test renderer, whose base address is http://localhost/.
    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();



    [TestMethod,
        DataRow("https://x.dev/a"),
        DataRow("http://x.dev/a"),
        DataRow("HTTPS://X.DEV/a"),
        DataRow("mailto:someone@x.dev"),
        DataRow("tel:+15550100"),
        DataRow("ftp://x.dev/file"),
        DataRow("/docs/a"),
        DataRow("docs/a"),
        DataRow("./docs/a"),
        DataRow("//x.dev/a"),
        DataRow("#section"),
        DataRow("?page=2"),
        DataRow("/path:with-colon")]
    public void ALinkKeepsAWebMailOrPhoneAddressAndARelativeOne(string url)
    {
        Assert.AreEqual(url, Link(url));
    }

    [TestMethod,
        DataRow("javascript:alert(1)"),
        DataRow("  JavaScript:alert(1)"),
        DataRow("vbscript:msgbox(1)"),
        DataRow(":x"),
        DataRow("data:text/html,<script>alert(1)</script>"),
        DataRow("data:image/png;base64,iVBORw0KGgo="),
        DataRow("file:///etc/passwd"),
        DataRow(""),
        DataRow("   ")]
    public void ALinkOfAnyOtherSchemeLeadsNowhere(string url)
    {
        Assert.AreEqual("#", Link(url));
    }

    [TestMethod,
        DataRow("https://img.x.dev/a.png"),
        DataRow("http://img.x.dev/a.png"),
        DataRow("/img/a.png"),
        DataRow("img/a.png"),
        DataRow("//img.x.dev/a.png"),
        DataRow("data:image/png;base64,iVBORw0KGgo="),
        DataRow("DATA:IMAGE/gif;base64,R0lGOD=="),
        DataRow("data:image/svg+xml;base64,PHN2Zz4=")]
    public void AnImageKeepsAWebAddressARelativeOneAndInlineImageData(string url)
    {
        Assert.AreEqual(url, Image(url));
    }

    [TestMethod,
        DataRow("javascript:alert(1)"),
        DataRow("  JAVASCRIPT:alert(1)"),
        DataRow("vbscript:msgbox(1)"),
        DataRow(":x"),
        DataRow("data:text/html,<script>alert(1)</script>"),
        DataRow("")]
    public void AnImageOfAnyOtherSourceIsTheBlankPixel(string url)
    {
        Assert.AreEqual(BlankImage, Image(url));
        StringAssert.StartsWith(BlankImage, "data:image/gif;base64,");
    }

    [TestMethod]
    public void TheBlankPixelIsShownUnderEveryPolicySinceItFetchesNothing()
    {
        foreach (var policy in new[] { BitAiImagePolicy.Allow, BitAiImagePolicy.ClickToLoad, BitAiImagePolicy.Block, BitAiImagePolicy.AllowListedHosts("cdn.x.dev") })
        {
            Assert.AreEqual(BitAiImageDecision.Show, Decide(policy, BlankImage, Navigation), policy.Mode.ToString());
        }
    }

    [TestMethod]
    public void AnAbsoluteSourceIsLeftAsItIsAndNeverReadsTheBaseAddress()
    {
        var navigation = new UninitializedNavigationManager();

        // The base address of a navigation manager that was never initialized - a view rendered outside an app - throws.
        Assert.ThrowsExactly<InvalidOperationException>(() => navigation.BaseUri);

        Assert.AreEqual("https://cdn.x.dev/a.png", Resolve("https://cdn.x.dev/a.png", navigation));
        Assert.AreEqual("data:image/png;base64,AAA", Resolve("data:image/png;base64,AAA", navigation));
        Assert.AreEqual("mailto:a@x.dev", Resolve("mailto:a@x.dev", navigation));
        Assert.AreEqual(BitAiImageDecision.Show, Decide(BitAiImagePolicy.AllowListedHosts("cdn.x.dev"), "https://cdn.x.dev/a.png", navigation));
        Assert.AreEqual(BitAiImageDecision.AskFirst, Decide(BitAiImagePolicy.AllowListedHosts("cdn.x.dev"), "https://evil.example/a.png", navigation));
        Assert.AreEqual(BitAiImageDecision.Show, Decide(BitAiImagePolicy.Block, BlankImage, navigation));

        // A relative source is what reads it.
        Assert.ThrowsExactly<InvalidOperationException>(() => Resolve("/img/a.png", navigation));
    }

    [TestMethod]
    public void OnlyAHostListReadsTheBaseAddressOfARelativeSource()
    {
        var navigation = new UninitializedNavigationManager();

        // No other policy depends on the host an image comes from, so a view under one never reads the base address.
        Assert.AreEqual(BitAiImageDecision.Show, Decide(BitAiImagePolicy.Allow, "/img/a.png", navigation));
        Assert.AreEqual(BitAiImageDecision.AskFirst, Decide(BitAiImagePolicy.ClickToLoad, "/img/a.png", navigation));
        Assert.AreEqual(BitAiImageDecision.Hide, Decide(BitAiImagePolicy.Block, "/img/a.png", navigation));

        Assert.ThrowsExactly<InvalidOperationException>(() => Decide(BitAiImagePolicy.AllowListedHosts("localhost"), "/img/a.png", navigation));
    }

    [TestMethod]
    public void ARelativeSourceWithNoBaseAddressToReadIsLeftAsItIs()
    {
        // What a view hands the helper where there is no base address to read: no navigation manager, or one never
        // initialized. The source matches no host then, so a host list asks first.
        Assert.AreEqual("/img/a.png", Resolve("/img/a.png", () => null));
        Assert.AreEqual("img/a.png", Resolve("img/a.png", () => "not an address"));
        Assert.AreEqual(BitAiImageDecision.AskFirst,
                        (BitAiImageDecision)Call(nameof(Decide), BitAiImagePolicy.AllowListedHosts("localhost"), "/img/a.png", (Func<string?>)(() => null))!);
    }

    [TestMethod,
        DataRow("/img/a.png", "https://app.x.dev/img/a.png"),
        DataRow("img/a.png", "https://app.x.dev/base/img/a.png"),
        DataRow("../a.png", "https://app.x.dev/a.png"),
        DataRow("//cdn.x.dev/a.png", "https://cdn.x.dev/a.png"),
        DataRow("?v=2", "https://app.x.dev/base/?v=2")]
    public void ARelativeOrProtocolRelativeSourceIsResolvedAgainstTheBaseAddress(string source, string expected)
    {
        var navigation = new InitializedNavigationManager("https://app.x.dev/base/", "https://app.x.dev/base/chat");

        Assert.AreEqual(expected, Resolve(source, navigation));
    }

    [TestMethod]
    public void TheAppsOwnHostListedShowsItsRelativeImages()
    {
        Assert.AreEqual("http://localhost/", Navigation.BaseUri);

        Assert.AreEqual(BitAiImageDecision.Show, Decide(BitAiImagePolicy.AllowListedHosts("localhost"), "/img/a.png", Navigation));
        Assert.AreEqual(BitAiImageDecision.Show, Decide(BitAiImagePolicy.AllowListedHosts("localhost"), "img/a.png", Navigation));
        Assert.AreEqual(BitAiImageDecision.AskFirst, Decide(BitAiImagePolicy.AllowListedHosts("cdn.x.dev"), "/img/a.png", Navigation));

        // A protocol-relative source is fetched from the host it names, not the app's.
        Assert.AreEqual(BitAiImageDecision.AskFirst, Decide(BitAiImagePolicy.AllowListedHosts("localhost"), "//cdn.x.dev/a.png", Navigation));
        Assert.AreEqual(BitAiImageDecision.Show, Decide(BitAiImagePolicy.AllowListedHosts("cdn.x.dev"), "//cdn.x.dev/a.png", Navigation));

        // The asked-first and blocked policies decide as they always do.
        Assert.AreEqual(BitAiImageDecision.AskFirst, Decide(BitAiImagePolicy.ClickToLoad, "/img/a.png", Navigation));
        Assert.AreEqual(BitAiImageDecision.Hide, Decide(BitAiImagePolicy.Block, "/img/a.png", Navigation));
    }

    [TestMethod]
    public void TheHostOfAnAddressNamesAPlaceholder()
    {
        Assert.AreEqual("img.x.dev", HostOf("https://img.x.dev/a.png"));
        Assert.AreEqual("localhost", HostOf(Resolve("/img/a.png", Navigation)));
        Assert.AreEqual("not an address", HostOf("not an address"));
    }



    private sealed class UninitializedNavigationManager : NavigationManager
    {
        protected override void NavigateToCore(string uri, NavigationOptions options) => throw new NotSupportedException();
    }

    private sealed class InitializedNavigationManager : NavigationManager
    {
        public InitializedNavigationManager(string baseUri, string uri) => Initialize(baseUri, uri);

        protected override void NavigateToCore(string uri, NavigationOptions options) => throw new NotSupportedException();
    }
}
