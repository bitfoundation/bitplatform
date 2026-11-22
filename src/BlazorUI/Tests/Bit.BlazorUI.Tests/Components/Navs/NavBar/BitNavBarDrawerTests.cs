using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.NavBar;

/// <summary>
/// Covers the drawer a DrawerBreakpoint folds the items of a navbar into: the toggle button that opens it, the shape
/// the browser reports, the modal surface the open drawer is, and everything that closes it.
/// </summary>
[TestClass]
public class BitNavBarDrawerTests : BunitTestContext
{
    private static List<BitNavBarItem> Items() =>
    [
        new() { Text = "Home", IconName = "Home" },
        new() { Text = "Products", IconName = "ProductVariant" },
        new() { Text = "Profile", IconName = "Contact" },
    ];

    private static List<BitNavBarItem> UrlItems() =>
    [
        new() { Text = "Home", Url = "/home" },
        new() { Text = "Products", Url = "/products" },
    ];

    private IRenderedComponent<BitNavBar<BitNavBarItem>> RenderNavBar(
        Action<ComponentParameterCollectionBuilder<BitNavBar<BitNavBarItem>>>? parameters = null,
        IList<BitNavBarItem>? items = null)
    {
        return RenderComponent<BitNavBar<BitNavBarItem>>(p =>
        {
            p.Add(c => c.Items, items ?? Items());
            p.Add(c => c.DrawerBreakpoint, BitScreenQuery.LtMd);
            parameters?.Invoke(p);
        });
    }

    // Drives the report of the media query the navbar reads its own shape from, which is the browser's answer in an app
    // and has none of its own in a test.
    private static void SetDrawerScreen(IRenderedComponent<BitNavBar<BitNavBarItem>> component, bool isDrawer)
    {
        component.InvokeAsync(() => component.Instance._OnMatchChange(isDrawer)).GetAwaiter().GetResult();
    }

    private IRenderedComponent<BitNavBar<BitNavBarItem>> RenderOpenDrawer(Action<ComponentParameterCollectionBuilder<BitNavBar<BitNavBarItem>>>? parameters = null,
                                                                          IList<BitNavBarItem>? items = null)
    {
        var component = RenderNavBar(parameters, items);

        SetDrawerScreen(component, true);

        component.Find(".bit-nbr-tgl").Click();

        return component;
    }

    private int Invocations(string identifier) => Context.JSInterop.Invocations.Count(i => i.Identifier == identifier);



    [TestMethod]
    public void BitNavBarWithoutDrawerBreakpointShouldRenderNoDrawer()
    {
        var component = RenderComponent<BitNavBar<BitNavBarItem>>(p => p.Add(c => c.Items, Items()));

        Assert.AreEqual(0, component.FindAll(".bit-nbr-tgl").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-drw").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-ovl").Count);
        Assert.AreEqual(0, Invocations("BitBlazorUI.MediaQuery.setup"));

