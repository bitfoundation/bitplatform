using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

/// <summary>
/// Covers what a navigation (mega) menu needs of the menu button: the description line of an item, the item matching
/// the current URL, and the OpenOnHover mode with its delays, its keyboard hand-over and the touch screens it leaves
/// to the click.
/// </summary>
[TestClass]
public class BitMenuButtonNavigationTests : BunitTestContext
{
    private const string IsHoverDevice = "BitBlazorUI.Utils.isHoverDevice";
    private const string SetupMenuKeyboard = "BitBlazorUI.Callouts.setupMenuKeyboard";
    private const string DisposeKeyboard = "BitBlazorUI.Callouts.disposeKeyboard";
    private const string FocusItem = "BitBlazorUI.MenuButtons.focusItem";

    private static List<BitMenuButtonItem> NavItems() =>
    [
        new() { Text = "Platform", Key = "platform", Href = "/platform", Description = "Build apps faster", Match = BitNavMatch.Exact },
        new()
        {
            Text = "Products",
            Key = "products",
            Description = "Everything we make",
            ChildItems =
            [
                new() { Text = "BlazorUI", Key = "blazorui", Href = "/products/blazorui", Match = BitNavMatch.Prefix },
                new() { Text = "Butil", Key = "butil", Href = "/products/butil", Match = BitNavMatch.Prefix }
            ]
        },
        new() { Text = "Pricing", Key = "pricing", Href = "/pricing" }
    ];

