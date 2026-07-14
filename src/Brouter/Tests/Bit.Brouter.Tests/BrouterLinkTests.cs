using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class BrouterLinkTests : BunitTestContext
{
    [TestMethod]
    public void Prefix_match_activates_on_exact_path()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users");

        var cut = RenderComponent<LinkHost>(p => p.Add(x => x.Href, "/users"));

        cut.WaitForAssertion(() =>
        {
            var anchor = cut.Find("[data-testid=link]");
            StringAssert.Contains(anchor.GetAttribute("class") ?? "", "active");
            Assert.AreEqual("page", anchor.GetAttribute("aria-current"));
        });
    }

    [TestMethod]
    public void Prefix_match_activates_on_child_path()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users/42");

        var cut = RenderComponent<LinkHost>(p => p.Add(x => x.Href, "/users"));

        cut.WaitForAssertion(() =>
        {
            var anchor = cut.Find("[data-testid=link]");
            StringAssert.Contains(anchor.GetAttribute("class") ?? "", "active");
        });
    }

    [TestMethod]
    public void Prefix_match_does_not_activate_on_partial_segment()
    {
        // "/user" is a textual prefix of "/users" but a different segment, so the link
        // must NOT be considered active. Guards the boundary check in UpdateActiveState.
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users");

        var cut = RenderComponent<LinkHost>(p => p.Add(x => x.Href, "/user"));

        cut.WaitForAssertion(() =>
        {
            var anchor = cut.Find("[data-testid=link]");
            var cls = anchor.GetAttribute("class") ?? "";
            Assert.IsFalse(cls.Contains("active"), $"expected not active, got class='{cls}'");
            Assert.IsNull(anchor.GetAttribute("aria-current"));
        });
    }

    [TestMethod]
    public void Prefix_match_on_root_href_does_not_activate_on_other_pages()
    {
        // The classic NavLink footgun: a "home" link (Href="/") under the default Prefix match
        // must NOT be active on every page just because every path starts with '/'. The root is
        // matched exactly even under Prefix (mirrors React Router's NavLink).
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users");

        var cut = RenderComponent<LinkHost>(p => p.Add(x => x.Href, "/"));

        cut.WaitForAssertion(() =>
        {
            var anchor = cut.Find("[data-testid=link]");
            var cls = anchor.GetAttribute("class") ?? "";
            Assert.IsFalse(cls.Contains("active"), $"expected not active, got class='{cls}'");
            Assert.IsNull(anchor.GetAttribute("aria-current"));
        });
    }

    [TestMethod]
    public void Prefix_match_on_root_href_activates_at_root()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/");

        var cut = RenderComponent<LinkHost>(p => p.Add(x => x.Href, "/"));

        cut.WaitForAssertion(() =>
        {
            var anchor = cut.Find("[data-testid=link]");
            StringAssert.Contains(anchor.GetAttribute("class") ?? "", "active");
            Assert.AreEqual("page", anchor.GetAttribute("aria-current"));
        });
    }

    [TestMethod]
    public void All_match_only_activates_on_exact_equality()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users/42");

        var cut = RenderComponent<LinkHost>(p => p
            .Add(x => x.Href, "/users")
            .Add(x => x.Match, BrouterLinkMatch.All));

        // /users/42 is a child of /users; with Match=All the link must not be active.
        cut.WaitForAssertion(() =>
        {
            var cls = cut.Find("[data-testid=link]").GetAttribute("class") ?? "";
            Assert.IsFalse(cls.Contains("active"), $"expected not active, got class='{cls}'");
        });

        // Now navigate to the exact path; the link should activate.
        nav.NavigateTo("http://localhost/users");

        cut.WaitForAssertion(() =>
        {
            var cls = cut.Find("[data-testid=link]").GetAttribute("class") ?? "";
            StringAssert.Contains(cls, "active");
        });
    }

    [TestMethod]
    public void Trailing_slash_on_href_is_normalized_to_match_path()
    {
        // Default options: IgnoreTrailingSlash=true. "/users/" href should be normalized
        // to "/users" inside NormalisePath, so an exact-equality (All) match against
        // current path "/users" succeeds.
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users");

        var cut = RenderComponent<LinkHost>(p => p
            .Add(x => x.Href, "/users/")
            .Add(x => x.Match, BrouterLinkMatch.All));

        cut.WaitForAssertion(() =>
        {
            var cls = cut.Find("[data-testid=link]").GetAttribute("class") ?? "";
            StringAssert.Contains(cls, "active");
        });
    }

    [TestMethod]
    public void Replace_navigates_with_replace_true_on_plain_left_click()
    {
        // Wire the JS module so OnAfterRenderAsync flips _replaceWired to true; otherwise
        // OnClick short-circuits and the C#-side replace navigation never runs.
        SetupReplaceJsModule();

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/start");

        var cut = RenderComponent<LinkHost>(p => p
            .Add(x => x.Href, "/users")
            .Add(x => x.Replace, true));

        // Wait until OnAfterRenderAsync has invoked wireConditionalPreventDefault, so
        // _replaceWired is true and the next click will actually drive Brouter.Navigate.
        WaitForReplaceWiring(cut);

        // Capture LocationChanged events fired AFTER the click. nav.History tracking varies
        // across bunit versions; LocationChanged is the stable contract NavigationManager
        // commits to and is what BrouterLink ultimately drives via NavigationManager.NavigateTo.
        var changes = new List<LocationChangedEventArgs>();
        nav.LocationChanged += (_, e) => changes.Add(e);

        cut.Find("[data-testid=link]").Click(new MouseEventArgs { Button = 0 });

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(changes.Count > 0, "expected the click to trigger a navigation");
            StringAssert.EndsWith(changes[^1].Location, "/users");
            StringAssert.EndsWith(nav.Uri, "/users");
            // The actual replace flag isn't carried on LocationChangedEventArgs, but History
            // does record it. nav.History is documented and the last entry is ours.
            Assert.IsTrue(nav.History.Last().Options.ReplaceHistoryEntry,
                "expected a replace navigation rather than a push");
        });
    }

    [TestMethod]
    public void Replace_does_not_navigate_on_modified_or_non_primary_clicks()
    {
        SetupReplaceJsModule();

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/start");

        var cut = RenderComponent<LinkHost>(p => p
            .Add(x => x.Href, "/users")
            .Add(x => x.Replace, true));

        // Without waiting for _replaceWired, OnClick early-returns and this test would pass
        // trivially - observing "no navigation happened" wouldn't prove the modifier-key
        // filter at all. Wait for wiring so the filter is the only thing keeping the
        // navigation from firing.
        WaitForReplaceWiring(cut);

        var changes = new List<LocationChangedEventArgs>();
        nav.LocationChanged += (_, e) => changes.Add(e);

        var anchor = cut.Find("[data-testid=link]");

        // Modified primary clicks: browser handles them natively (open in new tab/window).
        anchor.Click(new MouseEventArgs { Button = 0, CtrlKey = true });
        anchor.Click(new MouseEventArgs { Button = 0, ShiftKey = true });
        anchor.Click(new MouseEventArgs { Button = 0, AltKey = true });
        anchor.Click(new MouseEventArgs { Button = 0, MetaKey = true });
        // Non-primary click (middle button) must also fall through.
        anchor.Click(new MouseEventArgs { Button = 1 });

        Assert.AreEqual(0, changes.Count,
            "modified or non-primary clicks must not trigger a replace navigation");
        StringAssert.EndsWith(nav.Uri, "/start");
    }

    [TestMethod]
    public void Multiple_Replace_links_share_a_single_module_import()
    {
        SetupReplaceJsModule();

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/start");

        var cut = RenderComponent<MultiReplaceLinkHost>();

        // Wait for all three links to finish wiring before counting imports, so a link that
        // hasn't imported yet can't make the assertion pass vacuously.
        var jsInvocations = Context!.JSInterop.Invocations;
        cut.WaitForState(() => jsInvocations.Count(i => i.Identifier == "wireConditionalPreventDefault") == 3);

        Assert.AreEqual(1, jsInvocations.Count(i => i.Identifier == "import"),
            "all Replace links should reuse the scope-shared bit-brouter.js module instead of importing per link");
    }

    private void SetupReplaceJsModule()
    {
        var module = Context!.JSInterop.SetupModule("./_content/Bit.Brouter/bit-brouter.js");
        // BrouterLink.OnAfterRenderAsync calls module.InvokeAsync<IJSObjectReference>(
        //   "wireConditionalPreventDefault", _anchor) and stores the returned handle.
        // bunit only allows IJSObjectReference results to be produced via SetupModule, so we
        // model the handle as a nested module setup. The handle's own InvokeVoidAsync("dispose")
        // and DisposeAsync calls become no-op interop matches, which is exactly what we want.
        module.SetupModule("wireConditionalPreventDefault");
    }

    /// <summary>
    /// Wait until OnAfterRenderAsync has actually invoked wireConditionalPreventDefault, which
    /// is what flips _replaceWired to true inside BrouterLink. Without this, click tests race
    /// the async wiring and assert against the wrong state.
    /// </summary>
    private void WaitForReplaceWiring(IRenderedComponentBase<LinkHost> cut)
    {
        var jsInvocations = Context!.JSInterop.Invocations;
        cut.WaitForState(() => jsInvocations.Any(i => i.Identifier == "wireConditionalPreventDefault"));
    }
}
