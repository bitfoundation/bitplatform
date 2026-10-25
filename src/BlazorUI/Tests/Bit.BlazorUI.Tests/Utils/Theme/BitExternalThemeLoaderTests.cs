using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitExternalThemeLoaderTests : BunitTestContext
{
    // A root-relative path is the common same-origin case. A Unix-like runtime - browser-wasm among them - parses it as
    // an absolute file:// URI, which the scheme check must not take it for: a WebAssembly app would be refused every one.
    [TestMethod,
        DataRow("/_content/Bit.BlazorUI.Extras/styles/bit.blazorui.cupertino.css"),
        DataRow("/css/dark.css"),
        DataRow("themes/light.css"),
        DataRow("https://example.com/theme.css"),
        DataRow("http://example.com/theme.css")
    ]
    public async Task ASameOriginOrHttpHrefReachesTheBrowser(string href)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var loader = new BitExternalThemeLoader(Context.JSInterop.JSRuntime);

        await loader.AttachStylesheetAsync("theme-link", href);

        var invocation = Context.JSInterop.Invocations.Single();
        CollectionAssert.AreEqual(new object[] { "theme-link", href }, invocation.Arguments.ToArray());
    }

    [TestMethod,
        DataRow("//evil.example/x.css"),
        DataRow(@"\\evil.example\x.css"),
        DataRow(@"/\evil.example/x.css"),
        DataRow("file:///etc/x.css"),
        DataRow("javascript:alert(1)"),
        DataRow("data:text/css,body{}"),
        DataRow("vbscript:x")
    ]
    public void AnUnsafeHrefIsRefusedBeforeTheBrowserIsAsked(string href)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var loader = new BitExternalThemeLoader(Context.JSInterop.JSRuntime);

        Assert.ThrowsExactly<ArgumentException>(() => { _ = loader.AttachStylesheetAsync("theme-link", href); });
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count);
    }
}
