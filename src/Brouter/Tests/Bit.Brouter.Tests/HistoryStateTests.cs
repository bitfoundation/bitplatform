using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class HistoryStateTests : BunitTestContext
{
    [TestMethod]
    public void Navigate_without_state_exposes_null_HistoryState()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<HistoryStateHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.IsNull(brouter.Location.HistoryState);
            Assert.IsTrue(cut.Instance.HookFired);
            Assert.IsNull(cut.Instance.LastHookState);
        });
    }

    [TestMethod]
    public void Navigate_with_state_exposes_it_on_Location_and_hooks()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<HistoryStateHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b", historyState: "payload-42"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.AreEqual("payload-42", brouter.Location.HistoryState);
            Assert.AreEqual("payload-42", cut.Instance.LastHookState);
        });
    }

    [TestMethod]
    public void Navigate_with_state_and_replace_replaces_the_entry_and_keeps_the_state()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<HistoryStateHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b", replace: true, historyState: "replaced-state"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.AreEqual("replaced-state", brouter.Location.HistoryState);
        });

        // The BunitNavigationManager records the NavigationOptions it was invoked with; verify the
        // replace flag survived the options-based NavigateTo overload used when state is present.
        var last = nav.History.Last();
        Assert.IsTrue(last.Options.ReplaceHistoryEntry);
        Assert.AreEqual("replaced-state", last.Options.HistoryEntryState);
    }

    [TestMethod]
    public void Link_click_with_HistoryState_attaches_the_state()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<HistoryStateHost>(p => p.Add(x => x.LinkState, "from-link"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        // The state-carrying link intercepts clicks the same way Replace links do; the JS wiring
        // happens in OnAfterRenderAsync against the (loose-mode) module mock. Click once wired.
        var brouter = Services.GetRequiredService<IBrouter>();
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid=state-link]").Click();
            Assert.AreEqual("from-link", brouter.Location.HistoryState);
        });

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.AreEqual("from-link", cut.Instance.LastHookState);
        });
    }

    [TestMethod]
    public void NavigateToName_forwards_history_state()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<NamedRouteHost>(p => p
            .Add(x => x.Name, "user")
            .Add(x => x.Path, "/users/{id:int}"));
        var brouter = Services.GetRequiredService<IBrouter>();

        cut.InvokeAsync(() => brouter.NavigateToName(
            "user",
            new Dictionary<string, object?> { ["id"] = 42 },
            historyState: "named-state"));

        cut.WaitForAssertion(() => Assert.AreEqual("named-state", brouter.Location.HistoryState));
    }
}
