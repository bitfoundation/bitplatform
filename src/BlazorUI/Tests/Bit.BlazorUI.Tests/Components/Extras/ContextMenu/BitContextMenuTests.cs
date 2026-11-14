using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ContextMenu;

[TestClass]
public class BitContextMenuTests : BunitTestContext
{
    private const string Toggle = "BitBlazorUI.Callouts.toggle";
    private const string GetPoint = "BitBlazorUI.Extras.getContextMenuPoint";

    private static List<BitMenuButtonItem> Items() =>
    [
        new() { Text = "Cut", Key = "cut" },
        new() { Text = "Copy", Key = "copy" },
        new() { Text = "Paste", Key = "paste" }
    ];

    private IRenderedComponent<BitContextMenu<BitMenuButtonItem>> Render(
        bool disabled = false,
        List<BitMenuButtonItem>? clicked = null,
        List<bool>? openChanges = null,
        string? ariaLabel = null,
        string? tabIndex = null)
    {
        return RenderComponent<BitContextMenu<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Id, "ctm");
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.Disabled, disabled);
            parameters.AddChildContent("<p class=\"content\">Right-click here</p>");
            if (ariaLabel is not null) parameters.Add(p => p.AriaLabel, ariaLabel);
            if (tabIndex is not null) parameters.Add(p => p.TabIndex, tabIndex);
            if (clicked is not null) parameters.Add(p => p.OnItemClick, (BitMenuButtonItem item) => clicked.Add(item));
            if (openChanges is not null) parameters.Add(p => p.IsOpenChanged, (bool isOpen) => openChanges.Add(isOpen));
        });
    }

    private static string? PointStyle(IRenderedComponent<BitContextMenu<BitMenuButtonItem>> com) => com.FindAll(".bit-mnb-pnt").SingleOrDefault()?.GetAttribute("style");

    private static MouseEventArgs RightClick(double x, double y) => new() { Type = "contextmenu", Button = 2, ClientX = x, ClientY = y };

    // What Chromium dispatches for Shift+F10 and the Menu key: no button, at the middle of the focused element.
    private static MouseEventArgs KeyboardContextMenu(double x, double y) => new() { Type = "contextmenu", Button = -1, ClientX = x, ClientY = y };

    [TestMethod]
    public void BitContextMenuShouldRenderItsContentInAFocusableAreaAndNoButton()
    {
        var com = Render();

        var root = com.Find("#ctm");

        Assert.IsTrue(root.ClassList.Contains("bit-ctm"));
        Assert.AreEqual("0", root.GetAttribute("tabindex"));
        Assert.IsNotNull(root.QuerySelector(".content"));

        // The menu is a menu button's, whose button is never shown, and which has no overlay to take the page's clicks.
        var menuButton = com.Find("#ctm-mnb");

        StringAssert.Contains(menuButton.GetAttribute("style"), "display:none");
        Assert.AreEqual(0, com.FindAll(".bit-mnb-ovl").Count);
        Assert.IsNotNull(com.Find(".bit-mnb-cal [role=menu]"));
        Assert.IsFalse(com.Instance.IsOpen);
    }

    [TestMethod]
    public void BitContextMenuShouldOpenItsMenuWhereTheAreaIsRightClicked()
    {
        var openChanges = new List<bool>();
        var com = Render(openChanges: openChanges);

        com.Find("#ctm").ContextMenu(RightClick(120, 80));

        Assert.IsTrue(com.Instance.IsOpen);
        CollectionAssert.AreEqual(new[] { true }, openChanges);

        var style = PointStyle(com)!;
        StringAssert.Contains(style, "left:120px");
        StringAssert.Contains(style, "top:80px");

        // Placed against the point, once.
        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations[Toggle].Count));
        Assert.AreEqual(com.Find(".bit-mnb-pnt").Id, Context.JSInterop.Invocations[Toggle][0].Arguments[1]);
    }

    [TestMethod]
    public void BitContextMenuShouldMoveItsOpenMenuWhereTheAreaIsRightClickedAgain()
    {
        var com = Render();

        var root = com.Find("#ctm");

        root.ContextMenu(RightClick(120, 80));
        com.Find("#ctm").ContextMenu(RightClick(300, 40));

        Assert.IsTrue(com.Instance.IsOpen);
        StringAssert.Contains(PointStyle(com)!, "left:300px");
        StringAssert.Contains(PointStyle(com)!, "top:40px");

        // Moved rather than closed and opened again.
        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Callouts.reposition"].Count));
        Assert.AreEqual(1, Context.JSInterop.Invocations[Toggle].Count);
    }

    [TestMethod]
    [DataRow("F10", true)]
    [DataRow("ContextMenu", false)]
    public void BitContextMenuShouldOpenItsMenuFromTheKeyboardAtTheFocusedElement(string key, bool shift)
    {
        Context.JSInterop.Setup<double[]?>(GetPoint, _ => true).SetResult([64, 48]);

        var com = Render();

        com.Find("#ctm").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

        com.WaitForAssertion(() => Assert.IsTrue(com.Instance.IsOpen));

        // The point is the middle of the element that has the focus in the area, which the script measures.
        var getPoint = Context.JSInterop.Invocations[GetPoint].Single();
        Assert.AreEqual(com.Instance.RootElement.Id, ((ElementReference)getPoint.Arguments[0]!).Id);

        StringAssert.Contains(PointStyle(com)!, "left:64px");
        StringAssert.Contains(PointStyle(com)!, "top:48px");
    }

    [TestMethod]
    [DataRow("F10", false)]
    [DataRow("Enter", false)]
    [DataRow("ArrowDown", true)]
    public void BitContextMenuShouldNotOpenItsMenuFromAnyOtherKey(string key, bool shift)
    {
        var com = Render();

        com.Find("#ctm").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

        Assert.IsFalse(com.Instance.IsOpen);
        Assert.AreEqual(0, Context.JSInterop.Invocations[GetPoint].Count);
    }

    [TestMethod]
    public void BitContextMenuShouldNotOpenItsMenuASecondTimeForTheBrowsersOwnEventOfTheKey()
    {
        Context.JSInterop.Setup<double[]?>(GetPoint, _ => true).SetResult([64, 48]);

        var com = Render();

        com.Find("#ctm").KeyDown(new KeyboardEventArgs { Key = "F10", ShiftKey = true });
        com.WaitForAssertion(() => Assert.IsTrue(com.Instance.IsOpen));

        // Chromium follows Shift+F10 with a contextmenu event of its own, at a point of its own.
        com.Find("#ctm").ContextMenu(KeyboardContextMenu(200, 100));

        StringAssert.Contains(PointStyle(com)!, "left:64px");
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Callouts.reposition"].Count);

        // That event was the one being waited for: a later one from the keyboard is the page's own again, and so is a
        // right-click, which moves the menu.
        com.Find("#ctm").ContextMenu(KeyboardContextMenu(200, 100));
        StringAssert.Contains(PointStyle(com)!, "left:200px");

        com.Find("#ctm").ContextMenu(RightClick(10, 20));
        StringAssert.Contains(PointStyle(com)!, "left:10px");
    }

    [TestMethod]
    public void BitContextMenuShouldTakeARightClickRightAfterTheKey()
    {
        Context.JSInterop.Setup<double[]?>(GetPoint, _ => true).SetResult([64, 48]);

        var com = Render();

        com.Find("#ctm").KeyDown(new KeyboardEventArgs { Key = "ContextMenu" });
        com.WaitForAssertion(() => Assert.IsTrue(com.Instance.IsOpen));

        // On a browser that sends no event of its own for the key, the next right-click is still the user's.
        com.Find("#ctm").ContextMenu(RightClick(10, 20));

        StringAssert.Contains(PointStyle(com)!, "left:10px");
    }

    [TestMethod]
    public void BitContextMenuShouldPassAnItemClickedOn()
    {
        var clicked = new List<BitMenuButtonItem>();
        var com = Render(clicked: clicked);

        com.Find("#ctm").ContextMenu(RightClick(120, 80));

        com.FindAll(".bit-mnb-itm")[1].Click();

        Assert.AreEqual("Copy", clicked.Single().Text);
        Assert.IsFalse(com.Instance.IsOpen);
    }

    [TestMethod]
    public void BitContextMenuShouldNotPassTheClickOfTheHiddenButton()
    {
        var clicked = new List<BitMenuButtonItem>();
        var com = Render(clicked: clicked);

        // The menu button raises OnClick for its own button with no item; nothing an app can click here.
        com.Find("#ctm-mnb-opb").Click();

        Assert.AreEqual(0, clicked.Count);
    }

    [TestMethod]
    public void BitContextMenuShouldBeClosedFromItsIsOpenParameter()
    {
        var com = Render();

        com.Find("#ctm").ContextMenu(RightClick(120, 80));

        Assert.IsTrue(com.Instance.IsOpen);

        com.Render(parameters => parameters.Add(p => p.IsOpen, false));

        com.WaitForAssertion(() => Assert.AreEqual(2, Context.JSInterop.Invocations[Toggle].Count));
        Assert.AreEqual(false, Context.JSInterop.Invocations[Toggle][1].Arguments[6]);
        Assert.AreEqual("false", com.Find("#ctm-mnb-opb").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public async Task BitContextMenuShouldOpenItsMenuAtCoordinates()
    {
        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(30, 40));

        Assert.IsTrue(com.Instance.IsOpen);
        StringAssert.Contains(PointStyle(com)!, "left:30px");
        StringAssert.Contains(PointStyle(com)!, "top:40px");
    }

    [TestMethod]
    public async Task DisabledBitContextMenuShouldLeaveTheAreaToTheBrowser()
    {
        Context.JSInterop.Setup<double[]?>(GetPoint, _ => true).SetResult([64, 48]);

        var com = Render(disabled: true);

        var root = com.Find("#ctm");

        // No tab stop of its own, since there is nothing to open from it.
        Assert.IsNull(root.GetAttribute("tabindex"));

        root.ContextMenu(RightClick(120, 80));
        com.Find("#ctm").KeyDown(new KeyboardEventArgs { Key = "F10", ShiftKey = true });
        await com.InvokeAsync(() => com.Instance.OpenAt(30, 40));

        Assert.IsFalse(com.Instance.IsOpen);
        Assert.AreEqual(0, com.FindAll(".bit-mnb-pnt").Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations[GetPoint].Count);

        // The content is still the page's.
        Assert.IsNotNull(com.Find("#ctm .content"));
    }

    [TestMethod]
    public void BitContextMenuShouldKeepATabIndexThePageGaveIt()
    {
        var com = Render(tabIndex: "-1");

        Assert.AreEqual("-1", com.Find("#ctm").GetAttribute("tabindex"));
    }

    [TestMethod]
    [DataRow(null, "Context menu")]
    [DataRow("File actions", "File actions")]
    public void BitContextMenuShouldNameItsMenu(string? ariaLabel, string expected)
    {
        var com = Render(ariaLabel: ariaLabel);

        // The menu is named by the hidden button it belongs to.
        var menu = com.Find("[role=menu]");
        var labelledBy = menu.GetAttribute("aria-labelledby");

        Assert.AreEqual("ctm-mnb-opb", labelledBy);
        Assert.AreEqual(expected, com.Find($"#{labelledBy}").GetAttribute("aria-label"));

        // The area itself is a plain block of content, which an aria-label would not be allowed on.
        Assert.IsNull(com.Find("#ctm").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitContextMenuShouldRenderItsMenuFromOptions()
    {
        var com = RenderComponent<BitContextMenu<BitMenuButtonOption>>(parameters =>
        {
            parameters.Add(p => p.Options, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitMenuButtonOption>(0);
                builder.AddAttribute(1, nameof(BitMenuButtonOption.Text), "Rename");
                builder.AddAttribute(2, nameof(BitMenuButtonOption.Key), "rename");
                builder.CloseComponent();
            }));
        });

        Assert.AreEqual("Rename", com.Find(".bit-mnb-itm").TextContent.Trim());
    }
}
