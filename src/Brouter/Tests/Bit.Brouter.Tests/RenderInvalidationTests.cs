using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// Pins the render-invalidation contract that SetMatched relies on: a single render request at
/// the top of the matched chain must reveal the whole chain (down to a deep leaf), and the
/// pipeline's final render must unrender routes that lost the match.
/// </summary>
[TestClass]
public class RenderInvalidationTests : BunitTestContext
{
    [TestMethod]
    public void Navigating_between_siblings_unrenders_the_losing_route()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/plain");

        var cut = RenderComponent<RenderInvalidationHost>();
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=plain]")));

        nav.NavigateTo("http://localhost/other");

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=plain]").Count,
                "the previously matched sibling must be unrendered");
        });
    }

    [TestMethod]
    public void Deep_chain_renders_every_level_after_navigating_in_from_a_sibling()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/plain");

        var cut = RenderComponent<RenderInvalidationHost>();
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=plain]")));

        nav.NavigateTo("http://localhost/l1/42/l2/l3");

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("top:42", cut.Find("[data-testid=top]").TextContent);
            Assert.IsNotNull(cut.Find("[data-testid=mid]"));
            Assert.IsNotNull(cut.Find("[data-testid=leaf]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=plain]").Count);
        });
    }

    [TestMethod]
    public void Deep_chain_re_renders_with_new_parameters_when_only_a_parameter_changes()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/l1/1/l2/l3");

        var cut = RenderComponent<RenderInvalidationHost>();
        cut.WaitForAssertion(() => Assert.AreEqual("top:1", cut.Find("[data-testid=top]").TextContent));

        // Same chain matches again; only the bound parameter changes. The single render request
        // at the chain's top must still propagate the new parameter into the rendered content.
        nav.NavigateTo("http://localhost/l1/2/l2/l3");

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("top:2", cut.Find("[data-testid=top]").TextContent);
            Assert.IsNotNull(cut.Find("[data-testid=leaf]"));
        });
    }
}
