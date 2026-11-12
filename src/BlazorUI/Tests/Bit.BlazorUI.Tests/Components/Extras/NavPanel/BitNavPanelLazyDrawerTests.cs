using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.NavPanel;

/// <summary>
/// The drawer of a small screen is off the screen until it is opened, so its content is left out until it first opens
/// and kept from then on, while a prerender always sends it (see BitNavPanel.NoLazyDrawer).
/// </summary>
[TestClass]
public class BitNavPanelLazyDrawerTests : BunitTestContext
{
    // What the panel asks the browser before its first render, the way BitMediaQuery does (BitMediaQuery.MatchNow).
    private const string MatchNowIdentifier = "BitBlazorUI.MediaQuery.matchNow";

    private static readonly IList<BitNavItem> Items =
    [
        new() { Text = "Home", Url = "/home" },
        new() { Text = "Docs", Url = "/docs" }
    ];

    [TestMethod]
    public void BitNavPanelShouldLeaveTheContentOfAClosedDrawerOut()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false);

        // The root is there for the drawer to slide in from, and the content is not.
        Assert.HasCount(1, component.FindAll(".bit-npn"));
        Assert.HasCount(1, component.FindAll(".bit-npn-swp"));
        Assert.IsEmpty(component.FindAll(".bit-npn-cnt"));
        Assert.IsEmpty(component.FindComponents<BitNav<BitNavItem>>());
        Assert.IsEmpty(component.FindComponents<BitSearchBox>());
        Assert.IsEmpty(component.FindComponents<BitSwipeTrap>());
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentOfTheDrawerAsItFirstOpensAndKeepIt()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false);

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.HasCount(1, component.FindComponents<BitSwipeTrap>());
        Assert.HasCount(1, component.FindAll("a[href='/home']"));
        Assert.HasCount(1, component.FindAll("a[href='/docs']"));

        var nav = component.FindComponent<BitNav<BitNavItem>>().Instance;

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));

        // The same nav, with whatever state it holds, rather than one built again on the next open.
        Assert.AreSame(nav, component.FindComponent<BitNav<BitNavItem>>().Instance);
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentOfAnOpenDrawerAtOnce()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: true);

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public void BitNavPanelNoLazyDrawerShouldRenderTheContentOfAClosedDrawer()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false, noLazyDrawer: true);

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
        Assert.HasCount(1, component.FindComponents<BitSearchBox>());
    }

    [TestMethod]
    public void BitNavPanelNoLazyDrawerShouldBeTakenFromTheCascade()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitNavPanelParams { NoLazyDrawer = true } });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitNavPanel<BitNavItem>>(0);
                builder.AddAttribute(1, nameof(BitNavPanel<BitNavItem>.Items), Items);
                builder.AddAttribute(2, nameof(BitNavPanel<BitNavItem>.DrawerBreakpoint), (BitNavPanelBreakpoint?)BitNavPanelBreakpoint.Always);
                builder.CloseComponent();
            }));
        });

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentOfAClosedDrawerOnAStaticRender()
    {
        // What a prerender or a static SSR page sends is what a search engine reads.
        Context.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false);

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentOfAColumn()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Never, isOpen: false);

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public async Task BitNavPanelShouldRenderTheContentUntilTheScreenIsKnownToBeADrawers()
    {
        // Nothing answered before the first render (Server, Hybrid, a lazy script), so a column is never drawn empty while
        // the media query has not reported; a drawer that rendered it keeps it.
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Md, isOpen: false);

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());

        await SetDrawerScreen(component, isDrawer: true);

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public async Task BitNavPanelShouldLeaveTheContentOutWhenTheScreenIsADrawersBeforeTheFirstRender()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        Context.JSInterop.Setup<bool?>(MatchNowIdentifier, _ => true).SetResult(true);

        var component = RenderPanel(BitNavPanelBreakpoint.Md, isOpen: false);

        Assert.IsEmpty(component.FindComponents<BitNav<BitNavItem>>());

        // Asked of the query the panel turns into a drawer below: off the breakpoints the stylesheet lays it out at
        // until the media query reports, then off the live ones the media query is built from.
        var invocations = Context.JSInterop.VerifyInvoke(MatchNowIdentifier, calledTimes: 2);
        Assert.IsTrue(invocations.All(i => i.Arguments[0] is null && Equals(i.Arguments[1], "LtMd")));
        Assert.AreEqual("960px", ((IDictionary<string, string>)invocations[0].Arguments[2]!)["md"]);
        Assert.IsNull(invocations[1].Arguments[2]);

        await SetDrawerScreen(component, isDrawer: true);

        Assert.IsEmpty(component.FindComponents<BitNav<BitNavItem>>());

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public async Task BitNavPanelShouldRenderTheContentOnceAScreenAskedAsADrawersIsReportedAsAColumn()
    {
        // The answer before the first render is a hint: the media query has the last word.
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        Context.JSInterop.Setup<bool?>(MatchNowIdentifier, _ => true).SetResult(true);

        var component = RenderPanel(BitNavPanelBreakpoint.Lg, isOpen: false);

        Assert.IsEmpty(component.FindComponents<BitNav<BitNavItem>>());

        await SetDrawerScreen(component, isDrawer: false);

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentWhereTheStylesheetDrawsAColumnBeforeTheFirstRender()
    {
        // A theme that moves the breakpoint up makes a drawer of a screen the stylesheet still lays out as a column at the
        // default breakpoint until the media query reports: that column is never drawn empty.
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        Context.JSInterop.Setup<bool?>(MatchNowIdentifier, i => IsStylesheetQuery(i)).SetResult(false);
        Context.JSInterop.Setup<bool?>(MatchNowIdentifier, i => IsStylesheetQuery(i) is false).SetResult(true);

        var component = RenderPanel(BitNavPanelBreakpoint.Md, isOpen: false);

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());

        // Nothing of the page is read to find that out.
        Context.JSInterop.VerifyInvoke(MatchNowIdentifier, calledTimes: 1);
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentWhenTheBrowserFailsBeforeTheFirstRender()
    {
        // Any failure of the hint (a runtime not ready yet, not only a JSException) leaves the content rendered.
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        Context.JSInterop.Setup<bool?>(MatchNowIdentifier, _ => true).SetException(new InvalidOperationException("not ready"));

        var component = RenderPanel(BitNavPanelBreakpoint.Md, isOpen: false);

        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public void BitNavPanelShouldAskWithTheBreakpointOfAnEnclosingThemeProvider()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        Context.JSInterop.Setup<bool?>(MatchNowIdentifier, _ => true).SetResult(true);

        var theme = new BitTheme();
        theme.Layout.Breakpoints.Sm = "500px";

        RenderComponent<CascadingValue<BitTheme>>(parameters =>
        {
            parameters.Add(p => p.Value, theme);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitNavPanel<BitNavItem>>(0);
                builder.AddAttribute(1, nameof(BitNavPanel<BitNavItem>.Items), Items);
                builder.AddAttribute(2, nameof(BitNavPanel<BitNavItem>.DrawerBreakpoint), (BitNavPanelBreakpoint?)BitNavPanelBreakpoint.Sm);
                builder.CloseComponent();
            }));
        });

        var invocations = Context.JSInterop.VerifyInvoke(MatchNowIdentifier, calledTimes: 2);
        Assert.AreEqual("LtSm", invocations[1].Arguments[1]);
        Assert.AreEqual("500px", ((IDictionary<string, string>)invocations[1].Arguments[2]!)["sm"]);
    }

    [TestMethod]
    public void BitNavPanelShouldNotAskBeforeTheFirstRenderWhenItsShapeIsKnown()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false);
        RenderPanel(BitNavPanelBreakpoint.Never, isOpen: false);
        RenderPanel(BitNavPanelBreakpoint.Md, isOpen: false, noLazyDrawer: true);

        Context.JSInterop.VerifyNotInvoke(MatchNowIdentifier);
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentWhileTheAppListensToTheSelectionOfTheAutomaticMode()
    {
        // The automatic mode reports the selection it reads off the URL as the nav first renders, which a binding hears as
        // the app starts rather than in the middle of the first opening of the drawer.
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var bound = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.DrawerBreakpoint, BitNavPanelBreakpoint.Always);
            parameters.Add(p => p.SelectedItemChanged, (BitNavItem? _) => { });
        });

        var listened = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.DrawerBreakpoint, BitNavPanelBreakpoint.Always);
            parameters.Add(p => p.OnSelectItem, (BitNavItem _) => { });
        });

        // The manual mode selects nothing on its own, so the drawer stays lazy there.
        var manual = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.DrawerBreakpoint, BitNavPanelBreakpoint.Always);
            parameters.Add(p => p.NavMode, BitNavMode.Manual);
            parameters.Add(p => p.SelectedItemChanged, (BitNavItem? _) => { });
        });

        Assert.HasCount(1, bound.FindComponents<BitNav<BitNavItem>>());
        Assert.HasCount(1, listened.FindComponents<BitNav<BitNavItem>>());
        Assert.IsEmpty(manual.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public async Task BitNavPanelSelectItemShouldReachTheNavOfADrawerNeverOpened()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var items = NewItems();
        BitNavItem? selected = null;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DrawerBreakpoint, BitNavPanelBreakpoint.Always);
            parameters.Add(p => p.NavMode, BitNavMode.Manual);
            parameters.Add(p => p.SelectedItemChanged, (BitNavItem? item) => selected = item);
        });

        Assert.IsEmpty(component.FindComponents<BitNav<BitNavItem>>());

        await component.InvokeAsync(() => component.Instance.SelectItem(items[1]));

        Assert.AreSame(items[1], selected);
        Assert.AreSame(items[1], component.FindComponent<BitNav<BitNavItem>>().Instance.SelectedItem);
    }

    [TestMethod]
    public async Task BitNavPanelExpandItemShouldReachTheNavOfADrawerNeverOpened()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var items = NewItems();
        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false, items: items);

        Assert.IsFalse(await component.InvokeAsync(() => component.Instance.IsItemExpanded(items[0])));

        await component.InvokeAsync(() => component.Instance.ExpandItem(items[0]));

        Assert.IsTrue(await component.InvokeAsync(() => component.Instance.IsItemExpanded(items[0])));
    }

    [TestMethod]
    public async Task BitNavPanelIsItemExpandedShouldAnswerForTheNavOfADrawerNeverOpened()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var items = NewItems();
        items[0].IsExpanded = true;

        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false, items: items);

        Assert.IsEmpty(component.FindComponents<BitNav<BitNavItem>>());

        // Rendered by the call itself, which then answers for it.
        Assert.IsTrue(await component.InvokeAsync(() => component.Instance.IsItemExpanded(items[0])));
        Assert.HasCount(1, component.FindComponents<BitNav<BitNavItem>>());
    }

    [TestMethod]
    public async Task BitNavPanelExpandAllShouldReachTheNavOfADrawerNeverOpened()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var items = NewItems();
        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false, items: items);

        await component.InvokeAsync(() => component.Instance.ExpandAll());

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.IsItemExpanded(items[0])));
    }

    [TestMethod]
    public async Task BitNavPanelShouldWaitForTheNavWhenAskedForItDuringARender()
    {
        // A call made while the panel cannot render at once (from an after-render callback of the same batch) waits for
        // the render that brings the nav in, rather than finding none.
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var items = NewItems();
        Task? expanding = null;

        var component = RenderComponent<NavPanelCaller>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnFirstRender, panel => expanding = panel.ExpandItem(items[0]));
        });

        await component.InvokeAsync(() => expanding!);

        var panel = component.FindComponent<BitNavPanel<BitNavItem>>();

        Assert.IsTrue(await component.InvokeAsync(() => panel.Instance.IsItemExpanded(items[0])));
    }

    private static BitNavItem[] NewItems() =>
    [
        new() { Text = "Guides", ChildItems = [new() { Text = "Start", Url = "/start" }] },
        new() { Text = "Docs", Url = "/docs" }
    ];

    // A page holding a closed drawer and calling into it from its own first after-render callback, while the batch the
    // panel rendered in is still being processed.
    private sealed class NavPanelCaller : ComponentBase
    {
        private BitNavPanel<BitNavItem>? _panel;

        [Parameter] public IList<BitNavItem> Items { get; set; } = default!;

        [Parameter] public Action<BitNavPanel<BitNavItem>>? OnFirstRender { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitNavPanel<BitNavItem>>(0);
            builder.AddAttribute(1, nameof(BitNavPanel<BitNavItem>.Items), Items);
            builder.AddAttribute(2, nameof(BitNavPanel<BitNavItem>.DrawerBreakpoint), (BitNavPanelBreakpoint?)BitNavPanelBreakpoint.Always);
            builder.AddComponentReferenceCapture(3, panel => _panel = (BitNavPanel<BitNavItem>)panel);
            builder.CloseComponent();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender) OnFirstRender?.Invoke(_panel!);
        }
    }

    private static bool IsStylesheetQuery(JSRuntimeInvocation invocation)
        => invocation.Arguments[2] is IDictionary<string, string> breakpoints && breakpoints.TryGetValue("md", out var md) && md == "960px";

    private IRenderedComponent<BitNavPanel<BitNavItem>> RenderPanel(BitNavPanelBreakpoint breakpoint, bool isOpen, bool noLazyDrawer = false, IList<BitNavItem>? items = null)
    {
        return RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items ?? Items);
            parameters.Add(p => p.DrawerBreakpoint, breakpoint);
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.NoLazyDrawer, noLazyDrawer);
        });
    }

    // Drives the media query the panel reads its own shape from, which is the browser's answer in an app and has none of
    // its own in a test.
    private static Task SetDrawerScreen(IRenderedComponent<BitNavPanel<BitNavItem>> component, bool isDrawer)
    {
        var mediaQuery = component.FindComponent<BitMediaQuery>();

        return component.InvokeAsync(() => mediaQuery.Instance._OnMatchChange(isDrawer).AsTask());
    }
}
