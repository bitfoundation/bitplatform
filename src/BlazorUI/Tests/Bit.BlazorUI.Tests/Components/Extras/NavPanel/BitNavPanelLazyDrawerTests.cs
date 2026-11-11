using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.NavPanel;

/// <summary>
/// The drawer of a small screen is off the screen until it is opened, so an interactive render leaves its content out
/// until it first opens and keeps it from then on, while a prerender always sends it (see BitNavPanel.NoLazyDrawer).
/// </summary>
[TestClass]
public class BitNavPanelLazyDrawerTests : BunitTestContext
{
    private const string IsDrawerIdentifier = "BitBlazorUI.NavPanel.isDrawer";

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
        Assert.AreEqual(1, component.FindAll(".bit-npn").Count);
        Assert.AreEqual(1, component.FindAll(".bit-npn-swp").Count);
        Assert.AreEqual(0, component.FindAll(".bit-npn-cnt").Count);
        Assert.AreEqual(0, component.FindComponents<BitNav<BitNavItem>>().Count);
        Assert.AreEqual(0, component.FindComponents<BitSearchBox>().Count);
        Assert.AreEqual(0, component.FindComponents<BitSwipeTrap>().Count);
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentOfTheDrawerAsItFirstOpensAndKeepIt()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false);

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual(1, component.FindComponents<BitSwipeTrap>().Count);
        Assert.AreEqual(1, component.FindAll("a[href='/home']").Count);
        Assert.AreEqual(1, component.FindAll("a[href='/docs']").Count);

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

        Assert.AreEqual(1, component.FindComponents<BitNav<BitNavItem>>().Count);
    }

    [TestMethod]
    public void BitNavPanelNoLazyDrawerShouldRenderTheContentOfAClosedDrawer()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false, noLazyDrawer: true);

        Assert.AreEqual(1, component.FindComponents<BitNav<BitNavItem>>().Count);
        Assert.AreEqual(1, component.FindComponents<BitSearchBox>().Count);
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

        Assert.AreEqual(1, component.FindComponents<BitNav<BitNavItem>>().Count);
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentOfAClosedDrawerOnAStaticRender()
    {
        // What a prerender or a static SSR page sends is what a search engine reads.
        Context.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var component = RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false);

        Assert.AreEqual(1, component.FindComponents<BitNav<BitNavItem>>().Count);
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentOfAColumn()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Never, isOpen: false);

        Assert.AreEqual(1, component.FindComponents<BitNav<BitNavItem>>().Count);
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentUntilTheScreenIsKnownToBeADrawers()
    {
        // Nothing answered before the first render (Server, Hybrid, a lazy script), so a column is never drawn empty while
        // the media query has not reported; a drawer that rendered it keeps it.
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var component = RenderPanel(BitNavPanelBreakpoint.Md, isOpen: false);

        Assert.AreEqual(1, component.FindComponents<BitNav<BitNavItem>>().Count);

        SetDrawerScreen(component, isDrawer: true);

        Assert.AreEqual(1, component.FindComponents<BitNav<BitNavItem>>().Count);
    }

    [TestMethod]
    public void BitNavPanelShouldLeaveTheContentOutWhenTheScreenIsADrawersBeforeTheFirstRender()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        Context.JSInterop.Setup<bool>(IsDrawerIdentifier, _ => true).SetResult(true);

        var component = RenderPanel(BitNavPanelBreakpoint.Md, isOpen: false);

        Assert.AreEqual(0, component.FindComponents<BitNav<BitNavItem>>().Count);

        // Asked once, with the breakpoint the panel turns into a drawer below.
        var invocation = Context.JSInterop.VerifyInvoke(IsDrawerIdentifier);
        Assert.AreEqual("md", invocation.Arguments[0]);

        SetDrawerScreen(component, isDrawer: true);

        Assert.AreEqual(0, component.FindComponents<BitNav<BitNavItem>>().Count);

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual(1, component.FindComponents<BitNav<BitNavItem>>().Count);
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheContentOnceAScreenAskedAsADrawersIsReportedAsAColumn()
    {
        // The answer before the first render is a hint: the media query has the last word.
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        Context.JSInterop.Setup<bool>(IsDrawerIdentifier, _ => true).SetResult(true);

        var component = RenderPanel(BitNavPanelBreakpoint.Lg, isOpen: false);

        Assert.AreEqual(0, component.FindComponents<BitNav<BitNavItem>>().Count);

        SetDrawerScreen(component, isDrawer: false);

        Assert.AreEqual(1, component.FindComponents<BitNav<BitNavItem>>().Count);
    }

    [TestMethod]
    public void BitNavPanelShouldAskWithTheBreakpointOfAnEnclosingThemeProvider()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        Context.JSInterop.Setup<bool>(IsDrawerIdentifier, _ => true).SetResult(true);

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

        var invocation = Context.JSInterop.VerifyInvoke(IsDrawerIdentifier);
        Assert.AreEqual("sm", invocation.Arguments[0]);
        Assert.AreEqual("500px", invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitNavPanelShouldNotAskBeforeTheFirstRenderWhenItsShapeIsKnown()
    {
        Context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        RenderPanel(BitNavPanelBreakpoint.Always, isOpen: false);
        RenderPanel(BitNavPanelBreakpoint.Never, isOpen: false);
        RenderPanel(BitNavPanelBreakpoint.Md, isOpen: false, noLazyDrawer: true);

        Context.JSInterop.VerifyNotInvoke(IsDrawerIdentifier);
    }

    private IRenderedComponent<BitNavPanel<BitNavItem>> RenderPanel(BitNavPanelBreakpoint breakpoint, bool isOpen, bool noLazyDrawer = false)
    {
        return RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.DrawerBreakpoint, breakpoint);
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.NoLazyDrawer, noLazyDrawer);
        });
    }

    // Drives the media query the panel reads its own shape from, which is the browser's answer in an app and has none of
    // its own in a test.
    private static void SetDrawerScreen(IRenderedComponent<BitNavPanel<BitNavItem>> component, bool isDrawer)
    {
        var mediaQuery = component.FindComponent<BitMediaQuery>();

        component.InvokeAsync(() => mediaQuery.Instance._OnMatchChange(isDrawer).AsTask());
    }
}
