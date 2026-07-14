#if NET10_0_OR_GREATER
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// .NET 10 not-found contract: application code calls <c>NavigationManager.NotFound()</c> and the
/// router is expected to take over rendering via the <c>OnNotFound</c> event. These tests only run
/// on net10.0 because the API doesn't exist on earlier targets.
/// </summary>
[TestClass]
public class NotFoundInteropTests : BunitTestContext
{
    [TestMethod]
    public void NotFound_call_renders_inline_content_without_changing_the_url()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<NotFoundInteropHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        cut.InvokeAsync(() => nav.NotFound());

        cut.WaitForAssertion(() =>
        {
            // The route content is replaced by the inline fallback; the URL stays on /a because
            // the resource at this URL is what is missing, mirroring the built-in router.
            Assert.IsNotNull(cut.Find("[data-testid=nf-inline]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=a]").Count);
            Assert.IsTrue(nav.Uri.EndsWith("/a", StringComparison.Ordinal));
            Assert.AreEqual(1, cut.Instance.NotFoundHookCount);
            Assert.AreEqual("/a", cut.Instance.LastNotFoundPath);
        });
    }

    [TestMethod]
    public void NotFound_call_redirects_to_the_configured_NotFound_url()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<NotFoundInteropHost>(p => p.Add(x => x.NotFoundUrl, "/404"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        cut.InvokeAsync(() => nav.NotFound());

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=nf-page]"));
            Assert.IsTrue(nav.Uri.EndsWith("/404", StringComparison.Ordinal));
            Assert.AreEqual(1, cut.Instance.NotFoundHookCount);
        });
    }
}
#endif
