using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// Pins the single-batch initial mount (see <c>BrouterInitializer</c>): the first render batch of
/// the mount must already contain the matched route's content. The old two-batch mount (an empty
/// unmatched router in batch one, the page in batch two) is what caused a visible flash of blank
/// content when the interactive pass replaced prerendered HTML. The <c>InitialBatchProbe</c>
/// records the observable property directly: at the first dispatcher yield after the mount began
/// - the moment the first batch would be painted - was the routed content already there?
/// Also covers the late-registration safety net for route trees the boot sentinel cannot fully
/// observe (deep wrapper chains, conditionally-rendered routes).
/// </summary>
[TestClass]
public class InitialMountTests : BunitTestContext
{
    [TestInitialize]
    public void ResetState() => InitialMountState.Reset();

    private IRenderedComponent<THost> RenderHostAt<THost>(string url)
        where THost : Microsoft.AspNetCore.Components.IComponent
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo(url);
        return RenderComponent<THost>();
    }

    [TestMethod]
    public void Initial_mount_reveals_flat_route_in_the_first_render_batch()
    {
        var cut = RenderHostAt<InitialMountHost>("http://localhost/home");

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=im-content]")));
        // The probe recorded true only if the content existed BEFORE the first dispatcher yield,
        // i.e. inside the mount's first render batch - the no-blank-flash guarantee.
        cut.WaitForAssertion(() => Assert.AreEqual(true, InitialMountState.ContentPresentAtFirstYield,
            "the matched route must render within the initial render batch, not a later one"));
    }

    [TestMethod]
    public void Initial_mount_reveals_nested_route_chain_in_the_first_render_batch()
    {
        var cut = RenderHostAt<InitialMountNestedHost>("http://localhost/docs/guide/intro");

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=im-content]")));
        cut.WaitForAssertion(() => Assert.AreEqual(true, InitialMountState.ContentPresentAtFirstYield,
            "nested registration cascades one render wave per level; the boot sentinel must bridge " +
            "all of them without leaving the first batch"));
    }

    [TestMethod]
    public void Initial_mount_reveals_route_behind_a_wrapper_component_in_the_first_render_batch()
    {
        var cut = RenderHostAt<InitialMountWrappedHost>("http://localhost/home");

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=im-content]")));
        cut.WaitForAssertion(() => Assert.AreEqual(true, InitialMountState.ContentPresentAtFirstYield,
            "a wrapper component adds a registration-silent render wave the sentinel must tolerate"));
    }

    [TestMethod]
    public void Routes_behind_a_wrapper_chain_deeper_than_the_sentinel_tolerance_still_render()
    {
        // Five stacked wrappers out-wait the sentinel's quiet rounds, so the initial navigation
        // fires before these routes registered. The late-registration rematch must then correct
        // the outcome - the route may render a batch late here, but never not at all.
        var cut = RenderHostAt<InitialMountDeepWrappedHost>("http://localhost/home");

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=im-content]")));
    }

    [TestMethod]
    public void Route_registered_after_mount_at_its_own_url_renders_without_a_navigation()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/late");

        // Mount at /late while the /late route does not exist yet: nothing matches.
        var cut = RenderComponent<InitialMountLateRouteHost>(p => p.Add(h => h.ShowLate, false));
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=im-late]").Count));

        // Reveal the route. Its registration must trigger the winner re-evaluation and render it
        // in place - no navigate-away-and-back required.
        cut.Render(p => p.Add(h => h.ShowLate, true));

        cut.WaitForAssertion(() => Assert.AreEqual("late", cut.Find("[data-testid=im-late]").TextContent));
    }

    [TestMethod]
    public void Late_registration_that_does_not_change_the_winner_leaves_the_committed_route_alone()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/always");

        var cut = RenderComponent<InitialMountLateRouteHost>(p => p.Add(h => h.ShowLate, false));
        cut.WaitForAssertion(() => Assert.AreEqual("always", cut.Find("[data-testid=im-always]").TextContent));

        // Registering an unrelated route must resolve as a no-op: /always stays rendered.
        cut.Render(p => p.Add(h => h.ShowLate, true));

        cut.WaitForAssertion(() => Assert.AreEqual("always", cut.Find("[data-testid=im-always]").TextContent));
        Assert.AreEqual(0, cut.FindAll("[data-testid=im-late]").Count);
    }
}
