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
        Assert.IsTrue(root.GetAttribute("style")!.Contains("--bit-ButtonGroup-gap:1rem"));
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

    [TestMethod]
    public void BitButtonGroupShouldKeepTheToggledItemWhenItemsAreRebuiltWithNewInstances()
    {
        // A page that builds its items in a property hands the group a fresh list of fresh instances on
        // every render, so the toggled item has to be followed by its key rather than by reference.
        static List<BitButtonGroupItem> NewItems() =>
        [
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        ];

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, NewItems());
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.ToggleKey, "b");
            parameters.Add(p => p.ToggleKeyChanged, (string? _) => { });
        });

        Assert.AreEqual("true", comp.FindAll("button")[1].GetAttribute("aria-checked"));

        comp.Render(parameters => parameters.Add(p => p.Items, NewItems()));

        Assert.AreEqual("true", comp.FindAll("button")[1].GetAttribute("aria-checked"));
        Assert.AreEqual(1, comp.FindAll(".bit-btg-chk").Count);
    }

    [TestMethod]
    public void BitButtonGroupShouldKeepTheToggledItemsWhenItemsAreRebuiltWithNewInstancesInMultipleMode()
    {
        static List<BitButtonGroupItem> NewItems() =>
        [
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" },
            new() { Text = "C", Key = "c" }
        ];

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, NewItems());
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Multiple);
            parameters.Add(p => p.ToggleKeys, new[] { "a", "c" });
            parameters.Add(p => p.ToggleKeysChanged, (IEnumerable<string>? _) => { });
        });

        Assert.AreEqual(2, comp.FindAll(".bit-btg-chk").Count);

        comp.Render(parameters => parameters.Add(p => p.Items, NewItems()));

        var buttons = comp.FindAll("button");

        Assert.AreEqual("true", buttons[0].GetAttribute("aria-pressed"));
        Assert.AreEqual("false", buttons[1].GetAttribute("aria-pressed"));
        Assert.AreEqual("true", buttons[2].GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitButtonGroupShouldNotMoveTheToggleToAnotherItemWhenKeylessItemsAreRebuilt()
    {
        // An item type without a key gives the toggled item nothing to be followed by, so a rebuilt list must
        // not hand the toggle to whichever of the new items happens to lack a key first.
        static List<KeylessButtonGroupItem> NewItems() => [new(), new()];

        var comp = RenderComponent<BitButtonGroup<KeylessButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, NewItems());
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
        });

        comp.FindAll("button")[1].Click();

        Assert.AreEqual(1, comp.FindAll(".bit-btg-chk").Count);

        comp.Render(parameters => parameters.Add(p => p.Items, NewItems()));

        Assert.AreEqual(0, comp.FindAll(".bit-btg-chk").Count);
    }

    [TestMethod]
    public void BitButtonGroupNavigableShouldMoveTheTabStopBetweenKeylessItems()
    {
        // An item type without a key has none written back to it by the group either, so the tab stop it is
        // given can only be remembered by the item itself: held by the key alone it would never leave the
        // first item, and every arrow key would carry on from there rather than from the focused button.
        var items = new List<KeylessButtonGroupItem> { new(), new(), new() };

        var comp = RenderComponent<BitButtonGroup<KeylessButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        comp.FindAll("button")[2].Click();

        var buttons = comp.FindAll("button");
        Assert.AreEqual("-1", buttons[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", buttons[1].GetAttribute("tabindex"));
        Assert.AreEqual("0", buttons[2].GetAttribute("tabindex"));

        comp.Find(".bit-btg").KeyDown("ArrowLeft");

        buttons = comp.FindAll("button");
        Assert.AreEqual("-1", buttons[0].GetAttribute("tabindex"));
        Assert.AreEqual("0", buttons[1].GetAttribute("tabindex"));
        Assert.AreEqual("-1", buttons[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonGroupNavigableShouldMarkTheRootForTheKeyGuard()
    {
        // The capture-phase guard in BitButtonGroup.ts cancels the page scroll of the keys the group
        // navigates with, and reads this class to know whether the group navigates at all.
        var items = new List<BitButtonGroupItem> { new() { Text = "A" }, new() { Text = "B" } };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.IsTrue(comp.Find(".bit-btg").ClassList.Contains("bit-btg-nav"));

        comp.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Navigable, false);
        });

        Assert.IsFalse(comp.Find(".bit-btg").ClassList.Contains("bit-btg-nav"));
    }

    [TestMethod]
    public void BitButtonGroupIconOnlyShouldFallBackToTheItemTextForTheAccessibleName()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Bold", IconName = "Bold" },
            new() { Text = "Italic", IconName = "Italic", AriaLabel = "Make it italic" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IconOnly, true);
        });

        var buttons = comp.FindAll("button");

        // The text IconOnly hides is what names the button, unless an AriaLabel says otherwise.
        Assert.AreEqual("Bold", buttons[0].GetAttribute("aria-label"));
        Assert.AreEqual("Make it italic", buttons[1].GetAttribute("aria-label"));

        // A button that shows its text is named by it, so no aria-label is rendered over it.
        var labeled = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.IsNull(labeled.FindAll("button")[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitButtonGroupIconOnlyShouldFollowTheToggleStateInTheAccessibleName()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Key = "mute", OffText = "Mute", OnText = "Unmute", IconName = "Volume3" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
        });

        Assert.AreEqual("Mute", comp.Find("button").GetAttribute("aria-label"));

        comp.Find("button").Click();

        Assert.AreEqual("Unmute", comp.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitButtonGroupShouldHideTheDecorativePartsOfAButtonFromAssistiveTechnologies()
    {
        // The icon, the spinner and the check mark say nothing the button's own name does not, and a
        // screen reader reading them out would only lengthen it.
        var items = new List<BitButtonGroupItem>
        {
            new() { Key = "a", Text = "A", IconName = "Accept" },
            new() { Key = "b", Text = "B", IsLoading = true }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.ShowSelectionIndicator, true);
            parameters.Add(p => p.DefaultToggleKey, "a");
        });

        Assert.AreEqual("true", comp.Find(".bit-btg-ico").GetAttribute("aria-hidden"));
        Assert.AreEqual("true", comp.Find(".bit-btg-spn").GetAttribute("aria-hidden"));
        Assert.IsTrue(comp.FindAll(".bit-btg-sin").All(s => s.GetAttribute("aria-hidden") == "true"));
    }

    [TestMethod]
    public void BitButtonGroupLinkItemShouldBeHardenedAgainstReverseTabnabbing()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "New tab", Href = "https://bitplatform.dev", Target = "_blank" },
            new() { Text = "Tagged", Href = "https://bitplatform.dev", Target = "_blank", Rel = BitLinkRels.NoFollow },
            new() { Text = "Opener", Href = "https://bitplatform.dev", Target = "_blank", Rel = BitLinkRels.Opener },
            new() { Text = "Same tab", Href = "/components" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var links = comp.FindAll("a");

        // A link opened in a new browsing context must not hand it a reachable window.opener.
        Assert.AreEqual("noopener", links[0].GetAttribute("rel"));
        Assert.AreEqual("nofollow noopener", links[1].GetAttribute("rel"));

        // ... unless the item asks for the opposite on purpose.
        Assert.AreEqual("opener", links[2].GetAttribute("rel"));

        // A link staying in this tab has nothing to harden.
        Assert.IsNull(links[3].GetAttribute("rel"));
    }

    [TestMethod]
    public void BitButtonGroupDisabledLinkItemShouldDropItsHrefAndKeepItsRole()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Gone", Href = "/components", IsEnabled = false }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DisabledInteractive, true);
        });

        var link = comp.Find("a");

        // Without an href the anchor loses its implicit link role, which is put back explicitly.
        Assert.IsNull(link.GetAttribute("href"));
        Assert.IsNull(link.GetAttribute("rel"));
        Assert.AreEqual("link", link.GetAttribute("role"));
        Assert.AreEqual("true", link.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitButtonGroupSingleSelectionShouldCheckWhatTheArrowKeysLandOnByDefault()
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
            parameters.Add(p => p.DefaultToggleKey, "a");
        });

        comp.Find(".bit-btg").KeyDown("ArrowRight");

        // The Single mode renders a radiogroup, whose arrow keys the WAI-ARIA pattern expects to check the
        // radio they move to - so the selection follows the focus there without being asked to.
        Assert.AreEqual("true", comp.FindAll("button")[1].GetAttribute("aria-checked"));
        Assert.AreEqual("false", comp.FindAll("button")[0].GetAttribute("aria-checked"));
    }

    [TestMethod]
    public void BitButtonGroupSelectOnFocusFalseShouldMoveTheFocusWithoutCheckingAnything()
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
            parameters.Add(p => p.SelectOnFocus, false);
            parameters.Add(p => p.DefaultToggleKey, "a");
        });

        comp.Find(".bit-btg").KeyDown("ArrowRight");

        // The tab stop moved, which is the whole of what the arrow key did.
        Assert.AreEqual("0", comp.FindAll("button")[1].GetAttribute("tabindex"));
        Assert.AreEqual("true", comp.FindAll("button")[0].GetAttribute("aria-checked"));
        Assert.AreEqual("false", comp.FindAll("button")[1].GetAttribute("aria-checked"));
    }

    [TestMethod]
    public void BitButtonGroupMultipleSelectionShouldNotToggleWhileNavigating()
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
        });

        comp.Find(".bit-btg").KeyDown("ArrowRight");

        // The items of a toolbar are independent of one another, so arrowing across it presses nothing.
        Assert.IsTrue(comp.FindAll("button").All(b => b.GetAttribute("aria-pressed") == "false"));
    }

    [TestMethod]
    public void BitButtonGroupTabIndexShouldSetTheTabIndexOfTheRovingTabStop()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TabIndex, "-1");
        });

        var buttons = comp.FindAll("button");

        // The group is taken out of the tab order as a whole, and stays navigable once something focuses it.
        Assert.AreEqual("-1", buttons[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", buttons[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonGroupAutoFocusShouldBeWrittenOnTheTabStopAlone()
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
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "b");
        });

        var buttons = comp.FindAll("button");

        // The focus lands where a Tab into the group would have put it, and nowhere else.
        Assert.IsFalse(buttons[0].HasAttribute("autofocus"));
        Assert.IsTrue(buttons[1].HasAttribute("autofocus"));
        Assert.IsFalse(buttons[2].HasAttribute("autofocus"));

        var withoutAutoFocus = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.IsTrue(withoutAutoFocus.FindAll("button").All(b => b.HasAttribute("autofocus") is false));
    }

    [TestMethod]
    public void BitButtonGroupIconOnlyShouldSquareTheButtons()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Add", IconName = "Add" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IconOnly, true);
        });

        // The square shape is a class on the root, so it survives a re-render of the buttons inside it.
        Assert.IsTrue(comp.Find(".bit-btg").ClassList.Contains("bit-btg-ion"));

        comp.Render(parameters => parameters.Add(p => p.IconOnly, false));

        Assert.IsFalse(comp.Find(".bit-btg").ClassList.Contains("bit-btg-ion"));
    }

    [TestMethod]
    public void BitButtonGroupLinkItemShouldNotReportAToggleStateItsRoleDoesNotSupport()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Docs", Key = "docs", Href = "/components" },
            new() { Text = "Bold", Key = "bold" }
        };

        var multiple = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Multiple);
            parameters.Add(p => p.DefaultToggleKeys, ["docs"]);
        });

        // aria-pressed belongs to a button, so the link says the same thing in the vocabulary it does have.
        Assert.IsNull(multiple.Find("a").GetAttribute("aria-pressed"));
        Assert.AreEqual("true", multiple.Find("a").GetAttribute("aria-current"));
        Assert.AreEqual("false", multiple.Find("button").GetAttribute("aria-pressed"));

        var single = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitButtonGroupSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "docs");
        });

        // The Single mode gives every item the radio role explicitly, which does support aria-checked.
        Assert.AreEqual("radio", single.Find("a").GetAttribute("role"));
        Assert.AreEqual("true", single.Find("a").GetAttribute("aria-checked"));
        Assert.IsNull(single.Find("a").GetAttribute("aria-current"));
    }
    [TestMethod]
    public void BitButtonGroupLoadingSpinnerShouldBeValidInsideItsButton()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Save", IsLoading = true }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        // A button only takes phrasing content, which a div is not.
        Assert.AreEqual("SPAN", comp.Find(".bit-btg-spn").TagName);
    }

    [TestMethod]
    public void BitButtonGroupShouldKeepTheRoleAndLabelWrittenOnTheComponentItself()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A" }
        };

        // The unmatched attributes are captured by BitComponentBase rather than by a CaptureUnmatchedValues
        // parameter, so they are supplied as raw component attributes the way real markup writes them.
        var comp = Context.Render(builder =>
        {
            builder.OpenComponent<BitButtonGroup<BitButtonGroupItem>>(0);
            builder.AddAttribute(1, nameof(BitButtonGroup<BitButtonGroupItem>.Items), items);
            builder.AddAttribute(2, "role", "menubar");
            builder.AddAttribute(3, "aria-label", "Formatting");
            builder.CloseComponent();
        });

        var root = comp.Find(".bit-btg");

        // Everything the group writes on its root is written after the splat, and a null there would have
        // removed what the page put on the component by hand.
        Assert.AreEqual("menubar", root.GetAttribute("role"));
        Assert.AreEqual("Formatting", root.GetAttribute("aria-label"));
        Assert.AreEqual("horizontal", root.GetAttribute("aria-orientation"));

        // The parameter still wins over the splatted attribute, and the orientation follows the role: the
        // plain group role does not support it.
        var labelled = Context.Render(builder =>
        {
            builder.OpenComponent<BitButtonGroup<BitButtonGroupItem>>(0);
            builder.AddAttribute(1, nameof(BitButtonGroup<BitButtonGroupItem>.Items), items);
            builder.AddAttribute(2, nameof(BitButtonGroup<BitButtonGroupItem>.AriaLabel), "Alignment");
            builder.AddAttribute(3, "aria-label", "Formatting");
            builder.AddAttribute(4, "role", "group");
            builder.CloseComponent();
        });

        Assert.AreEqual("Alignment", labelled.Find(".bit-btg").GetAttribute("aria-label"));
        Assert.IsNull(labelled.Find(".bit-btg").GetAttribute("aria-orientation"));
    }
    [TestMethod]
    public void BitButtonGroupShouldMoveTheTabStopToAClickedButtonWhoseClickIsIgnored()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b", IsEnabled = false },
            new() { Text = "C", Key = "c" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DisabledInteractive, true);
        });

        comp.FindAll("button")[1].Click();

        // The press focused the disabled button, so the next arrow key has to carry on from there rather
        // than from the button that held the tab stop before it.
        var buttons = comp.FindAll("button");
        Assert.AreEqual("-1", buttons[0].GetAttribute("tabindex"));
        Assert.AreEqual("0", buttons[1].GetAttribute("tabindex"));
        Assert.AreEqual("-1", buttons[2].GetAttribute("tabindex"));

        // And a plain action toolbar, which toggles nothing, is left with one tab stop rather than two.
        comp.FindAll("button")[2].Click();

        buttons = comp.FindAll("button");
        Assert.AreEqual("-1", buttons[1].GetAttribute("tabindex"));
        Assert.AreEqual("0", buttons[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonGroupIconOnlyShouldKeepTheBadgeInTheAccessibleName()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Inbox", IconName = "Mail", Badge = "3" },
            new() { Text = "Drafts", IconName = "Edit" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IconOnly, true);
        });

        var buttons = comp.FindAll("button");

        // The name written to replace the hidden text carries the count the button is there to report,
        // which the text and the badge together would have read out on their own.
        Assert.AreEqual("Inbox 3", buttons[0].GetAttribute("aria-label"));
        Assert.AreEqual("Drafts", buttons[1].GetAttribute("aria-label"));

        // An explicit label is the author's, and is left exactly as it was written.
        var labelled = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem>
            {
                new() { Text = "Inbox", IconName = "Mail", Badge = "3", AriaLabel = "Inbox, 3 unread" }
            });
            parameters.Add(p => p.IconOnly, true);
        });

        Assert.AreEqual("Inbox, 3 unread", labelled.Find("button").GetAttribute("aria-label"));
    }

    public class KeylessButtonGroupItem
    {
        public string? Text { get; set; }
    }
}