    private IRenderedComponent<BitMenuButton<BitMenuButtonItem>> RenderNav(List<BitMenuButtonItem>? items = null,
                                                                           BitMenuButtonClassStyles? classes = null,
                                                                           BitMenuButtonClassStyles? styles = null)
    {
        return RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Items, items ?? NavItems());
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
        });
    }

    private void NavigateTo(string url) => Services.GetRequiredService<NavigationManager>().NavigateTo(url);

    private static AngleSharp.Dom.IElement RootItem(IRenderedComponent<BitMenuButton<BitMenuButtonItem>> com, int index)
    {
        return com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[index];
    }

    private IRenderedComponent<BitMenuButton<BitMenuButtonItem>> RenderHover(int hoverCloseDelay = 0, bool split = false, bool disabled = false)
    {
        Context.JSInterop.Setup<bool>(IsHoverDevice, _ => true).SetResult(true);

        return RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Products");
            parameters.Add(p => p.Items, NavItems());
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverCloseDelay, hoverCloseDelay);
            parameters.Add(p => p.Split, split);
            parameters.Add(p => p.Disabled, disabled);
        });
    }

    private static string? Expanded(IRenderedComponent<BitMenuButton<BitMenuButtonItem>> com)
    {
        return com.Find(".bit-mnb-opb").GetAttribute("aria-expanded");
    }

    private static bool HasHandler(AngleSharp.Dom.IElement element, string eventName)
    {
        return element.HasAttribute($"blazor:{eventName}");
    }



    [TestMethod]
    public void BitMenuButtonShouldRenderTheDescriptionOfAnItemUnderItsText()
    {
        var com = RenderNav();

        var item = RootItem(com, 0);
        var label = item.QuerySelector(".bit-mnb-ilb");
        Assert.IsNotNull(label);
        Assert.AreEqual("Platform", label.QuerySelector(".bit-mnb-btx")!.TextContent.Trim());

        var description = label.QuerySelector(".bit-mnb-ids")!;
        Assert.AreEqual("Build apps faster", description.TextContent.Trim());

        // The description is announced as one rather than folded into the name of the item.
        Assert.IsFalse(string.IsNullOrEmpty(description.Id));
        Assert.AreEqual(description.Id, item.GetAttribute("aria-describedby"));
        Assert.AreEqual("Platform", item.GetAttribute("aria-label"));

        // A row of a submenu carries one too.
        Assert.AreEqual("Everything we make", RootItem(com, 1).QuerySelector(".bit-mnb-ids")!.TextContent.Trim());

        // An item without one keeps the markup it always had.
        var plain = RootItem(com, 2);
        Assert.IsNull(plain.QuerySelector(".bit-mnb-ilb"));
        Assert.IsNull(plain.GetAttribute("aria-describedby"));
        Assert.IsNull(plain.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMenuButtonShouldKeepTheSecondaryTextAndTheAriaLabelInTheNameOfAnItemWithADescription()
    {
        var com = RenderNav(
        [
            new() { Text = "Save", Key = "save", Description = "Writes the file", SecondaryText = "Ctrl+S" },
            new() { Text = "Open", Key = "open", Description = "Reads a file", AriaLabel = "Open a file" }
        ]);

        Assert.AreEqual("Save Ctrl+S", RootItem(com, 0).GetAttribute("aria-label"));
        Assert.AreEqual("Open a file", RootItem(com, 1).GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMenuButtonShouldLeaveTheDescriptionToATemplate()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "Platform", Key = "platform", Description = "Build apps faster" }]);
            parameters.Add(p => p.ItemTemplate, item => builder => builder.AddContent(0, item?.Text));
        });

        Assert.IsNull(com.Find(".bit-mnb-itm").QuerySelector(".bit-mnb-ids"));
        Assert.IsNull(com.Find(".bit-mnb-itm").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitMenuButtonShouldApplyTheItemDescriptionClassStyles()
    {
        var com = RenderNav(classes: new() { ItemDescription = "custom-description" },
                            styles: new() { ItemDescription = "color: red;" });

        var description = RootItem(com, 0).QuerySelector(".bit-mnb-ids")!;

        Assert.IsTrue(description.ClassList.Contains("custom-description"));
        Assert.AreEqual("color: red;", description.GetAttribute("style"));
    }

    [TestMethod]
    public void BitMenuButtonShouldMarkTheItemMatchingTheCurrentUrl()
    {
        NavigateTo("/platform");

        var com = RenderNav(classes: new() { Current = "custom-current" }, styles: new() { Current = "font-weight: 700" });

        var current = RootItem(com, 0);
        Assert.AreEqual("page", current.GetAttribute("aria-current"));
        Assert.IsTrue(current.ClassList.Contains("bit-mnb-cur"));
        Assert.IsTrue(current.ClassList.Contains("custom-current"));
        StringAssert.Contains(current.GetAttribute("style"), "font-weight: 700");

        // The row of a submenu that does not hold the current page is not marked.
        Assert.IsFalse(RootItem(com, 1).ClassList.Contains("bit-mnb-cur"));

        // An item that says nothing about matching is never marked, whatever its Href.
        NavigateTo("/pricing");
        Assert.IsNull(RootItem(com, 2).GetAttribute("aria-current"));
        Assert.IsFalse(RootItem(com, 2).ClassList.Contains("bit-mnb-cur"));
    }

    [TestMethod]
    public void BitMenuButtonShouldFollowTheCurrentUrlAsTheAppNavigates()
    {
        NavigateTo("/platform");

        var com = RenderNav();

        Assert.AreEqual("page", RootItem(com, 0).GetAttribute("aria-current"));

        NavigateTo("/products/blazorui/getting-started");

        com.WaitForAssertion(() => Assert.IsNull(RootItem(com, 0).GetAttribute("aria-current")));

        // The prefix match marks the item of the submenu, and the row that opens the submenu is drawn as the way to
        // it - with the class alone, since the page it names is the one inside.
        var parent = RootItem(com, 1);
        Assert.IsTrue(parent.ClassList.Contains("bit-mnb-cur"));
        Assert.IsNull(parent.GetAttribute("aria-current"));

        var child = com.FindAll(".bit-mnb-sub .bit-mnb-itm").First(i => i.TextContent.Contains("BlazorUI"));
        Assert.AreEqual("page", child.GetAttribute("aria-current"));
        Assert.IsTrue(child.ClassList.Contains("bit-mnb-cur"));
    }

    [TestMethod]
    public void BitMenuButtonOptionShouldCarryTheDescriptionAndTheMatch()
    {
        NavigateTo("/docs");

        var com = RenderComponent<BitMenuButton<BitMenuButtonOption>>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitMenuButtonOption>(0);
                builder.AddComponentParameter(1, nameof(BitMenuButtonOption.Text), "Docs");
                builder.AddComponentParameter(2, nameof(BitMenuButtonOption.Href), "/docs");
                builder.AddComponentParameter(3, nameof(BitMenuButtonOption.Description), "Read the guides");
                builder.AddComponentParameter(4, nameof(BitMenuButtonOption.Match), (BitNavMatch?)BitNavMatch.Exact);
                builder.CloseComponent();

                builder.OpenComponent<BitMenuButtonOption>(5);
                builder.AddComponentParameter(6, nameof(BitMenuButtonOption.Text), "Blog");
                builder.AddComponentParameter(7, nameof(BitMenuButtonOption.Href), "/blog");
                builder.AddComponentParameter(8, nameof(BitMenuButtonOption.Match), (BitNavMatch?)BitNavMatch.Exact);
                builder.CloseComponent();
            }));
        });

        var items = com.FindAll(".bit-mnb-itm");

        Assert.AreEqual("Read the guides", items[0].QuerySelector(".bit-mnb-ids")!.TextContent.Trim());
        Assert.AreEqual("page", items[0].GetAttribute("aria-current"));
        Assert.IsNull(items[1].GetAttribute("aria-current"));

        // The options render their own items, and still follow the app as it navigates.
        NavigateTo("/blog");

        com.WaitForAssertion(() => Assert.AreEqual("page", com.FindAll(".bit-mnb-itm")[1].GetAttribute("aria-current")));
        Assert.IsNull(com.FindAll(".bit-mnb-itm")[0].GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitMenuButtonCustomItemsShouldReadTheDescriptionAndTheMatchThroughTheNameSelectors()
    {
        NavigateTo("/a/b");

        var com = RenderComponent<BitMenuButton<CustomNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new() { Name = "A", Url = "/a", Summary = "First", UrlMatch = BitNavMatch.Prefix },
                new() { Name = "B", Url = "/b", Summary = "Second", UrlMatch = BitNavMatch.Prefix }
            ]);
            parameters.Add(p => p.NameSelectors, new()
            {
                Text = { Name = nameof(CustomNavItem.Name) },
                Href = { Name = nameof(CustomNavItem.Url) },
                Description = { Name = nameof(CustomNavItem.Summary) },
                Match = { Selector = i => i.UrlMatch }
            });
        });

        var items = com.FindAll(".bit-mnb-itm");

        Assert.AreEqual("First", items[0].QuerySelector(".bit-mnb-ids")!.TextContent.Trim());
        Assert.AreEqual("page", items[0].GetAttribute("aria-current"));
        Assert.IsNull(items[1].GetAttribute("aria-current"));
    }



    [TestMethod]
    public void BitMenuButtonShouldOpenAndCloseOnHover()
    {
        var com = RenderHover();

        com.Find(".bit-mnb").MouseEnter();

        Assert.AreEqual("true", Expanded(com));

        com.Find(".bit-mnb").MouseLeave();

        com.WaitForAssertion(() => Assert.AreEqual("false", Expanded(com)));
    }

    [TestMethod]
    public void BitMenuButtonShouldNotHoverOnADeviceWithoutAPointerToHoverWith()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Products");
            parameters.Add(p => p.Items, NavItems());
            parameters.Add(p => p.OpenOnHover, true);
        });

        // The JS side reports no hovering pointer, which is what a touch screen does: the tap is left to the click,
        // and the pointer is not even listened to.
        Assert.IsFalse(HasHandler(com.Find(".bit-mnb"), "onmouseenter"));

        Assert.AreEqual("false", Expanded(com));
        Assert.HasCount(1, com.FindAll(".bit-mnb-ovl"));

        com.Find(".bit-mnb-opb").Click();

        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public void BitMenuButtonShouldRenderNoOverlayInTheHoverMode()
    {
        var com = RenderHover();

        // The overlay covers the page while the menu is open, so it would take the pointer away from the button the
        // menu was opened from - and close the menu it only just opened. It stays hidden rather than leaving the page,
        // since whether the device hovers is only known after the first render.
        com.WaitForAssertion(() => Assert.IsTrue(HasHandler(com.Find(".bit-mnb"), "onmouseenter")));

        com.Find(".bit-mnb").MouseEnter();

        StringAssert.Contains(com.Find(".bit-mnb-ovl").GetAttribute("style"), "display:none");
        var toggle = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"].Last();
        Assert.AreEqual(string.Empty, toggle.Arguments[5]);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldKeepTheMenuOpenWhileThePointerMovesIntoIt()
    {
        var com = RenderHover(hoverCloseDelay: 500);

        com.Find(".bit-mnb").MouseEnter();
        Assert.AreEqual("true", Expanded(com));

        // The pointer moves off the button and onto the menu before the close delay is up. Both moves are raised in
        // one turn of the renderer, and the leave's handler is awaited, which is over only once the close it
        // scheduled has been called off - or, had the menu not called it off, carried out.
        Task leave = Task.CompletedTask, enter = Task.CompletedTask;
        await com.InvokeAsync(() =>
        {
            leave = com.Find(".bit-mnb").MouseLeaveAsync(new MouseEventArgs());
            enter = com.Find(".bit-mnb-cal:not(.bit-mnb-sub)").MouseEnterAsync(new MouseEventArgs());
        });
        await Task.WhenAll(leave, enter);

        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public async Task BitMenuButtonShouldKeepTheMenuOpenWhileThePointerMovesIntoASubmenu()
    {
        var com = RenderHover(hoverCloseDelay: 500);

        com.Find(".bit-mnb").MouseEnter();

        // The submenu is moved to the body while it is open, so the pointer travelling into it leaves the menu.
        Task leave = Task.CompletedTask, enter = Task.CompletedTask;
        await com.InvokeAsync(() =>
        {
            leave = com.Find(".bit-mnb-cal:not(.bit-mnb-sub)").MouseLeaveAsync(new MouseEventArgs());
            enter = com.Find(".bit-mnb-sub").MouseEnterAsync(new MouseEventArgs());
        });
        await Task.WhenAll(leave, enter);

        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public void BitMenuButtonShouldNotCloseOnAClickWhileThePointerHoldsItOpen()
    {
        var com = RenderHover();

        com.Find(".bit-mnb").MouseEnter();
        com.Find(".bit-mnb-opb").Click();

        // Closing here would take away what the pointer has only just been shown.
        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public void BitMenuButtonShouldOpenOnAClickInTheHoverModeWhenThePointerIsAway()
    {
        var com = RenderHover();

        com.Find(".bit-mnb-opb").Click();
        Assert.AreEqual("true", Expanded(com));

        com.Find(".bit-mnb-opb").Click();
        Assert.AreEqual("false", Expanded(com));
    }

    [TestMethod]
    public void BitMenuButtonShouldLeaveTheFocusWhereItIsWhenOpenedByHovering()
    {
        var com = RenderHover();

        com.Find(".bit-mnb").MouseEnter();

        Assert.IsEmpty(Context.JSInterop.Invocations[FocusItem]);

        // A menu shown wherever the focus is answers an Escape pressed anywhere outside it and the root (WCAG 1.4.13),
        // which is the pointer's mode of the callout keyboard.
        var setup = Context.JSInterop.Invocations[SetupMenuKeyboard].Single();
        Assert.AreEqual("focusIn", setup.Arguments[2]);
        Assert.AreEqual(com.Find(".bit-mnb").Id, setup.Arguments[4]);
        Assert.IsFalse((bool)setup.Arguments[5]!);
    }

    [TestMethod]
    [DataRow("Enter")]
    [DataRow(" ")]
    [DataRow("ArrowDown")]
    public void BitMenuButtonShouldHandTheHoverOpenedMenuToTheKeyboardOnRequest(string key)
    {
        var com = RenderHover();

        com.Find(".bit-mnb").MouseEnter();

        var trigger = com.Find(".bit-mnb-opb");
        trigger.KeyDown(key switch { "Enter" => Key.Enter, " " => Key.Space, _ => Key.Down });
        if (key is not "ArrowDown")
        {
            // The browser dispatches the click that activating a button from the keyboard makes.
            trigger.Click();
        }

        // The menu stays open and the focus is taken into it rather than closing what is already showing.
        Assert.AreEqual("true", Expanded(com));
        Assert.HasCount(1, Context.JSInterop.Invocations[FocusItem]);
        Assert.AreEqual("first", Context.JSInterop.Invocations[FocusItem][0].Arguments[1]);

        // The Escape from anywhere is the pointer's alone, and goes with the hand-over.
        Assert.HasCount(1, Context.JSInterop.Invocations[DisposeKeyboard]);

        // The keyboard holds the menu now, so the pointer drifting off the button does not close it.
        com.Find(".bit-mnb").MouseLeave();
        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public void BitMenuButtonShouldNotCloseOnPointerLeaveWhenOpenedFromTheKeyboard()
    {
        var com = RenderHover();

        com.Find(".bit-mnb-opb").KeyDown(Key.Down);
        Assert.AreEqual("true", Expanded(com));

        com.Find(".bit-mnb").MouseEnter();
        com.Find(".bit-mnb").MouseLeave();

        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public async Task BitMenuButtonShouldCloseTheHoverOpenedMenuOnAnEscapeFromAnywhere()
    {
        var com = RenderHover(hoverCloseDelay: 5000);

        com.Find(".bit-mnb").MouseEnter();
        Assert.AreEqual("true", Expanded(com));

        await com.InvokeAsync(() => com.Instance._OnEscape());

        Assert.AreEqual("false", Expanded(com));
        Assert.HasCount(1, Context.JSInterop.Invocations[DisposeKeyboard]);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldHandTheHoverOpenedMenuToTheKeyboardWhenTheFocusGoesIn()
    {
        var com = RenderHover();

        com.Find(".bit-mnb").MouseEnter();

        await com.InvokeAsync(() => com.Instance._OnFocusIn());

        com.Find(".bit-mnb").MouseLeave();

        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public void BitMenuButtonShouldNotOpenOnHoverWhenDisabled()
    {
        var com = RenderHover(disabled: true);

        com.Find(".bit-mnb").MouseEnter();

        Assert.AreEqual("false", Expanded(com));
    }

    [TestMethod]
    public async Task BitMenuButtonShouldWaitOutTheHoverOpenDelay()
    {
        Context.JSInterop.Setup<bool>(IsHoverDevice, _ => true).SetResult(true);

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Products");
            parameters.Add(p => p.Items, NavItems());
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverOpenDelay, 5000);
        });

        // The pointer passing over the button leaves before the delay is up, which calls the opening off.
        Task enter = Task.CompletedTask;
        await com.InvokeAsync(() =>
        {
            enter = com.Find(".bit-mnb").MouseEnterAsync(new MouseEventArgs());
            _ = com.Find(".bit-mnb").MouseLeaveAsync(new MouseEventArgs());
        });
        await enter;

        Assert.AreEqual("false", Expanded(com));
    }

    [TestMethod]
    public void BitMenuButtonShouldNotNameAnItemWithNoTextAfterItsText()
    {
        var com = RenderNav([new() { IconName = "Home", Key = "home", Href = "/", Description = "Go to start" }]);

        // An empty aria-label would take the name its content gives it.
        Assert.IsNull(RootItem(com, 0).GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMenuButtonCustomItemsShouldIgnoreAMatchPropertyOfAnotherType()
    {
        NavigateTo("/a");

        var com = RenderComponent<BitMenuButton<OtherMatchItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "A", Href = "/a", Match = "exact" }]);
        });

        Assert.IsNull(com.Find(".bit-mnb-itm").GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitMenuButtonShouldNotListenToThePointerOutsideTheHoverMode()
    {
        var com = RenderNav([new() { Text = "Products", Key = "products", ChildItems = [new() { Text = "A", Key = "a" }] }]);

        Assert.IsFalse(HasHandler(com.Find(".bit-mnb"), "onmouseenter"));
        Assert.IsFalse(HasHandler(com.Find(".bit-mnb"), "onmouseleave"));
        Assert.IsFalse(HasHandler(com.Find(".bit-mnb-cal:not(.bit-mnb-sub)"), "onmouseenter"));
        Assert.IsFalse(HasHandler(com.Find(".bit-mnb-cal:not(.bit-mnb-sub)"), "onmouseleave"));
        Assert.IsFalse(HasHandler(com.Find(".bit-mnb-sub"), "onmouseleave"));
    }

    [TestMethod]
    public void BitMenuButtonShouldRunThePageMouseHandlersInTheHoverMode()
    {
        Context.JSInterop.Setup<bool>(IsHoverDevice, _ => true).SetResult(true);

        var entered = 0;
        var left = 0;
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Products");
            parameters.Add(p => p.Items, NavItems());
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverCloseDelay, 0);
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object>
            {
                ["onmouseenter"] = EventCallback.Factory.Create<MouseEventArgs>(this, () => entered++),
                ["onMouseLeave"] = (System.Action)(() => left++)
            });
        });

        com.WaitForAssertion(() => Assert.IsTrue(HasHandler(com.Find(".bit-mnb"), "onmouseenter")));

        com.Find(".bit-mnb").MouseEnter();
        Assert.AreEqual(1, entered);
        Assert.AreEqual("true", Expanded(com));

        com.Find(".bit-mnb").MouseLeave();
        Assert.AreEqual(1, left);
        com.WaitForAssertion(() => Assert.AreEqual("false", Expanded(com)));
    }

    [TestMethod]
    public void BitMenuButtonShouldCloseOnAClickAMenuTheKeyboardHoldsWhileThePointerIsOnIt()
    {
        var com = RenderHover();

        com.Find(".bit-mnb").MouseEnter();
        com.Find(".bit-mnb-opb").KeyDown(Key.Down);
        Assert.AreEqual("true", Expanded(com));

        // The pointer is still on the button, but the menu is the keyboard's now, so the button toggles it as always.
        com.Find(".bit-mnb-opb").Click();

        Assert.AreEqual("false", Expanded(com));
    }

    [TestMethod]
    public async Task BitMenuButtonShouldNotHandToThePointerAMenuOpenedOtherwiseWhileItsOpenDelayRan()
    {
        Context.JSInterop.Setup<bool>(IsHoverDevice, _ => true).SetResult(true);

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Products");
            parameters.Add(p => p.Items, NavItems());
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverOpenDelay, 200);
            parameters.Add(p => p.IsOpen, false);
        });

        com.WaitForAssertion(() => Assert.IsTrue(HasHandler(com.Find(".bit-mnb"), "onmouseenter")));

        // The page opens the menu while the open the pointer asked for is still being waited out.
        Task enter = Task.CompletedTask;
        await com.InvokeAsync(() => enter = com.Find(".bit-mnb").MouseEnterAsync(new MouseEventArgs()));
        com.Render(parameters => parameters.Add(p => p.IsOpen, true));
        await enter;

        // The delay ending does not open it a second time and take it over for the pointer.
        Assert.AreEqual("true", Expanded(com));
        Assert.IsEmpty(Context.JSInterop.Invocations[SetupMenuKeyboard]);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldNotOpenOnHoverAfterTheKeyboardOpenedItDuringTheOpenDelay()
    {
        Context.JSInterop.Setup<bool>(IsHoverDevice, _ => true).SetResult(true);

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Products");
            parameters.Add(p => p.Items, NavItems());
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverOpenDelay, 200);
            parameters.Add(p => p.HoverCloseDelay, 0);
        });

        com.WaitForAssertion(() => Assert.IsTrue(HasHandler(com.Find(".bit-mnb"), "onmouseenter")));

        Task enter = Task.CompletedTask, key = Task.CompletedTask;
        await com.InvokeAsync(() =>
        {
            enter = com.Find(".bit-mnb").MouseEnterAsync(new MouseEventArgs());
            key = com.Find(".bit-mnb-opb").KeyDownAsync(Key.Down);
        });
        await Task.WhenAll(enter, key);

        // The keyboard holds the menu: the pointer never took it, and leaving does not close it.
        Assert.AreEqual("true", Expanded(com));
        Assert.IsEmpty(Context.JSInterop.Invocations[SetupMenuKeyboard]);

        com.Find(".bit-mnb").MouseLeave();
        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public async Task BitMenuButtonShouldNotCloseFromUnderTheKeyboardAfterThePointerLeft()
    {
        Context.JSInterop.Setup<bool>(IsHoverDevice, _ => true).SetResult(true);

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Products");
            parameters.Add(p => p.Items, NavItems());
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverCloseDelay, 200);
            parameters.Add(p => p.IsOpen, false);
        });

        com.WaitForAssertion(() => Assert.IsTrue(HasHandler(com.Find(".bit-mnb"), "onmouseenter")));

        // A menu the page opened, which the pointer crosses and leaves.
        com.Render(parameters => parameters.Add(p => p.IsOpen, true));
        com.Find(".bit-mnb").MouseEnter();

        // The keyboard takes the menu while the close the pointer scheduled is still being waited out.
        Task leave = Task.CompletedTask, key = Task.CompletedTask;
        await com.InvokeAsync(() =>
        {
            leave = com.Find(".bit-mnb").MouseLeaveAsync(new MouseEventArgs());
            key = com.Find(".bit-mnb-opb").KeyDownAsync(Key.Down);
        });
        await Task.WhenAll(leave, key);

        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public void BitMenuButtonShouldSetUpEvenWhenTheHoverDeviceCannotBeAsked()
    {
        Context.JSInterop.Setup<bool>(IsHoverDevice, _ => true).SetException(new Microsoft.JSInterop.JSException("failed"));

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Products");
            parameters.Add(p => p.Items, NavItems());
            parameters.Add(p => p.OpenOnHover, true);
        });

        Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.MenuButtons.setup"]);

        // The device is taken for one that does not hover, which leaves the button toggling on a click.
        com.Find(".bit-mnb-opb").Click();
        Assert.AreEqual("true", Expanded(com));
    }

    [TestMethod]
    public void BitMenuButtonShouldTellTheJsSideTheOverlayIsGoneFromAMenuOpenBeforeTheDeviceWasKnown()
    {
        var device = Context.JSInterop.Setup<bool>(IsHoverDevice, _ => true);

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Products");
            parameters.Add(p => p.Items, NavItems());
            parameters.Add(p => p.OpenOnHover, true);
        });

        // The menu opens before the device has answered, so it is handed to the JS side with its overlay.
        com.Find(".bit-mnb-opb").Click();
        var overlayId = com.Find(".bit-mnb-ovl").Id;
        Assert.AreEqual(overlayId, Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"].Last().Arguments[5]);

        device.SetResult(true);

        com.WaitForAssertion(() =>
        {
            var toggle = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"].Last();
            Assert.AreEqual(string.Empty, toggle.Arguments[5]);
            Assert.IsTrue((bool)toggle.Arguments[6]!);
        });
        StringAssert.Contains(com.Find(".bit-mnb-ovl").GetAttribute("style"), "display:none");
    }



    public class CustomNavItem
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Summary { get; set; }
        public BitNavMatch? UrlMatch { get; set; }
    }

    public class OtherMatchItem
    {
        public string? Text { get; set; }
        public string? Href { get; set; }
        public string? Match { get; set; }
    }
}
