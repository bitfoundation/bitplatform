using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// Regression tests for issue #12752: navigating between pages that both provide SectionContent
/// for the same SectionId (rendered into a shared layout's SectionOutlet) must not throw the
/// framework's "There is already a subscriber to the content with the given section ID" error.
/// </summary>
[TestClass]
public class SectionOutletTests : BunitTestContext
{
    [TestMethod]
    public void Navigating_forward_between_pages_with_section_content_does_not_throw()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/section-a");

        var cut = RenderComponent<SectionHost>();
        cut.WaitForAssertion(() =>
            Assert.AreEqual("footer-from-a", cut.Find("[data-testid=footer-content]").TextContent));

        nav.NavigateTo("http://localhost/section-b");

        cut.WaitForAssertion(() =>
            Assert.AreEqual("footer-from-b", cut.Find("[data-testid=footer-content]").TextContent));

        // The issue's "navigate away and back" step: returning to the first page must resubscribe
        // cleanly too.
        nav.NavigateTo("http://localhost/section-a");

        cut.WaitForAssertion(() =>
            Assert.AreEqual("footer-from-a", cut.Find("[data-testid=footer-content]").TextContent));
    }

    [TestMethod]
    public void Navigating_back_to_an_earlier_declared_route_with_section_content_does_not_throw()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/section-b");

        var cut = RenderComponent<SectionHost>();
        cut.WaitForAssertion(() =>
            Assert.AreEqual("footer-from-b", cut.Find("[data-testid=footer-content]").TextContent));

        nav.NavigateTo("http://localhost/section-a");

        cut.WaitForAssertion(() =>
            Assert.AreEqual("footer-from-a", cut.Find("[data-testid=footer-content]").TextContent));
    }
}