        // The list stays a child of the root, where every rule of the bar finds it.
        Assert.AreEqual("bit-nbr", component.Find(".bit-nbr-cnt").ParentElement!.ClassList[0]);
        Assert.IsFalse(Regex.IsMatch(component.Find(".bit-nbr").ClassName, @"bit-nbr-(dq-|dsh|dop)"));
    }

    [TestMethod]
    public void BitNavBarShouldRenderTheToggleAndTheDrawerAroundTheSameItems()
    {
        var component = RenderNavBar();

        var toggle = component.Find(".bit-nbr-tgl");
        var drawer = component.Find(".bit-nbr-drw");

        Assert.AreEqual("button", toggle.TagName.ToLowerInvariant());
        Assert.AreEqual("button", toggle.GetAttribute("type"));
        Assert.AreEqual("false", toggle.GetAttribute("aria-expanded"));
        Assert.AreEqual(drawer.Id, toggle.GetAttribute("aria-controls"));
        Assert.IsFalse(string.IsNullOrEmpty(drawer.Id));

        // The drawer holds the very list of the bar, so the items are rendered once.
        Assert.AreEqual(drawer, component.Find(".bit-nbr-cnt").ParentElement);
        Assert.AreEqual(3, component.FindAll(".bit-nbr-itm").Count);

        // The toggle comes right before the drawer it controls.
        Assert.AreEqual(drawer, toggle.NextElementSibling);

        // A closed drawer is no dialog, and nothing of it is modal.
        Assert.IsNull(drawer.GetAttribute("role"));
        Assert.IsNull(drawer.GetAttribute("aria-modal"));
        Assert.IsNull(drawer.GetAttribute("tabindex"));
        Assert.AreEqual(0, component.FindAll(".bit-nbr-ovl").Count);
    }

    [TestMethod]
    public void BitNavBarShouldNameTheToggleAndDrawWhatItIsGiven()
    {
        var component = RenderNavBar();

        var toggle = component.Find(".bit-nbr-tgl");

        // Named by the strings while nothing else names it, and drawn by the navbar itself.
        Assert.AreEqual(BitStrings.Current.Menu, toggle.GetAttribute("aria-label"));
        Assert.AreEqual(BitStrings.Current.Menu, toggle.GetAttribute("title"));
        Assert.AreEqual(1, toggle.QuerySelectorAll(".bit-nbr-tgh").Length);
        Assert.AreEqual(0, toggle.QuerySelectorAll(".bit-nbr-tgi").Length);

        component.Render(p =>
        {
            p.Add(c => c.ToggleAriaLabel, "Site navigation");
            p.Add(c => c.ToggleIconName, "GlobalNavButton");
        });

        toggle = component.Find(".bit-nbr-tgl");

        Assert.AreEqual("Site navigation", toggle.GetAttribute("aria-label"));
        Assert.AreEqual(0, toggle.QuerySelectorAll(".bit-nbr-tgh").Length);
        Assert.IsTrue(toggle.QuerySelector(".bit-nbr-tgi")!.ClassList.Contains("bit-icon--GlobalNavButton"));

        // An external icon wins over the name.
        component.Render(p => p.Add(c => c.ToggleIcon, BitIconInfo.Css("fa-solid fa-bars")));

        var icon = component.Find(".bit-nbr-tgi");

        Assert.IsTrue(icon.ClassList.Contains("fa-bars"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--GlobalNavButton"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitNavBarShouldTakeItsShapeFromTheStylesheetUntilTheBrowserReportsIt()
    {
        var component = RenderComponent<BitNavBar<BitNavBarItem>>(p =>
        {
            p.Add(c => c.Items, Items());
            p.Add(c => c.DrawerBreakpoint, BitScreenQuery.SmToMd);
        });

        var root = component.Find(".bit-nbr");

        // The query itself is the class until the browser answers, which the stylesheet resolves at the default
        // breakpoints - what a prerendered page is laid out by.
        Assert.IsTrue(root.ClassList.Contains("bit-nbr-dq-smtomd"));
        Assert.IsFalse(root.ClassList.Contains("bit-nbr-dsh"));

        SetDrawerScreen(component, true);

        root = component.Find(".bit-nbr");
        Assert.IsTrue(root.ClassList.Contains("bit-nbr-dsh"));
        Assert.IsFalse(root.ClassList.Contains("bit-nbr-dq-smtomd"));

        SetDrawerScreen(component, false);

        root = component.Find(".bit-nbr");
        Assert.IsFalse(root.ClassList.Contains("bit-nbr-dsh"));
        Assert.IsFalse(root.ClassList.Contains("bit-nbr-dq-smtomd"));

        // A breakpoint changed for another asks the browser of the new one, and keeps the shape it last reported: the
        // listener only reports again when the new query answers otherwise.
        component.Render(p => p.Add(c => c.DrawerBreakpoint, BitScreenQuery.LtLg));

        root = component.Find(".bit-nbr");
        Assert.IsFalse(root.ClassList.Contains("bit-nbr-dq-ltlg"));
        Assert.IsFalse(root.ClassList.Contains("bit-nbr-dsh"));
        Assert.AreEqual("LtLg", Context.JSInterop.Invocations["BitBlazorUI.MediaQuery.setup"].Last().Arguments[3]);

        // One taken away and set afresh falls back on the stylesheet until the browser answers.
        component.Render(p => p.Add(c => c.DrawerBreakpoint, null));

        Assert.AreEqual(1, Invocations("BitBlazorUI.MediaQuery.dispose"));

        component.Render(p => p.Add(c => c.DrawerBreakpoint, BitScreenQuery.LtMd));

        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dq-ltmd"));
    }

    [TestMethod]
    public void BitNavBarOpenDrawerShouldStayModalWhenTheBreakpointChangesForOneTheScreenStillMatches()
    {
        var component = RenderOpenDrawer();

        component.Render(p => p.Add(c => c.DrawerBreakpoint, BitScreenQuery.LtLg));

        // No report comes for a query that answers the same, so the drawer is still the modal surface it was.
        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dsh"));
        Assert.AreEqual("dialog", component.Find(".bit-nbr-drw").GetAttribute("role"));
        Assert.AreEqual("claim", component.Find(".bit-nbr-drw").GetAttribute("data-bit-esc"));
        Assert.AreEqual(0, Invocations("BitBlazorUI.Utils.unlockScroll"));

        component.FindAll(".bit-nbr-itm")[1].Click();

        Assert.IsFalse(component.Instance.DrawerOpen);
    }

    [TestMethod]
    public void BitNavBarDrawerTakenAwayWhileOpenShouldCloseIt()
    {
        var opened = new List<bool>();

        var component = RenderOpenDrawer(p => p.Add(c => c.DrawerOpenChanged, (bool open) => opened.Add(open)));

        component.Render(p => p.Add(c => c.DrawerBreakpoint, null));

        Assert.IsFalse(component.Instance.DrawerOpen);
        CollectionAssert.AreEqual(new[] { true, false }, opened);
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.unlockScroll"));

        // So a breakpoint set again does not show the drawer open on its own.
        component.Render(p => p.Add(c => c.DrawerBreakpoint, BitScreenQuery.LtMd));

        Assert.IsFalse(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dop"));
    }

    [TestMethod]
    public void BitNavBarShouldNotOpenTheDrawerOfAScreenReportedToBeABar()
    {
        var opened = new List<bool>();

        var component = RenderNavBar(p => p.Add(c => c.DrawerOpenChanged, (bool open) => opened.Add(open)));

        SetDrawerScreen(component, false);

        component.Render(p => p.Add(c => c.DrawerOpen, true));

        Assert.IsFalse(component.Instance.DrawerOpen);
        CollectionAssert.AreEqual(new[] { false }, opened);
        Assert.IsFalse(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dop"));
    }

    [TestMethod]
    public void BitNavBarToggleShouldOpenAndCloseTheDrawer()
    {
        var opened = new List<bool>();

        var component = RenderNavBar(p => p.Add(c => c.DrawerOpenChanged, (bool open) => opened.Add(open)));

        SetDrawerScreen(component, true);

        component.Find(".bit-nbr-tgl").Click();

        var toggle = component.Find(".bit-nbr-tgl");
        var drawer = component.Find(".bit-nbr-drw");

        Assert.AreEqual("true", toggle.GetAttribute("aria-expanded"));
        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dop"));
        Assert.IsTrue(component.Instance.DrawerOpen);
        CollectionAssert.AreEqual(new[] { true }, opened);

        // The open drawer covers the page: it is a modal dialog named like its toggle, behind which an overlay sits.
        Assert.AreEqual("dialog", drawer.GetAttribute("role"));
        Assert.AreEqual("true", drawer.GetAttribute("aria-modal"));
        Assert.AreEqual(BitStrings.Current.Menu, drawer.GetAttribute("aria-label"));
        Assert.AreEqual("-1", drawer.GetAttribute("tabindex"));
        Assert.AreEqual(1, component.FindAll(".bit-nbr-ovl").Count);
        Assert.AreEqual("true", component.Find(".bit-nbr-ovl").GetAttribute("aria-hidden"));

        component.Find(".bit-nbr-tgl").Click();

        Assert.AreEqual("false", component.Find(".bit-nbr-tgl").GetAttribute("aria-expanded"));
        Assert.IsFalse(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dop"));
        Assert.IsNull(component.Find(".bit-nbr-drw").GetAttribute("role"));
        Assert.AreEqual(0, component.FindAll(".bit-nbr-ovl").Count);
        CollectionAssert.AreEqual(new[] { true, false }, opened);
    }

    [TestMethod]
    public void BitNavBarOpenDrawerShouldHoldThePageAndTheFocusAndHandThemBack()
    {
        var component = RenderOpenDrawer();

        // The drawer takes the focus onto itself, keeps it inside, and holds the page behind it from scrolling.
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.storeFocus"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.lockScroll"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.setupFocusTrap"));
        Assert.AreEqual(1, Invocations("Blazor._internal.domWrapper.focus"));

        var drawerId = component.Find(".bit-nbr-drw").Id;
        Assert.AreEqual(drawerId, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Single().Arguments[0]);

        component.Find(".bit-nbr-tgl").Click();

        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.disposeFocusTrap"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.unlockScroll"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.restoreFocus"));

        // Nothing is taken twice, and nothing handed back twice.
        component.Render();

        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.lockScroll"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.unlockScroll"));
    }

    [TestMethod]
    public void BitNavBarShouldNotHoldThePageBeforeTheBrowserReportsADrawer()
    {
        var component = RenderNavBar();

        component.Find(".bit-nbr-tgl").Click();

        // Opened, but the screen is not known to be a drawer's yet: nothing of the page is held.
        Assert.AreEqual("true", component.Find(".bit-nbr-tgl").GetAttribute("aria-expanded"));
        Assert.AreEqual(0, Invocations("BitBlazorUI.Utils.lockScroll"));
        Assert.IsNull(component.Find(".bit-nbr-drw").GetAttribute("role"));

        SetDrawerScreen(component, true);

        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.lockScroll"));
        Assert.AreEqual("dialog", component.Find(".bit-nbr-drw").GetAttribute("role"));
    }

    [TestMethod]
    public void BitNavBarItemClickShouldCloseTheDrawer()
    {
        BitNavBarItem? clicked = null;

        var component = RenderOpenDrawer(p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.OnItemClick, (BitNavBarItem item) => clicked = item);
        });

        component.FindAll(".bit-nbr-itm")[1].Click();

        Assert.AreEqual("Products", clicked?.Text);
        Assert.AreEqual("Products", component.Instance.SelectedItem?.Text);
        Assert.IsFalse(component.Instance.DrawerOpen);
        Assert.AreEqual("false", component.Find(".bit-nbr-tgl").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.unlockScroll"));
    }

    [TestMethod]
    public void BitNavBarClickOfTheCurrentDestinationShouldCloseTheDrawerToo()
    {
        var component = RenderOpenDrawer(items: UrlItems());

        // The reader picked where to go even when it is where they are.
        component.FindAll(".bit-nbr-itm")[0].Click();
        component.WaitForAssertion(() => Assert.IsFalse(component.Instance.DrawerOpen));

        component.Find(".bit-nbr-tgl").Click();
        Assert.IsTrue(component.Instance.DrawerOpen);

        component.FindAll(".bit-nbr-itm")[0].Click();
        component.WaitForAssertion(() => Assert.IsFalse(component.Instance.DrawerOpen));
    }

    [TestMethod]
    public void BitNavBarItemClickShouldLeaveTheBarOfAWideScreenAlone()
    {
        var component = RenderNavBar(p => p.Add(c => c.Mode, BitNavMode.Manual));

        SetDrawerScreen(component, false);

        component.FindAll(".bit-nbr-itm")[1].Click();

        Assert.IsFalse(component.Instance.DrawerOpen);
        Assert.AreEqual(0, Invocations("BitBlazorUI.Utils.lockScroll"));
    }

    [TestMethod]
    public void BitNavBarEscapeShouldCloseTheDrawerFromTheDrawerAndFromTheToggle()
    {
        var component = RenderOpenDrawer();

        // The press is claimed where it is acted on, so a surface around the navbar does not close on it too.
        Assert.AreEqual("claim", component.Find(".bit-nbr-drw").GetAttribute("data-bit-esc"));
        Assert.AreEqual("claim", component.Find(".bit-nbr-tgl").GetAttribute("data-bit-esc"));

        component.Find(".bit-nbr-drw").KeyDown(Key.Escape);

        Assert.IsFalse(component.Instance.DrawerOpen);
        Assert.IsNull(component.Find(".bit-nbr-drw").GetAttribute("data-bit-esc"));
        Assert.IsNull(component.Find(".bit-nbr-tgl").GetAttribute("data-bit-esc"));

        component.Find(".bit-nbr-tgl").Click();
        Assert.IsTrue(component.Instance.DrawerOpen);

        component.Find(".bit-nbr-tgl").KeyDown(Key.Escape);
        Assert.IsFalse(component.Instance.DrawerOpen);
    }

    [TestMethod]
    public void BitNavBarShouldIgnoreAModifiedEscapeAndOtherKeys()
    {
        var component = RenderOpenDrawer();

        component.Find(".bit-nbr-drw").KeyDown(Key.Escape + Key.Shift);
        component.Find(".bit-nbr-drw").KeyDown(Key.Enter);
        component.Find(".bit-nbr-tgl").KeyDown(Key.Escape + Key.Alt);

        Assert.IsTrue(component.Instance.DrawerOpen);
    }

    [TestMethod]
    public void BitNavBarShouldLeaveAnEscapeAPartOfTheDrawerClaimedAlone()
    {
        var component = RenderOpenDrawer();

        // The browser watches the drawer for the presses a part of it claims first (Utils.watchEscapeClaim).
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.watchEscapeClaim"));

        component.InvokeAsync(() => component.Instance._OnEscapeVerdict(true));
        component.Find(".bit-nbr-drw").KeyDown(Key.Escape);

        Assert.IsTrue(component.Instance.DrawerOpen);

        // The verdict is read once: the next press is the drawer's own.
        component.Find(".bit-nbr-drw").KeyDown(Key.Escape);

        Assert.IsFalse(component.Instance.DrawerOpen);
    }

    [TestMethod]
    public void BitNavBarShouldClaimNoEscapeOnAClosedDrawerOrAWideScreen()
    {
        var component = RenderNavBar();

        SetDrawerScreen(component, true);

        Assert.IsNull(component.Find(".bit-nbr-drw").GetAttribute("data-bit-esc"));
        Assert.IsNull(component.Find(".bit-nbr-tgl").GetAttribute("data-bit-esc"));

        component.Find(".bit-nbr-drw").KeyDown(Key.Escape);
        Assert.IsFalse(component.Instance.DrawerOpen);

        component.Render(p => p.Add(c => c.DrawerOpen, true));
        SetDrawerScreen(component, false);

        Assert.IsNull(component.Find(".bit-nbr-drw").GetAttribute("data-bit-esc"));
    }

    [TestMethod]
    public void BitNavBarOverlayShouldCloseTheDrawerOnAClickAndOnATap()
    {
        var component = RenderOpenDrawer();

        component.Find(".bit-nbr-ovl").Click();

        Assert.IsFalse(component.Instance.DrawerOpen);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-ovl").Count);

        component.Find(".bit-nbr-tgl").Click();

        var overlay = component.Find(".bit-nbr-ovl");

        // A drag that ends on the overlay is no tap.
        overlay.TouchStart(new TouchEventArgs { Touches = [Touch(100, 200)], ChangedTouches = [Touch(100, 200)] });
        overlay.TouchEnd(new TouchEventArgs { Touches = [], ChangedTouches = [Touch(160, 200)] });

        Assert.IsTrue(component.Instance.DrawerOpen);

        overlay.TouchStart(new TouchEventArgs { Touches = [Touch(100, 200)], ChangedTouches = [Touch(100, 200)] });
        overlay.TouchEnd(new TouchEventArgs { Touches = [], ChangedTouches = [Touch(103, 198)] });

        Assert.IsFalse(component.Instance.DrawerOpen);
    }

    [TestMethod]
    public void BitNavBarScreenGrowingOutOfTheDrawerShouldCloseIt()
    {
        var component = RenderOpenDrawer();

        SetDrawerScreen(component, false);

        Assert.IsFalse(component.Instance.DrawerOpen);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-ovl").Count);
        Assert.IsNull(component.Find(".bit-nbr-drw").GetAttribute("role"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.unlockScroll"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.disposeFocusTrap"));

        // The focus stays on the item it is on, which is in the bar now: the toggle button it would be handed back to
        // is hidden there.
        Assert.AreEqual(0, Invocations("BitBlazorUI.Utils.restoreFocus"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.forgetFocus"));
    }

    [TestMethod]
    public void BitNavBarNavigationShouldCloseTheDrawer()
    {
        var component = RenderOpenDrawer(p => p.Add(c => c.Mode, BitNavMode.Manual));

        // A link an item template renders navigates without the navbar hearing of the click.
        Services.GetRequiredService<NavigationManager>().NavigateTo("/somewhere");

        component.WaitForAssertion(() => Assert.IsFalse(component.Instance.DrawerOpen));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.unlockScroll"));
    }

    [TestMethod]
    public void BitNavBarShouldSetUpTheWheelOfTheListRenderedAnewInOrOutOfTheDrawer()
    {
        var component = RenderComponent<BitNavBar<BitNavBarItem>>(p =>
        {
            p.Add(c => c.Items, Items());
            p.Add(c => c.Scrollable, true);
        });

        Assert.AreEqual(1, Invocations("BitBlazorUI.NavBar.setupWheel"));

        component.Render(p => p.Add(c => c.DrawerBreakpoint, BitScreenQuery.LtMd));

        Assert.AreEqual(2, Invocations("BitBlazorUI.NavBar.setupWheel"));

        component.Render(p => p.Add(c => c.DrawerBreakpoint, BitScreenQuery.LtLg));

        Assert.AreEqual(2, Invocations("BitBlazorUI.NavBar.setupWheel"));

        component.Render(p => p.Add(c => c.DrawerBreakpoint, null));

        Assert.AreEqual(3, Invocations("BitBlazorUI.NavBar.setupWheel"));
    }

    [TestMethod]
    public void BitNavBarBoundDrawerOpenShouldOpenTheDrawer()
    {
        var component = RenderNavBar(p => p.Add(c => c.DrawerOpen, true));

        // Until the browser has reported the screen, the overlay is left to the stylesheet's fallback shape.
        Assert.AreEqual("true", component.Find(".bit-nbr-tgl").GetAttribute("aria-expanded"));
        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dop"));
        Assert.AreEqual(1, component.FindAll(".bit-nbr-ovl").Count);

        SetDrawerScreen(component, true);

        Assert.AreEqual("dialog", component.Find(".bit-nbr-drw").GetAttribute("role"));

        component.Render(p => p.Add(c => c.DrawerOpen, false));

        Assert.AreEqual("false", component.Find(".bit-nbr-tgl").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.unlockScroll"));
    }

    [TestMethod]
    public void BitNavBarDisabledShouldNeitherOpenNorCoverThePage()
    {
        var component = RenderNavBar(p => p.Add(c => c.Disabled, true));

        SetDrawerScreen(component, true);

        var toggle = component.Find(".bit-nbr-tgl");

        Assert.IsTrue(toggle.HasAttribute("disabled"));
        Assert.AreEqual("true", toggle.GetAttribute("aria-disabled"));

        toggle.Click();

        Assert.IsFalse(component.Instance.DrawerOpen);

        // A drawer open as the navbar is disabled covers nothing: no overlay, no dialog, no claim.
        component.Render(p => p.Add(c => c.DrawerOpen, true));

        Assert.AreEqual(0, component.FindAll(".bit-nbr-ovl").Count);
        Assert.IsNull(component.Find(".bit-nbr-drw").GetAttribute("role"));
        Assert.IsNull(component.Find(".bit-nbr-drw").GetAttribute("data-bit-esc"));
        Assert.AreEqual(0, Invocations("BitBlazorUI.Utils.lockScroll"));

        // Nor is it shown open, which would leave a drawer on screen with nothing to dismiss it with.
        Assert.IsFalse(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dop"));
        Assert.AreEqual("false", component.Find(".bit-nbr-tgl").GetAttribute("aria-expanded"));

        // Disabled while open, the drawer slides away and comes back once the navbar is enabled again.
        component.Render(p => p.Add(c => c.Disabled, false));

        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dop"));
        Assert.AreEqual("dialog", component.Find(".bit-nbr-drw").GetAttribute("role"));

        component.Render(p => p.Add(c => c.Disabled, true));

        Assert.IsFalse(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dop"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.unlockScroll"));
    }

    [TestMethod]
    public void BitNavBarShouldApplyTheClassesAndStylesOfTheDrawerParts()
    {
        var component = RenderOpenDrawer(p =>
        {
            p.Add(c => c.Classes, new() { Drawer = "drawer-class", Overlay = "overlay-class", ToggleButton = "toggle-class" });
            p.Add(c => c.Styles, new() { Drawer = "color: red", Overlay = "color: green", ToggleButton = "color: blue" });
        });

        var drawer = component.Find(".bit-nbr-drw");
        var overlay = component.Find(".bit-nbr-ovl");
        var toggle = component.Find(".bit-nbr-tgl");

        Assert.IsTrue(drawer.ClassList.Contains("drawer-class"));
        Assert.IsTrue(overlay.ClassList.Contains("overlay-class"));
        Assert.IsTrue(toggle.ClassList.Contains("toggle-class"));

        Assert.AreEqual("color: red", drawer.GetAttribute("style"));
        Assert.AreEqual("color: green", overlay.GetAttribute("style"));
        Assert.AreEqual("color: blue", toggle.GetAttribute("style"));
    }

    [TestMethod]
    public void BitNavBarShouldKeepTheHeaderAndTheFooterInTheBar()
    {
        var component = RenderNavBar(p =>
        {
            p.Add(c => c.HeaderTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"logo\">Logo</span>")));
            p.Add(c => c.FooterTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<button class=\"cta\">Start</button>")));
        });

        var children = component.Find(".bit-nbr").Children.Select(c => c.ClassList[0]).ToArray();

        // The header, the toggle and the drawer it controls, and the footer, in the order they are read in.
        CollectionAssert.AreEqual(new[] { "bit-nbr-hdr", "bit-nbr-tgl", "bit-nbr-drw", "bit-nbr-ftr" }, children);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-drw .cta").Count);
    }

    [TestMethod]
    public void BitNavBarShouldTakeTheDrawerParametersFromTheCascade()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitNavBarParams { DrawerBreakpoint = BitScreenQuery.LtLg, ToggleAriaLabel = "Navigation", ToggleIconName = "GlobalNavButton" }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitNavBar<BitNavBarItem>>(0);
                builder.AddAttribute(1, nameof(BitNavBar<BitNavBarItem>.Items), Items());
                builder.CloseComponent();
            });
        });

        var toggle = component.Find(".bit-nbr-tgl");

        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-dq-ltlg"));
        Assert.AreEqual("Navigation", toggle.GetAttribute("aria-label"));
        Assert.AreEqual(1, toggle.QuerySelectorAll(".bit-icon--GlobalNavButton").Length);
    }

    [TestMethod]
    public void BitNavBarDisposedWhileOpenShouldHandThePageBack()
    {
        var component = RenderOpenDrawer();

        component.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();

        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.unlockScroll"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.disposeFocusTrap"));
        Assert.AreEqual(1, Invocations("BitBlazorUI.Utils.forgetFocus"));
    }

    [TestMethod]
    public void BitNavBarStylesheetShouldResolveEveryScreenQueryAtTheDefaultBreakpoints()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Navs", "NavBar", "BitNavBar.scss").Replace("\r\n", "\n");
        var start = stylesheet.IndexOf("\n$nbr-drawer-queries: (", StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, "The stylesheet has no ranges for the screen queries.");

        var map = stylesheet[start..stylesheet.IndexOf("\n);", start, StringComparison.Ordinal)];

        // Until the browser reports the shape, the class of the query is all the stylesheet has to go by, so every
        // query a DrawerBreakpoint can name has its range there.
        foreach (var query in Enum.GetValues<BitScreenQuery>())
        {
            StringAssert.Contains(map, $"\n    {query.ToString().ToLowerInvariant()}: (", $"The stylesheet has no range for {query}.");
        }
    }

    [TestMethod]
    public void BitNavBarStylesheetShouldKeepTheSafeAreaMarginOffTheFixedDrawerAndOverlay()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Navs", "NavBar", "BitNavBar.scss").Replace("\r\n", "\n");

        // A margin on a box fixed from top to bottom cuts it short, which leaves a strip of the page under the open
        // drawer uncovered.
        StringAssert.Contains(stylesheet, ".bit-nbr-sfa:not(.bit-nbr-vrt) > :not(.bit-nbr-drw, .bit-nbr-ovl),");
        StringAssert.Contains(stylesheet, ".bit-nbr-sfa.bit-nbr-vrt > :last-child:not(.bit-nbr-drw),");
        StringAssert.Contains(stylesheet, ".bit-nbr.bit-nbr-sfa.bit-nbr-dsh {\n    @include nbr-drawer-safe-area;");
        StringAssert.Contains(stylesheet, ".bit-nbr.bit-nbr-sfa.bit-nbr-dq-#{$name} {");
    }

    [TestMethod]
    public void BitHeaderShouldNotConfineTheDrawerOfANavBarInsideIt()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Layouts", "Header", "BitHeader.scss");

        // A will-change of transform makes the header the containing block of the fixed drawer and overlay for as long
        // as the header is on the page, which confines them to its box.
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(stylesheet, @"^\s*will-change:\s*transform", System.Text.RegularExpressions.RegexOptions.Multiline));
    }

    private static TouchPoint Touch(double x, double y) => new() { ClientX = x, ClientY = y };
}
