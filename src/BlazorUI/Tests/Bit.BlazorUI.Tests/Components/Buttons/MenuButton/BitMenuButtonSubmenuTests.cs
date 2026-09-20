using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

[TestClass]
public class BitMenuButtonSubmenuTests : BunitTestContext
{
    private static List<BitMenuButtonItem> SubmenuItems() =>
    [
        new() { Text = "Text box", Key = "text" },
        new()
        {
            Text = "Chart",
            Key = "chart",
            ChildItems =
            [
                new() { Text = "Bar", Key = "bar" },
                new()
                {
                    Text = "More",
                    Key = "more",
                    ChildItems = [new() { Text = "Scatter", Key = "scatter" }]
                }
            ]
        },
        new() { Text = "Table", Key = "table" }
    ];

    [TestMethod]
    public void BitMenuButtonAnItemWithChildrenShouldHaveMenuPopupSemantics()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, SubmenuItems());
        });

        var rootItems = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm");
        Assert.AreEqual(3, rootItems.Count);

        var parentItem = rootItems[1];

        Assert.AreEqual("menuitem", parentItem.GetAttribute("role"));
        Assert.AreEqual("menu", parentItem.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", parentItem.GetAttribute("aria-expanded"));
        // The submenu is not in the accessibility tree while it is closed, so nothing points at it yet.
        Assert.IsNull(parentItem.GetAttribute("aria-controls"));

        // A row without a submenu says nothing about one.
        Assert.IsNull(rootItems[0].GetAttribute("aria-haspopup"));
        Assert.IsNull(rootItems[0].GetAttribute("aria-expanded"));

        // The chevron is decoration: aria-haspopup on the row is what announces the submenu.
        var chevron = parentItem.QuerySelector(".bit-mnb-sch");
        Assert.IsNotNull(chevron);
        Assert.AreEqual("true", chevron.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitMenuButtonASubmenuShouldBeAMenuOfItsOwnLabelledByTheItemItOpensFrom()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, SubmenuItems());
        });

        var parentItem = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1];
        var submenu = com.Find(".bit-mnb-sub");
        var submenuList = submenu.QuerySelector("ul");

        Assert.IsNotNull(submenuList);
        Assert.AreEqual("menu", submenuList.GetAttribute("role"));
        Assert.AreEqual(parentItem.Id, submenuList.GetAttribute("aria-labelledby"));

        // The rows of the submenu are its own, and the one that opens a submenu of its own carries the
        // same semantics one level further in.
        var submenuItems = submenuList.QuerySelectorAll(":scope > li > .bit-mnb-itm");
        Assert.AreEqual(2, submenuItems.Length);
        Assert.AreEqual("Bar", submenuItems[0].TextContent.Trim());
        Assert.AreEqual("menu", submenuItems[1].GetAttribute("aria-haspopup"));

        // Two levels of submenu, and each is a callout of its own rather than a part of the one above it.
        Assert.AreEqual(2, com.FindAll(".bit-mnb-sub").Count);
    }

    [TestMethod]
    public void BitMenuButtonAnItemWithChildrenShouldNotBeALinkOrACheckItem()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Plain", Key = "plain", Checkable = true },
                new()
                {
                    Text = "Parent",
                    Key = "parent",
                    Href = "https://bitplatform.dev",
                    Checkable = true,
                    ChildItems = [new() { Text = "Child", Key = "child" }]
                }
            });
        });

        var parentItem = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1];

        Assert.AreEqual("BUTTON", parentItem.TagName);
        Assert.AreEqual("menuitem", parentItem.GetAttribute("role"));
        Assert.IsNull(parentItem.GetAttribute("aria-checked"));
        Assert.AreEqual("menu", parentItem.GetAttribute("aria-haspopup"));
    }

    [TestMethod]
    public void BitMenuButtonClickingAnItemWithChildrenShouldOpenItsSubmenuInsteadOfRaisingAClick()
    {
        var clicked = 0;
        var items = SubmenuItems();
        items[1].OnClick = _ => clicked++;

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].Click();

        var parentItem = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1];

        Assert.AreEqual(0, clicked);
        Assert.AreEqual("true", parentItem.GetAttribute("aria-expanded"));
        Assert.AreEqual(parentItem.Id, com.Find(".bit-mnb-sub ul").GetAttribute("aria-labelledby"));
        Assert.IsTrue(parentItem.ClassList.Contains("bit-mnb-osb"));
        Assert.IsNotNull(parentItem.GetAttribute("aria-controls"));

        // Clicking it again walks back out of the submenu.
        com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].Click();

        Assert.AreEqual("false", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitMenuButtonStickyShouldNotPromoteAnItemWithChildrenToItsHeader()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Parent", Key = "parent", ChildItems = [new() { Text = "Child", Key = "child" }] },
                new() { Text = "Plain", Key = "plain" }
            });
        });

        Assert.AreEqual("Plain", com.Instance.SelectedItem!.Text);
        Assert.AreEqual("Plain", com.Find(".bit-mnb-opb .bit-mnb-btx").TextContent.Trim());
    }

    [TestMethod]
    public void BitMenuButtonTheCheckColumnShouldBeReservedPerMenuRatherThanPerMenuButton()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Plain", Key = "plain" },
                new()
                {
                    Text = "Parent",
                    Key = "parent",
                    ChildItems =
                    [
                        new() { Text = "Checked", Key = "checked", Checkable = true, IsChecked = true },
                        new() { Text = "Beside it", Key = "beside" }
                    ]
                }
            });
        });

        // Nothing in the menu itself is checkable, so its rows reserve nothing.
        Assert.AreEqual(0, com.Find(".bit-mnb-cal:not(.bit-mnb-sub) > ul").QuerySelectorAll(":scope > li > .bit-mnb-itm > .bit-mnb-ick").Length);

        // The submenu has a check item, so both of its rows reserve the column and their labels line up.
        Assert.AreEqual(2, com.Find(".bit-mnb-sub ul").QuerySelectorAll(":scope > li > .bit-mnb-itm > .bit-mnb-ick").Length);
    }

    [TestMethod]
    public void BitMenuButtonSubmenuItemsShouldNotBeInTheTabSequenceOfTheMenu()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, SubmenuItems());
        });

        Assert.IsTrue(com.FindAll(".bit-mnb-itm").All(item => item.GetAttribute("tabindex") == "-1"));
    }

    [TestMethod]
    public void BitMenuButtonNestedOptionsShouldRenderAsASubmenuRatherThanAsRowsOfTheMenu()
    {
        var com = RenderComponent<BitMenuButtonSubmenuTest>();

        var rootItems = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm");

        CollectionAssert.AreEqual(
            new[] { "Text box", "Chart", "Table" },
            rootItems.Select(i => i.QuerySelector(".bit-mnb-btx")!.TextContent.Trim()).ToArray());

        Assert.AreEqual("menu", rootItems[1].GetAttribute("aria-haspopup"));

        var submenuItems = com.Find(".bit-mnb-sub ul").QuerySelectorAll(":scope > li > .bit-mnb-itm");

        CollectionAssert.AreEqual(
            new[] { "Bar", "Line" },
            submenuItems.Select(i => i.QuerySelector(".bit-mnb-btx")!.TextContent.Trim()).ToArray());

        // The check column is decided by the submenu the options are in, not by the menu around it.
        Assert.AreEqual("menuitemcheckbox", submenuItems[1].GetAttribute("role"));
        Assert.AreEqual(2, com.Find(".bit-mnb-sub ul").QuerySelectorAll(":scope > li > .bit-mnb-itm > .bit-mnb-ick").Length);
        Assert.AreEqual(0, com.Find(".bit-mnb-cal:not(.bit-mnb-sub) > ul").QuerySelectorAll(":scope > li > .bit-mnb-itm > .bit-mnb-ick").Length);
    }

    [TestMethod]
    public void BitMenuButtonGenericItemsShouldTakeTheirChildrenFromTheNameSelectors()
    {
        var com = RenderComponent<BitMenuButton<SubmenuModel>>(parameters =>
        {
            parameters.Add(p => p.NameSelectors, new BitMenuButtonNameSelectors<SubmenuModel>
            {
                Text = { Name = nameof(SubmenuModel.Label) },
                ChildItems = { Name = nameof(SubmenuModel.Children) }
            });
            parameters.Add(p => p.Items, new List<SubmenuModel>
            {
                new() { Label = "Plain" },
                new()
                {
                    Label = "Parent",
                    Children = [new() { Label = "Child" }]
                }
            });
        });

        var rootItems = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm");

        Assert.AreEqual("menu", rootItems[1].GetAttribute("aria-haspopup"));
        Assert.AreEqual("Child", com.Find(".bit-mnb-sub .bit-mnb-btx").TextContent.Trim());
    }

    [TestMethod]
    public void BitMenuButtonTheArrowKeysShouldWalkIntoAndBackOutOfASubmenu()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, SubmenuItems());
        });

        com.Find(".bit-mnb-opb").Click();

        var parentItem = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1];
        parentItem.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("true", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));

        // The key that points back at the menu the submenu was opened from closes it again, and only it.
        com.Find(".bit-mnb-sub").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual("false", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));
        Assert.IsTrue(com.Instance.IsOpen);
    }

    [TestMethod]
    public void BitMenuButtonEscapeInASubmenuShouldCloseOnlyThatSubmenu()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, SubmenuItems());
        });

        com.Find(".bit-mnb-opb").Click();
        com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].Click();

        com.Find(".bit-mnb-sub").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual("false", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));
        Assert.IsTrue(com.Instance.IsOpen);

        // Escape in the menu itself closes the menu.
        com.Find(".bit-mnb-cal:not(.bit-mnb-sub)").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(com.Instance.IsOpen);
    }

    [TestMethod]
    public void BitMenuButtonInRtlTheArrowKeysShouldBeMirrored()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Items, SubmenuItems());
        });

        com.Find(".bit-mnb-opb").Click();

        var parentItem = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1];

        // ArrowRight points back at the menu in a right-to-left one, so it does not walk into the submenu.
        parentItem.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreEqual("false", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));

        parentItem.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.AreEqual("true", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));

        com.Find(".bit-mnb-sub").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreEqual("false", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitMenuButtonOpeningASubmenuShouldCloseTheOneOpenBesideIt()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "First", Key = "first", ChildItems = [new() { Text = "One", Key = "one" }] },
                new() { Text = "Second", Key = "second", ChildItems = [new() { Text = "Two", Key = "two" }] }
            });
        });

        com.Find(".bit-mnb-opb").Click();

        com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[0].Click();
        com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].Click();

        var rootItems = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm");

        Assert.AreEqual("false", rootItems[0].GetAttribute("aria-expanded"));
        Assert.AreEqual("true", rootItems[1].GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitMenuButtonClosingTheMenuShouldCloseTheSubmenusWithIt()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, SubmenuItems());
        });

        com.Find(".bit-mnb-opb").Click();
        com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].Click();

        Assert.AreEqual("true", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));

        // The page writing IsOpen back is the menu closing, so the submenu goes with it.
        com.Render(parameters =>
        {
            parameters.Add(p => p.Items, SubmenuItems());
            parameters.Add(p => p.IsOpen, false);
        });

        Assert.AreEqual("false", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));
    }

    private class SubmenuModel
    {
        public string? Label { get; set; }

        public List<SubmenuModel> Children { get; set; } = [];
    }
}
