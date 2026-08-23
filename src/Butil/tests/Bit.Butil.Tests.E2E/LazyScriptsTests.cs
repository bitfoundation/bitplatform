using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The lazy script-loading mode, end to end: the harness page is opened with <c>?lazy=1</c>, so the app
/// starts with no <c>bit-butil.js</c> on the page and <c>BitButil.UseLazyScripts()</c> on (see
/// <c>Bit.Butil.Samples.Web/Program.cs</c>). Every <c>BitButil.*</c> namespace present in the browser is
/// then there only because the library <c>import()</c>ed that module on first use.
/// </summary>
/// <remarks>
/// The rest of the suite covers bundle mode; the point here is not to re-test every API but to prove the
/// mechanism in a real browser: modules arrive on demand, one at a time, self-contained (a module and the
/// helpers it depends on both work), and the calls behave exactly as in bundle mode.
/// </remarks>
[TestClass]
public class LazyScriptsTests : ButilHarnessTestBase
{
    protected override string HarnessRoute => "/e2e?lazy=1";

    [TestMethod]
    public async Task Page_Starts_Without_The_Bundle()
    {
        // No <script> tag for the bundle was written, and nothing has been called yet.
        var bundleScripts = await Page.EvaluateAsync<int>("document.querySelectorAll('script[src*=\"bit-butil.js\"]').length");
        Assert.AreEqual(0, bundleScripts, "the bundle must not be on the page in lazy mode");

        var namespaces = await LoadedNamespacesAsync();
        Assert.IsEmpty(namespaces, "no Butil module may be loaded before the first call");
    }

    [TestMethod]
    public async Task First_Call_Imports_Only_That_Module()
    {
        await ClickAndExpectAsync("crypto-uuid", "crypto:uuid:");

        var namespaces = await LoadedNamespacesAsync();
        Assert.Contains("crypto", namespaces);
        // Every module carries the prelude and utils it depends on.
        Assert.Contains("utils", namespaces);
        Assert.Contains("version", namespaces);
        // ...and nothing else: no storage, no window, no document.
        Assert.DoesNotContain("storage", namespaces);
        Assert.DoesNotContain("window", namespaces);
        Assert.DoesNotContain("document", namespaces);
    }

    [TestMethod]
    public async Task Modules_Accumulate_As_APIs_Are_Used()
    {
        await ClickAndExpectAsync("ls-clear", "ls:clear");
        var afterStorage = await LoadedNamespacesAsync();
        Assert.Contains("storage", afterStorage);
        Assert.DoesNotContain("window", afterStorage);

        await ClickAndExpectAsync("window-base64", "window:b64:YnV0aWw=/butil");
        var afterWindow = await LoadedNamespacesAsync();
        Assert.Contains("storage", afterWindow);
        Assert.Contains("window", afterWindow);
    }

    [TestMethod]
    public async Task Calls_Behave_As_In_Bundle_Mode()
    {
        // The same flows the bundle-mode fixtures assert on, across the sync-fast (storage), async
        // (crypto) and DTO-returning (document) paths.
        await ClickAndExpectAsync("ls-clear", "ls:clear");
        await ClickAndExpectAsync("ls-set", "ls:set");
        await ClickAndExpectAsync("ls-get", "ls:get:butil-e2e-value");
        await ClickAndExpectAsync("ls-typed-set", "ls:typed-set");
        await ClickAndExpectAsync("ls-typed-get", "ls:typed-get:42/answer");
        await ClickAndExpectAsync("crypto-digest", "crypto:sha256:2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        await ClickAndExpectAsync("crypto-roundtrip", "crypto:aes-gcm:True");
        await ClickAndExpectAsync("doc-title", "doc:title:butil-e2e-title");
        await ClickAndExpectAsync("history-scroll", "history:scroll:Manual");
    }

    [TestMethod]
    public async Task Modules_Are_Fetched_From_The_Package_Content_Path()
    {
        await ClickAndExpectAsync("cookie-set", "cookie:set");

        // Resource timing rather than Playwright's request events: the sample registers a service worker,
        // and fetches it answers still show up here.
        var requests = await Page.EvaluateAsync<string[]>(
            "() => performance.getEntriesByType('resource').map(e => e.name).filter(n => n.includes('/_content/Bit.Butil/'))");

        Assert.Contains((string name) => name.EndsWith("/_content/Bit.Butil/modules/cookie.js", StringComparison.Ordinal), requests);
        Assert.DoesNotContain((string name) => name.EndsWith("/bit-butil.js", StringComparison.Ordinal), requests);
    }

    [TestMethod]
    public async Task Only_The_Modules_Used_Are_Fetched_And_One_File_Each()
    {
        // Three APIs, three modules. Two things are being asserted at once, and both are the point of
        // the feature: nothing that was not called is downloaded, and a module arrives as ONE file
        // rather than as itself plus a request per dependency - the per-module files are built
        // self-contained precisely so a lazy app does not pay a round trip for `utils`.
        await ClickAndExpectAsync("ls-clear", "ls:clear");
        await ClickAndExpectAsync("crypto-uuid", "crypto:uuid:");
        await ClickAndExpectAsync("doc-title", "doc:title:butil-e2e-title");

        var fetched = await Page.EvaluateAsync<string[]>(
            """
            () => [...new Set(performance.getEntriesByType('resource')
                .map(entry => entry.name)
                .filter(name => name.includes('/_content/Bit.Butil/modules/'))
                .map(name => name.substring(name.lastIndexOf('/') + 1)))].sort()
            """);

        Assert.AreSequenceEqual(new[] { "crypto.js", "document.js", "storage.js" }, fetched,
            $"a lazy app should fetch exactly one file per module it uses, but it fetched: {string.Join(", ", fetched)}");
    }

    /// <summary>The <c>BitButil.*</c> keys currently registered on the page, or empty when nothing is loaded.</summary>
    private Task<string[]> LoadedNamespacesAsync()
        => Page.EvaluateAsync<string[]>("() => window.BitButil ? Object.keys(window.BitButil) : []");
}
