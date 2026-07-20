using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ButtonGroup;

[TestClass]
public class BitButtonGroupTests : BunitTestContext
{
    [TestMethod]
    public void BitButtonGroupShouldRenderItemsFromItemsParameter()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "One" },
            new() { Text = "Two" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var buttons = comp.FindAll("button");
        Assert.AreEqual(2, buttons.Count);
        Assert.IsTrue(buttons[0].TextContent.Contains("One"));
        Assert.IsTrue(buttons[1].TextContent.Contains("Two"));
    }

    [TestMethod]
    public void BitButtonGroupShouldInvokeOnItemClickAndItemOnClickAction()
    {
        var actionInvokedText = string.Empty;
        var onItemClickCalled = false;

        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "ClickMe", OnClick = i => actionInvokedText = i.Text },
            new() { Text = "Other" }
        };

        var component = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnItemClick, (BitButtonGroupItem it) => onItemClickCalled = true);
        });

        var btn = component.Find(".bit-btg-itm");

        btn.Click();

        Assert.IsTrue(onItemClickCalled);
        Assert.AreEqual("ClickMe", actionInvokedText);
    }

    [TestMethod]
    public void BitButtonGroupToggleDefaultKeyShouldSetToggledItemAndRaiseOnToggleChange()
    {
        string? toggledKey = null;

        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Toggle, true);
            parameters.Add(p => p.DefaultToggleKey, "b");
            parameters.Add(p => p.OnToggleChange, (BitButtonGroupItem it) => toggledKey = it?.Key);
        });

        // After initialization the default toggled item should be applied
        Assert.AreEqual("b", toggledKey);

        // The rendered button with toggled class
        var toggled = comp.FindAll(".bit-btg-chk");
        Assert.IsTrue(toggled.Count >= 1);
        Assert.IsTrue(toggled[0].TextContent.Contains("B"));

        // Click the first button to toggle
        var firstBtn = comp.FindAll("button")[0];
        firstBtn.Click();

        // Now toggledKey should change to 'a'
        Assert.AreEqual("a", toggledKey);
    }

    [TestMethod]
    public void BitButtonGroupWithoutSelectionShouldRenderNeitherAriaPressedNorAriaChecked()
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem> { new() { Text = "A" }, new() { Text = "B" } });
        });

        var button = comp.Find("button");

        Assert.IsFalse(button.HasAttribute("aria-pressed"));
        Assert.IsFalse(button.HasAttribute("aria-checked"));
        Assert.IsFalse(button.HasAttribute("role"));
    }

    [TestMethod]
    public void BitButtonGroupSingleSelectionShouldUseTheRadioGroupPattern()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "b");
        });

        Assert.AreEqual("radiogroup", comp.Find(".bit-btg").GetAttribute("role"));

        var buttons = comp.FindAll("button");

        Assert.AreEqual("radio", buttons[0].GetAttribute("role"));
        Assert.AreEqual("false", buttons[0].GetAttribute("aria-checked"));
        Assert.AreEqual("true", buttons[1].GetAttribute("aria-checked"));

        // aria-pressed belongs to the multiple selection mode only.
        Assert.IsFalse(buttons[0].HasAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitButtonGroupMultipleSelectionShouldUseTheToolbarPatternAndToggleIndependently()
    {
        IEnumerable<string>? toggleKeys = null;

        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" },
            new() { Text = "C", Key = "c" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Multiple);
            parameters.Add(p => p.ToggleKeysChanged, (IEnumerable<string>? keys) => toggleKeys = keys);
        });

        Assert.AreEqual("toolbar", comp.Find(".bit-btg").GetAttribute("role"));

        var buttons = comp.FindAll("button");

        Assert.AreEqual("false", buttons[0].GetAttribute("aria-pressed"));
        Assert.IsFalse(buttons[0].HasAttribute("aria-checked"));

        comp.FindAll("button")[2].Click();
        comp.FindAll("button")[0].Click();

        // Both stay toggled, and the keys follow the order of the items, not the order of the clicks.
        CollectionAssert.AreEqual(new[] { "a", "c" }, toggleKeys?.ToArray());
        Assert.AreEqual(2, comp.FindAll(".bit-btg-chk").Count);

        comp.FindAll("button")[0].Click();

        CollectionAssert.AreEqual(new[] { "c" }, toggleKeys?.ToArray());
    }

    [TestMethod]
    public void BitButtonGroupMaxTogglesShouldCapTheNumberOfToggledItems()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" },
            new() { Text = "C", Key = "c" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Multiple);
            parameters.Add(p => p.MaxToggles, 2);
        });

        comp.FindAll("button")[0].Click();
        comp.FindAll("button")[1].Click();
        comp.FindAll("button")[2].Click();

        Assert.AreEqual(2, comp.FindAll(".bit-btg-chk").Count);
    }

    [TestMethod]
    public void BitButtonGroupFixedToggleShouldKeepTheLastToggledItemInMultipleMode()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Multiple);
            parameters.Add(p => p.FixedToggle, true);
            parameters.Add(p => p.DefaultToggleKeys, new[] { "a" });
        });

        Assert.AreEqual(1, comp.FindAll(".bit-btg-chk").Count);

        // Un-toggling the only toggled item is rejected.
        comp.FindAll("button")[0].Click();

        Assert.AreEqual(1, comp.FindAll(".bit-btg-chk").Count);
    }

    [TestMethod]
    public void BitButtonGroupNavigableShouldMakeTheGroupASingleTabStop()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" },
            new() { Text = "C", Key = "c" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "b");
        });

        var buttons = comp.FindAll("button");

        // The toggled item owns the tabindex, the rest are removed from the tab order.
        Assert.AreEqual("-1", buttons[0].GetAttribute("tabindex"));
        Assert.AreEqual("0", buttons[1].GetAttribute("tabindex"));
        Assert.AreEqual("-1", buttons[2].GetAttribute("tabindex"));

        var notNavigable = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "b");
            parameters.Add(p => p.Navigable, false);
        });

        // Without the roving tabindex every enabled button is its own tab stop again.
        Assert.IsTrue(notNavigable.FindAll("button").All(b => b.GetAttribute("tabindex") == "0"));
    }

    [TestMethod]
    public void BitButtonGroupNavigableShouldMoveTheFocusWithTheArrowHomeAndEndKeys()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" },
            new() { Text = "C", Key = "c" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "b");
        });

        var group = comp.Find(".bit-btg");

        // The focused item owns the tabindex, so the roving tabindex is what the navigation is observed through.
        group.KeyDown("ArrowRight");
        Assert.AreEqual("0", comp.FindAll("button")[2].GetAttribute("tabindex"));

        // The navigation wraps around at both ends.
        group.KeyDown("ArrowRight");
        Assert.AreEqual("0", comp.FindAll("button")[0].GetAttribute("tabindex"));

        group.KeyDown("ArrowLeft");
        Assert.AreEqual("0", comp.FindAll("button")[2].GetAttribute("tabindex"));

        group.KeyDown("Home");
        Assert.AreEqual("0", comp.FindAll("button")[0].GetAttribute("tabindex"));

        group.KeyDown("End");
        Assert.AreEqual("0", comp.FindAll("button")[2].GetAttribute("tabindex"));

        // The vertical arrows belong to a vertical group only.
        group.KeyDown("ArrowUp");
        Assert.AreEqual("0", comp.FindAll("button")[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonGroupSelectOnFocusShouldToggleTheItemTheNavigationLandsOn()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" },
            new() { Text = "C", Key = "c" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.SelectOnFocus, true);
            parameters.Add(p => p.DefaultToggleKey, "a");
        });

        var group = comp.Find(".bit-btg");

        group.KeyDown("ArrowRight");

        // Navigating toggles the item the focus landed on, and the single selection mode keeps only that one.
        Assert.AreEqual("true", comp.FindAll("button")[1].GetAttribute("aria-checked"));
        Assert.AreEqual("false", comp.FindAll("button")[0].GetAttribute("aria-checked"));

        group.KeyDown("End");

        Assert.AreEqual("true", comp.FindAll("button")[2].GetAttribute("aria-checked"));
        Assert.AreEqual("false", comp.FindAll("button")[1].GetAttribute("aria-checked"));
    }

    [TestMethod]
    public void BitButtonGroupDisabledInteractiveShouldKeepDisabledItemsFocusable()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A" },
            new() { Text = "B", IsEnabled = false }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DisabledInteractive, true);
        });

        var disabled = comp.FindAll("button")[1];

        Assert.IsFalse(disabled.HasAttribute("disabled"));
        Assert.AreEqual("true", disabled.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitButtonGroupItemHrefShouldRenderAnAnchor()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Href = "/a", Target = "_blank" },
            new() { Text = "B" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var anchor = comp.Find("a.bit-btg-itm");

        Assert.AreEqual("/a", anchor.GetAttribute("href"));
        Assert.AreEqual("_blank", anchor.GetAttribute("target"));
        Assert.AreEqual(1, comp.FindAll("button.bit-btg-itm").Count);
    }

    [TestMethod]
    public void BitButtonGroupLoadingItemShouldRenderASpinnerAndBlockItsClick()
    {
        var clicked = false;

        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", IconName = "Add", IsLoading = true, OnClick = _ => clicked = true }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual(1, comp.FindAll(".bit-btg-spn").Count);
        Assert.AreEqual("true", comp.Find("button").GetAttribute("aria-busy"));

        comp.Find("button").Click();

        Assert.IsFalse(clicked);
    }

    [TestMethod]
    public void BitButtonGroupItemBadgeAndAriaLabelShouldBeRendered()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Inbox", Badge = "12", AriaLabel = "Inbox, 12 unread" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual("12", comp.Find(".bit-btg-bdg").TextContent);
        Assert.AreEqual("Inbox, 12 unread", comp.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitButtonGroupShouldMarkTheFirstAndLastButtonsExplicitly()
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem>
            {
                new() { Text = "A" }, new() { Text = "B" }, new() { Text = "C" }
            });
        });

        var buttons = comp.FindAll("button");

        Assert.IsTrue(buttons[0].ClassList.Contains("bit-btg-fst"));
        Assert.IsFalse(buttons[1].ClassList.Contains("bit-btg-fst"));
        Assert.IsFalse(buttons[1].ClassList.Contains("bit-btg-lst"));
        Assert.IsTrue(buttons[2].ClassList.Contains("bit-btg-lst"));
    }

    [TestMethod]
    public void BitButtonGroupLayoutParametersShouldBeReflectedInTheRootClasses()
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem> { new() { Text = "A" } });
            parameters.Add(p => p.Justified, true);
            parameters.Add(p => p.Rounded, true);
            parameters.Add(p => p.Detached, true);
            parameters.Add(p => p.Gap, "1rem");
            parameters.Add(p => p.Overflow, BitButtonGroupOverflow.Scroll);
        });

        var root = comp.Find(".bit-btg");

        Assert.IsTrue(root.ClassList.Contains("bit-btg-jst"));
        Assert.IsTrue(root.ClassList.Contains("bit-btg-rnd"));
        Assert.IsTrue(root.ClassList.Contains("bit-btg-dtc"));
        Assert.IsTrue(root.ClassList.Contains("bit-btg-scr"));
        Assert.IsFalse(root.ClassList.Contains("bit-btg-scb"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("--bit-btg-gap:1rem"));
    }

    [TestMethod]
    [DataRow(BitButtonGroupOverflow.Clip, "")]
    [DataRow(BitButtonGroupOverflow.Wrap, "bit-btg-wrp")]
    [DataRow(BitButtonGroupOverflow.Scroll, "bit-btg-scr")]
    [DataRow(BitButtonGroupOverflow.Scrollbar, "bit-btg-scb")]
    public void BitButtonGroupOverflowShouldMapToItsOwnClass(BitButtonGroupOverflow overflow, string expectedClass)
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem> { new() { Text = "A" } });
            parameters.Add(p => p.Overflow, overflow);
        });

        var root = comp.Find(".bit-btg");

        var overflowClasses = new[] { "bit-btg-wrp", "bit-btg-scr", "bit-btg-scb" };

        foreach (var cls in overflowClasses)
        {
            Assert.AreEqual(cls == expectedClass, root.ClassList.Contains(cls), cls);
        }
    }

    [TestMethod]
    public void BitButtonGroupShowSelectionIndicatorShouldReserveTheIndicatorSpaceForEveryItem()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.ShowSelectionIndicator, true);
            parameters.Add(p => p.DefaultToggleKey, "a");
        });

        // The indicator is rendered for every button so that toggling never shifts the layout.
        Assert.AreEqual(2, comp.FindAll(".bit-btg-sin").Count);
    }

    [TestMethod]
    public void BitButtonGroupVerticalShouldSetTheAriaOrientation()
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem> { new() { Text = "A" } });
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.AriaLabel, "Operations");
        });

        var root = comp.Find(".bit-btg");

        Assert.AreEqual("vertical", root.GetAttribute("aria-orientation"));
        Assert.AreEqual("Operations", root.GetAttribute("aria-label"));
    }
}
