using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "b");
            parameters.Add(p => p.Navigable, false);
        });

        // Without the roving tabindex every enabled button is its own tab stop again.
        Assert.IsTrue(notNavigable.FindAll("button").All(b => b.GetAttribute("tabindex") == "0"));
    }

    // The horizontal arrows walk the group the way it is laid out: a Dir that names no direction leaves that to the
    // page (unset) or to the content (Auto), which is read off the group for those keys alone.
    [TestMethod,
        DataRow(null, true),
        DataRow(BitDir.Auto, true),
        DataRow(BitDir.Rtl, true),
        DataRow(BitDir.Ltr, false)
    ]
    public void BitButtonGroupArrowLeftShouldFollowTheDirectionTheGroupIsLaidOutIn(BitDir? dir, bool rtl)
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetResult(true);

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
            parameters.Add(p => p.Items, [new() { Text = "A", Key = "a" }, new() { Text = "B", Key = "b" }, new() { Text = "C", Key = "c" }]);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "b");
        });

        comp.Find(".bit-btg").KeyDown("ArrowUp");
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.isRtl");

        comp.Find(".bit-btg").KeyDown("ArrowLeft");

        Assert.AreEqual("0", comp.FindAll("button")[rtl ? 2 : 0].GetAttribute("tabindex"));
        Assert.AreEqual(dir is null or BitDir.Auto, Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Utils.isRtl"));
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            new() { Text = "B", IsDisabled = true }
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
    public void BitButtonGroupDisabledInteractiveShouldNotRunTheItemsOfADisabledGroup()
    {
        // DisabledInteractive keeps the buttons of a disabled group out of the disabled attribute, so the click a key
        // press or a screen reader delivers reaches the group, which turns it away for the group's Disabled as it does
        // for an item's own IsDisabled.
        var itemClicks = 0;
        var groupClicks = 0;
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", OnClick = _ => itemClicks++ },
            new() { Text = "B", Href = "/b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.DisabledInteractive, true);
            parameters.Add(p => p.OnItemClick, (BitButtonGroupItem _) => groupClicks++);
        });

        comp.Find("button.bit-btg-itm").Click();
        comp.Find("a.bit-btg-itm").Click();

        Assert.AreEqual(0, itemClicks);
        Assert.AreEqual(0, groupClicks);
    }

    [TestMethod]
    public void BitButtonGroupDisabledGroupShouldTakeItsLinkItemsOutOfTheTabOrder()
    {
        // A link item has no disabled attribute to take it out of the tab order, so a disabled group does it for it.
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Href = "/a" },
            new() { Text = "B", Href = "/b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Disabled, true);
        });

        Assert.IsTrue(comp.FindAll("a.bit-btg-itm").All(a => a.GetAttribute("tabindex") == "-1"));
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
    [DataRow(BitColor.Primary, "bit-btg-pri")]
    [DataRow(BitColor.Error, "bit-btg-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-btg-pbg")]
    [DataRow(BitColor.TertiaryBorder, "bit-btg-tbr")]
    public void BitButtonGroupShouldRespectColor(BitColor color, string expectedClass)
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem> { new() { Text = "A" } });
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(comp.Find(".bit-btg").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-btg-sm")]
    [DataRow(BitSize.Medium, "bit-btg-md")]
    [DataRow(BitSize.Large, "bit-btg-lg")]
    public void BitButtonGroupShouldRespectSize(BitSize size, string expectedClass)
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem> { new() { Text = "A" } });
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(comp.Find(".bit-btg").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitButtonGroupShouldPublishNoColorOrSizeWhileTheyAreUnset()
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem> { new() { Text = "A" } });
        });

        // An unset Color or Size publishes nothing, so the public --bit-ButtonGroup-* variables restyle the default while
        // an explicit value - which does publish its class - wins over them.
        var root = comp.Find(".bit-btg");
        var published = new[]
        {
            "bit-btg-pri", "bit-btg-sec", "bit-btg-ter", "bit-btg-inf", "bit-btg-suc", "bit-btg-wrn", "bit-btg-swr", "bit-btg-err",
            "bit-btg-pbg", "bit-btg-sbg", "bit-btg-tbg", "bit-btg-pfg", "bit-btg-sfg", "bit-btg-tfg", "bit-btg-pbr", "bit-btg-sbr", "bit-btg-tbr",
            "bit-btg-sm", "bit-btg-md", "bit-btg-lg",
            "bit-btg-rnd",
        };

        foreach (var cssClass in published)
        {
            Assert.IsFalse(root.ClassList.Contains(cssClass), $"An unset parameter published {cssClass}.");
        }

        // The Variant keeps its default class: it is the structure of the group rather than a value a variable restyles.
        Assert.IsTrue(root.ClassList.Contains("bit-btg-fil"));
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.ShowSelectionIndicator, true);
            parameters.Add(p => p.DefaultToggleKey, "a");
        });

        Assert.AreEqual("true", comp.Find(".bit-btg-ico").GetAttribute("aria-hidden"));
        Assert.AreEqual("true", comp.Find(".bit-btg-spn").GetAttribute("aria-hidden"));
        Assert.IsTrue(comp.FindAll(".bit-btg-sin").All(s => s.GetAttribute("aria-hidden") == "true"));
    }

    [TestMethod]
    public void BitButtonGroupLinkItemShouldBeHardenedForAnyCaseOfTheBlankTarget()
    {
        // The browser matches the target keyword case-insensitively, so "_BLANK" opens a new context too.
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "New tab", Href = "https://bitplatform.dev", Target = "_BLANK" },
            new() { Text = "Tagged", Href = "https://bitplatform.dev", Target = "_Blank", Rel = BitLinkRels.NoFollow }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var links = comp.FindAll("a");

        Assert.AreEqual("noopener", links[0].GetAttribute("rel"));
        Assert.AreEqual("nofollow noopener", links[1].GetAttribute("rel"));
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
            new() { Text = "Gone", Href = "/components", IsDisabled = true }
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
            parameters.Add(p => p.DefaultToggleKeys, ["docs"]);
        });

        // aria-pressed belongs to a button, so the link says the same thing in the vocabulary it does have.
        Assert.IsNull(multiple.Find("a").GetAttribute("aria-pressed"));
        Assert.AreEqual("true", multiple.Find("a").GetAttribute("aria-current"));
        Assert.AreEqual("false", multiple.Find("button").GetAttribute("aria-pressed"));

        var single = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
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
            builder.AddAttribute(2, "role", "toolbar");
            builder.AddAttribute(3, "aria-label", "Formatting");
            builder.CloseComponent();
        });

        var root = comp.Find(".bit-btg");

        // Everything the group writes on its root is written after the splat, and a null there would have
        // removed what the page put on the component by hand.
        Assert.AreEqual("toolbar", root.GetAttribute("role"));
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
    public void BitButtonGroupShouldDropARoleWhoseItemSemanticsItDoesNotRender()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A" }
        };

        // A menubar owns menuitems and is driven by the menu keyboard pattern, while the group renders plain
        // buttons with the toolbar one. A role it renders nothing to match is dropped for the role the selection
        // mode implies, rather than announcing a pattern nothing underneath it follows.
        var generated = Context.Render(builder =>
        {
            builder.OpenComponent<BitButtonGroup<BitButtonGroupItem>>(0);
            builder.AddAttribute(1, nameof(BitButtonGroup<BitButtonGroupItem>.Items), items);
            builder.AddAttribute(2, "role", "menubar");
            builder.CloseComponent();
        });

        Assert.AreEqual("toolbar", generated.Find(".bit-btg").GetAttribute("role"));

        // Child content writes the items itself, so it is the page that decides what they are: the role stands.
        var custom = Context.Render(builder =>
        {
            builder.OpenComponent<BitButtonGroup<BitButtonGroupItem>>(0);
            builder.AddAttribute(1, "role", "menubar");
            builder.AddAttribute(2, nameof(BitButtonGroup<BitButtonGroupItem>.ChildContent), (RenderFragment)(b =>
            {
                b.OpenElement(0, "button");
                b.AddAttribute(1, "role", "menuitem");
                b.AddContent(2, "A");
                b.CloseElement();
            }));
            builder.CloseComponent();
        });

        Assert.AreEqual("menubar", custom.Find(".bit-btg").GetAttribute("role"));
    }

    [TestMethod]
    public void BitButtonGroupShouldMoveTheTabStopToAClickedButtonWhoseClickIsIgnored()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b", IsDisabled = true },
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

    [TestMethod]
    public void BitButtonGroupNavigationShouldNeverUncheckTheItemItLandsOn()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "a");
        });

        var group = comp.Find(".bit-btg");

        // Home on the first item and End on the last one land on the item that is already checked, and the arrow
        // keys of a radiogroup may not leave it with nothing checked.
        group.KeyDown("Home");

        Assert.AreEqual("true", comp.FindAll("button")[0].GetAttribute("aria-checked"));

        group.KeyDown("End");
        group.KeyDown("End");

        Assert.AreEqual("true", comp.FindAll("button")[1].GetAttribute("aria-checked"));
        Assert.AreEqual("false", comp.FindAll("button")[0].GetAttribute("aria-checked"));

        // A click is the way a Single-mode selection is taken back, and it still is.
        comp.FindAll("button")[1].Click();

        Assert.AreEqual(0, comp.FindAll(".bit-btg-chk").Count);
    }

    [TestMethod]
    public void BitButtonGroupSingleItemGroupShouldStayCheckedWhileTheArrowKeysWrapAroundIt()
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitButtonGroupItem> { new() { Text = "A", Key = "a" } });
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "a");
        });

        var group = comp.Find(".bit-btg");

        // The navigation wraps around, so in a group of one every arrow key lands back on the checked item.
        group.KeyDown("ArrowRight");
        group.KeyDown("ArrowLeft");

        Assert.AreEqual("true", comp.Find("button").GetAttribute("aria-checked"));
    }

    [TestMethod]
    public void BitButtonGroupMaxTogglesShouldReportTheCappedItemsAsDisabledAndIgnoreTheirClick()
    {
        var clicks = 0;
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" },
            new() { Text = "C", Key = "c" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
            parameters.Add(p => p.MaxToggles, 2);
            parameters.Add(p => p.DefaultToggleKeys, new[] { "a", "b" });
            parameters.Add(p => p.OnItemClick, (BitButtonGroupItem _) => clicks++);
        });

        var buttons = comp.FindAll("button");

        // The cap is reached, so the item that is not toggled cannot be toggled at all and says so - while the
        // toggled ones stay live, since un-toggling one is how the cap is made room in.
        Assert.IsNull(buttons[0].GetAttribute("aria-disabled"));
        Assert.IsNull(buttons[1].GetAttribute("aria-disabled"));
        Assert.AreEqual("true", buttons[2].GetAttribute("aria-disabled"));

        // A capped item is out of reach, not disabled: it keeps no disabled attribute, so it stays focusable and
        // the group's keyboard navigation reaches it.
        Assert.IsFalse(buttons[2].HasAttribute("disabled"));

        comp.FindAll("button")[2].Click();

        Assert.AreEqual(0, clicks);
        Assert.AreEqual(2, comp.FindAll(".bit-btg-chk").Count);

        // Un-toggling one of them brings the capped item back.
        comp.FindAll("button")[0].Click();

        Assert.IsNull(comp.FindAll("button")[2].GetAttribute("aria-disabled"));

        comp.FindAll("button")[2].Click();

        Assert.AreEqual(2, comp.FindAll(".bit-btg-chk").Count);
        Assert.AreEqual(2, clicks);
    }

    [TestMethod]
    public void BitButtonGroupShouldMoveTheTabStopToTheFocusedButton()
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
        });

        Assert.AreEqual("0", comp.FindAll("button")[0].GetAttribute("tabindex"));

        // The focus does not only arrive through the arrow keys: a screen reader moves it by itself, and the tab
        // stop follows it wherever it lands so that the next arrow key carries on from there.
        comp.FindAll("button")[2].Focus();

        Assert.AreEqual("0", comp.FindAll("button")[2].GetAttribute("tabindex"));
        Assert.AreEqual("-1", comp.FindAll("button")[0].GetAttribute("tabindex"));

        comp.Find(".bit-btg").KeyDown("ArrowRight");

        Assert.AreEqual("0", comp.FindAll("button")[0].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonGroupDisabledGroupShouldReportItselfAsDisabled()
    {
        var items = new List<BitButtonGroupItem> { new() { Text = "A", Key = "a" } };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.IsNull(comp.Find(".bit-btg").GetAttribute("aria-disabled"));

        comp.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Disabled, true);
        });

        Assert.AreEqual("true", comp.Find(".bit-btg").GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitButtonGroupShouldNotMoveTheTabStopToAnItemTheNavigationSkips()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b", Href = "/b", IsDisabled = true },
            new() { Text = "C", Key = "c" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        comp.FindAll(".bit-btg-itm")[2].Focus();

        Assert.AreEqual("0", comp.FindAll(".bit-btg-itm")[2].GetAttribute("tabindex"));

        // A disabled link has no disabled attribute to stop a pointer press with, so the press still reaches the
        // group. The tab stop belongs to the items the arrow keys navigate between, and that one is not among
        // them: left to follow the press, it would be dropped where nothing matches it and would silently reset.
        comp.FindAll(".bit-btg-itm")[1].Click();

        Assert.AreEqual("0", comp.FindAll(".bit-btg-itm")[2].GetAttribute("tabindex"));
        Assert.AreEqual("-1", comp.FindAll(".bit-btg-itm")[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", comp.FindAll(".bit-btg-itm")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonGroupClearingTheBoundToggleKeyShouldTakeTheSelectionBack()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.ToggleKey, "a");
            parameters.Add(p => p.ToggleKeyChanged, (string? _) => { });
        });

        Assert.AreEqual("true", comp.FindAll("button")[0].GetAttribute("aria-checked"));

        comp.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.ToggleKey, null);
            parameters.Add(p => p.ToggleKeyChanged, (string? _) => { });
        });

        // The key the group reports its selection through now names nothing, so neither does the group.
        Assert.IsTrue(comp.FindAll("button").All(b => b.GetAttribute("aria-checked") == "false"));

        comp.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.ToggleKey, "b");
            parameters.Add(p => p.ToggleKeyChanged, (string? _) => { });
        });

        Assert.AreEqual("true", comp.FindAll("button")[1].GetAttribute("aria-checked"));

        comp.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.ToggleKey, "no-such-item");
            parameters.Add(p => p.ToggleKeyChanged, (string? _) => { });
        });

        // A key naming no item of this group is the same thing: it is not a reason to keep showing the last one.
        Assert.IsTrue(comp.FindAll("button").All(b => b.GetAttribute("aria-checked") == "false"));
    }

    [TestMethod]
    public void BitButtonGroupBoundToggleKeysShouldBeCorrectedToWhatTheGroupCanHold()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" },
            new() { Text = "C", Key = "c" }
        };

        IEnumerable<string>? toggleKeys = null;

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
            parameters.Add(p => p.MaxToggles, 2);
            parameters.Add(p => p.ToggleKeys, (IEnumerable<string>?)["a", "b", "c"]);
            parameters.Add(p => p.ToggleKeysChanged, (IEnumerable<string>? keys) => toggleKeys = keys);
        });

        var buttons = comp.FindAll("button");
        Assert.AreEqual("true", buttons[0].GetAttribute("aria-pressed"));
        Assert.AreEqual("true", buttons[1].GetAttribute("aria-pressed"));
        Assert.AreEqual("false", buttons[2].GetAttribute("aria-pressed"));

        // The cap took the third key, and the bound value is what the page reads its own state back out of:
        // left as it was given, it would say the group holds an item it does not.
        CollectionAssert.AreEqual(new[] { "a", "b" }, toggleKeys?.ToArray());

        comp.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
            parameters.Add(p => p.MaxToggles, 2);
            parameters.Add(p => p.ToggleKeys, (IEnumerable<string>?)["b", "no-such-item"]);
            parameters.Add(p => p.ToggleKeysChanged, (IEnumerable<string>? keys) => toggleKeys = keys);
        });

        // A key naming no item is dropped the same way.
        CollectionAssert.AreEqual(new[] { "b" }, toggleKeys?.ToArray());
        Assert.AreEqual("true", comp.FindAll("button")[1].GetAttribute("aria-pressed"));
        Assert.AreEqual("false", comp.FindAll("button")[0].GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitButtonGroupSpaceShouldSelectALinkItemStandingInForARadio()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Day", Key = "day", Href = "/day" },
            new() { Text = "Week", Key = "week", Href = "/week" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.SelectOnFocus, false);
            parameters.Add(p => p.DefaultToggleKey, "day");
        });

        comp.Find(".bit-btg").KeyDown("ArrowRight");

        Assert.AreEqual("true", comp.FindAll("a")[0].GetAttribute("aria-checked"));

        comp.Find(".bit-btg").KeyDown(" ");

        // A link is followed by Enter and scrolls the page on Space, which is the right pair of keys for a link
        // and the wrong one for the radio the Single mode has made of it: a radio is checked with Space.
        Assert.AreEqual("true", comp.FindAll("a")[1].GetAttribute("aria-checked"));
        Assert.AreEqual("false", comp.FindAll("a")[0].GetAttribute("aria-checked"));
    }

    [TestMethod]
    public void BitButtonGroupSpaceShouldLeaveTheItemsThatAreNotRadiosAlone()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Bold", Key = "bold", Href = "/bold" },
            new() { Text = "Italic", Key = "italic", Href = "/italic" }
        };

        var links = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
        });

        links.Find(".bit-btg").KeyDown(" ");

        // Outside the Single mode a link is a plain link, and Space is not one of the two keys a link has.
        Assert.IsTrue(links.FindAll("a").All(a => a.GetAttribute("aria-current") is null));

        var buttonItems = new List<BitButtonGroupItem>
        {
            new() { Text = "Bold", Key = "bold" },
            new() { Text = "Italic", Key = "italic" }
        };

        var buttons = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, buttonItems);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.SelectOnFocus, false);
        });

        buttons.Find(".bit-btg").KeyDown(" ");

        // A button is activated by Space by itself, so the handler must not select it a second time.
        Assert.IsTrue(buttons.FindAll("button").All(b => b.GetAttribute("aria-checked") == "false"));
    }

    [TestMethod]
    public void BitButtonGroupTighteningMaxTogglesShouldCutWhatTheGroupAlreadyHolds()
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
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
            parameters.Add(p => p.DefaultToggleKeys, (IEnumerable<string>?)["a", "b", "c"]);
        });

        Assert.IsTrue(comp.FindAll("button").All(b => b.GetAttribute("aria-pressed") == "true"));

        comp.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
            parameters.Add(p => p.MaxToggles, 2);
        });

        // A cap tightened onto a group holding more than it allows is still a cap: left alone, the group would
        // be showing a third toggled item that nothing it does from here on would let the user reach.
        var buttons = comp.FindAll("button");
        Assert.AreEqual("true", buttons[0].GetAttribute("aria-pressed"));
        Assert.AreEqual("true", buttons[1].GetAttribute("aria-pressed"));
        Assert.AreEqual("false", buttons[2].GetAttribute("aria-pressed"));
        Assert.AreEqual("true", buttons[2].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitButtonGroupShouldLeaveItsToggleStateAloneWhileItIsDisposedTogetherWithItsOptions()
    {
        var component = RenderComponent<BitButtonGroupTeardownTest>();

        component.Render(parameters => parameters.Add(p => p.Show, false));

        Assert.AreEqual(0, component.Instance.ToggleKeyChanges.Count);
        Assert.AreEqual(0, component.Instance.ToggleKeysChanges.Count);
        Assert.AreEqual("a", component.Instance.ToggleKey);
        CollectionAssert.AreEqual(new[] { "a" }, component.Instance.ToggleKeys!.ToArray());
    }

    [TestMethod]
    public void BitButtonGroupShouldStillClearTheToggleStateWhenTheToggledOptionIsRemovedFromAGroupThatStays()
    {
        var component = RenderComponent<BitButtonGroupTeardownTest>();

        component.Render(parameters => parameters.Add(p => p.ShowFirst, false));

        CollectionAssert.AreEqual(new string?[] { null }, component.Instance.ToggleKeyChanges);
        Assert.AreEqual(1, component.Instance.ToggleKeysChanges.Count);
        Assert.AreEqual(0, component.Instance.ToggleKeysChanges[0]!.Count());
    }


    [TestMethod]
    public void BitButtonGroupShouldRenderTheBaseParametersOnItsRoot()
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, BasicItems);
            parameters.Add(p => p.Id, "the-id");
            parameters.Add(p => p.Class, "the-class");
            parameters.Add(p => p.Style, "margin: 1px;");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.ForceAnimation, true);
        });

        var root = comp.Find(".bit-btg");

        Assert.AreEqual("the-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("the-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-fam"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitButtonGroupShouldFollowTheVisibility(BitVisibility visibility, string? expectedStyle)
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, BasicItems);
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = comp.Find(".bit-btg").GetAttribute("style") ?? string.Empty;

        if (expectedStyle is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod,
        DataRow(null, "bit-btg-fil"),
        DataRow(BitVariant.Fill, "bit-btg-fil"),
        DataRow(BitVariant.Outline, "bit-btg-otl"),
        DataRow(BitVariant.Text, "bit-btg-txt")
    ]
    public void BitButtonGroupVariantShouldMapToItsRootClass(BitVariant? variant, string expectedClass)
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, BasicItems);
            parameters.Add(p => p.Variant, variant);
        });

        var root = comp.Find(".bit-btg");

        foreach (var cls in new[] { "bit-btg-fil", "bit-btg-otl", "bit-btg-txt" })
        {
            Assert.AreEqual(cls == expectedClass, root.ClassList.Contains(cls), cls);
        }
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(false, false)
    ]
    public void BitButtonGroupFullWidthAndVerticalShouldMapToTheirRootClasses(bool fullWidth, bool vertical)
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, BasicItems);
            parameters.Add(p => p.FullWidth, fullWidth);
            parameters.Add(p => p.Vertical, vertical);
        });

        var root = comp.Find(".bit-btg");

        Assert.AreEqual(fullWidth, root.ClassList.Contains("bit-btg-flw"));
        Assert.AreEqual(vertical, root.ClassList.Contains("bit-btg-vrt"));
        Assert.AreEqual(vertical ? "vertical" : "horizontal", root.GetAttribute("aria-orientation"));
    }

    [TestMethod]
    public void BitButtonGroupDisabledShouldDisableTheRootAndEveryButton()
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, BasicItems);
            parameters.Add(p => p.Disabled, true);
        });

        var root = comp.Find(".bit-btg");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));

        foreach (var button in comp.FindAll("button"))
        {
            // the native attribute and aria-disabled say the same thing, and nothing in the group holds a tab stop
            Assert.IsTrue(button.HasAttribute("disabled"));
            Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
            Assert.AreEqual("-1", button.GetAttribute("tabindex"));
        }

        // a disabled group navigates nothing
        root.KeyDown("ArrowRight");

        Assert.IsTrue(comp.FindAll("button").All(b => b.GetAttribute("tabindex") == "-1"));
    }

    [TestMethod]
    public void BitButtonGroupItemTemplateShouldRenderEveryItemAndAnItemsOwnTemplateShouldWinOverIt()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a", Template = item => builder => builder.AddMarkupContent(0, $"<em class=\"own\">{item.Text}</em>") },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ItemTemplate, (RenderFragment<BitButtonGroupItem>)(item => builder => builder.AddMarkupContent(0, $"<strong class=\"shared\">{item.Text}</strong>")));
        });

        var buttons = comp.FindAll("button");

        Assert.AreEqual("A", buttons[0].QuerySelector(".own")!.TextContent);
        Assert.IsNull(buttons[0].QuerySelector(".shared"));
        Assert.AreEqual("B", buttons[1].QuerySelector(".shared")!.TextContent);
        // a template renders the item's content itself, so the plain text and its aria-label stay out
        Assert.IsEmpty(comp.FindAll(".bit-btg-btx"));
        Assert.IsTrue(buttons.All(b => b.HasAttribute("aria-label") is false));
    }

    [TestMethod]
    public void BitButtonGroupToggledItemShouldShowItsOnTextTitleAndIcon()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Key = "mute", Text = "Sound", OffText = "Mute", OnText = "Unmute", OffTitle = "Turn the sound off", OnTitle = "Turn the sound on", OffIconName = "Volume3", OnIconName = "Volume0" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
        });

        var button = comp.Find("button");

        Assert.AreEqual("Mute", comp.Find(".bit-btg-btx").TextContent);
        Assert.AreEqual("Turn the sound off", button.GetAttribute("title"));
        StringAssert.Contains(comp.Find(".bit-btg-ico").ClassName, "bit-icon--Volume3");

        button.Click();

        Assert.AreEqual("Unmute", comp.Find(".bit-btg-btx").TextContent);
        Assert.AreEqual("Turn the sound on", comp.Find("button").GetAttribute("title"));
        StringAssert.Contains(comp.Find(".bit-btg-ico").ClassName, "bit-icon--Volume0");
        Assert.IsTrue(comp.Find("button").ClassList.Contains("bit-btg-chk"));
    }

    [TestMethod]
    public void BitButtonGroupWithoutASelectionShouldIgnoreTheToggleStateTexts()
    {
        // Outside the selection modes nothing is ever toggled, so the plain Text, Title and icon are what is rendered.
        var items = new List<BitButtonGroupItem>
        {
            new() { Key = "mute", Text = "Sound", Title = "Sound", IconName = "Volume2", OffText = "Mute", OffTitle = "Off", OffIconName = "Volume3" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual("Sound", comp.Find(".bit-btg-btx").TextContent);
        Assert.AreEqual("Sound", comp.Find("button").GetAttribute("title"));
        Assert.IsTrue(comp.Find(".bit-btg-ico").ClassList.Contains("bit-icon--Volume2"));
    }

    [TestMethod]
    public void BitButtonGroupItemShouldCarryItsOwnClassStyleIconAndReversedIcon()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Heart", Icon = BitIconInfo.Fa("solid heart"), Class = "item-class", Style = "color: red;", ReversedIcon = true }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var button = comp.Find("button");

        Assert.IsTrue(button.ClassList.Contains("item-class"));
        Assert.IsTrue(button.ClassList.Contains("bit-btg-rvi"));
        StringAssert.Contains(button.GetAttribute("style"), "color: red");
        Assert.IsTrue(comp.Find(".bit-btg-ico").ClassList.Contains("fa-heart"));
    }

    [TestMethod]
    public void BitButtonGroupShouldApplyTheClassesAndStylesOfEveryPart()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "Inbox", Key = "a", IconName = "Mail", Badge = "3" },
            new() { Text = "Busy", Key = "b", IsLoading = true }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.ShowSelectionIndicator, true);
            parameters.Add(p => p.DefaultToggleKey, "a");
            parameters.Add(p => p.Classes, new BitButtonGroupClassStyles
            {
                Icon = "c-icon",
                Badge = "c-badge",
                Spinner = "c-spinner",
                SelectionIndicator = "c-indicator",
                ToggledButton = "c-toggled",
            });
            parameters.Add(p => p.Styles, new BitButtonGroupClassStyles
            {
                Icon = "color: red;",
                Badge = "color: blue;",
                Spinner = "color: green;",
                SelectionIndicator = "color: gray;",
                ToggledButton = "color: gold;",
            });
        });

        var buttons = comp.FindAll("button");

        Assert.IsTrue(comp.Find(".bit-btg-ico").ClassList.Contains("c-icon"));
        Assert.AreEqual("color: red;", comp.Find(".bit-btg-ico").GetAttribute("style"));
        Assert.IsTrue(comp.Find(".bit-btg-bdg").ClassList.Contains("c-badge"));
        Assert.AreEqual("color: blue;", comp.Find(".bit-btg-bdg").GetAttribute("style"));
        Assert.IsTrue(comp.Find(".bit-btg-spn").ClassList.Contains("c-spinner"));
        Assert.AreEqual("color: green;", comp.Find(".bit-btg-spn").GetAttribute("style"));
        Assert.IsTrue(comp.FindAll(".bit-btg-sin").All(s => s.ClassList.Contains("c-indicator") && s.GetAttribute("style") == "color: gray;"));

        // the toggled class and style reach the toggled button alone
        Assert.IsTrue(buttons[0].ClassList.Contains("c-toggled"));
        StringAssert.Contains(buttons[0].GetAttribute("style"), "color: gold");
        Assert.IsFalse(buttons[1].ClassList.Contains("c-toggled"));
        Assert.IsFalse((buttons[1].GetAttribute("style") ?? string.Empty).Contains("gold"));
    }

    [TestMethod]
    public void BitButtonGroupShouldReadACustomItemTypeThroughTheDefaultPropertyNames()
    {
        var items = new List<CustomButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" },
            new() { Text = "C", Key = "c", IsDisabled = true },
            new() { Text = "D", Key = "d", Href = "/d", Target = "_blank" }
        };

        var comp = RenderComponent<BitButtonGroup<CustomButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, new BitButtonGroupNameSelectors<CustomButtonGroupItem>());
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "b");
        });

        var controls = comp.FindAll(".bit-btg-itm");

        Assert.AreEqual(4, controls.Count);
        Assert.AreEqual("B", controls[1].QuerySelector(".bit-btg-btx")!.TextContent);
        Assert.AreEqual("true", controls[1].GetAttribute("aria-checked"));
        // the toggle state is written back to the item through the selector
        Assert.IsTrue(items[1].IsToggled);
        Assert.IsFalse(items[0].IsToggled);
        Assert.IsTrue(controls[2].HasAttribute("disabled"));
        Assert.AreEqual("A", controls[3].TagName);
        Assert.AreEqual("/d", controls[3].GetAttribute("href"));
        Assert.AreEqual("noopener", controls[3].GetAttribute("rel"));
    }

    [TestMethod]
    public async Task BitButtonGroupFocusAsyncShouldFocusTheButtonHoldingTheTabStop()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Add(p => p.DefaultToggleKey, "b");
        });

        await comp.Instance.FocusAsync();

        Assert.HasCount(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void BitButtonGroupVerticalShouldNavigateWithTheVerticalArrowsAlone()
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
            parameters.Add(p => p.Vertical, true);
        });

        var group = comp.Find(".bit-btg");

        group.KeyDown("ArrowDown");
        Assert.AreEqual("0", comp.FindAll("button")[1].GetAttribute("tabindex"));

        group.KeyDown("ArrowUp");
        Assert.AreEqual("0", comp.FindAll("button")[0].GetAttribute("tabindex"));

        // wraps around at the top, and the horizontal arrows are left to the page
        group.KeyDown("ArrowUp");
        Assert.AreEqual("0", comp.FindAll("button")[2].GetAttribute("tabindex"));

        group.KeyDown("ArrowRight");
        Assert.AreEqual("0", comp.FindAll("button")[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonGroupShouldLeaveTheModifiedKeysAndANonNavigableGroupAlone()
    {
        var items = new List<BitButtonGroupItem>
        {
            new() { Text = "A", Key = "a" },
            new() { Text = "B", Key = "b" }
        };

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        comp.Find(".bit-btg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });
        comp.Find(".bit-btg").KeyDown(new KeyboardEventArgs { Key = "End", AltKey = true });

        Assert.AreEqual("0", comp.FindAll("button")[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", comp.FindAll("button")[1].GetAttribute("tabindex"));

        var notNavigable = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Navigable, false);
        });

        notNavigable.Find(".bit-btg").KeyDown("ArrowRight");

        // every button is its own tab stop, and the keys move nothing
        Assert.IsTrue(notNavigable.FindAll("button").All(b => b.GetAttribute("tabindex") == "0"));
    }

    [TestMethod]
    public void BitButtonGroupSingleSelectionShouldWriteTheBoundToggleKeyBack()
    {
        string? toggleKey = null;
        var changes = 0;

        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, BasicItemsWithKeysAAndB);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Single);
            parameters.Bind(p => p.ToggleKey, toggleKey, v => toggleKey = v);
            parameters.Add(p => p.OnToggleChange, (BitButtonGroupItem _) => changes++);
        });

        comp.FindAll("button")[1].Click();

        Assert.AreEqual("b", toggleKey);
        Assert.AreEqual(1, changes);

        // clicking the toggled item takes the selection back, and the bound key says so
        comp.FindAll("button")[1].Click();

        Assert.IsNull(toggleKey);
        Assert.AreEqual(2, changes);
        Assert.IsEmpty(comp.FindAll(".bit-btg-chk"));
    }

    [TestMethod]
    public void BitButtonGroupToggleShouldBeTheSingleSelectionModeInShort()
    {
        var comp = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, BasicItemsWithKeysAAndB);
            parameters.Add(p => p.Toggle, true);
        });

        Assert.AreEqual("radiogroup", comp.Find(".bit-btg").GetAttribute("role"));
        Assert.IsTrue(comp.FindAll("button").All(b => b.GetAttribute("role") == "radio"));

        // an explicit selection mode wins over the shorthand
        var multiple = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, BasicItemsWithKeysAAndB);
            parameters.Add(p => p.Toggle, true);
            parameters.Add(p => p.SelectionMode, BitSelectionMode.Multiple);
        });

        Assert.AreEqual("toolbar", multiple.Find(".bit-btg").GetAttribute("role"));
    }

    [TestMethod]
    public void BitButtonGroupOptionsShouldToggleAndRaiseTheirOwnClick()
    {
        BitButtonGroupOption? clicked = null;

        var comp = Context.Render(builder =>
        {
            builder.OpenComponent<BitButtonGroup<BitButtonGroupOption>>(0);
            builder.AddAttribute(1, nameof(BitButtonGroup<BitButtonGroupOption>.Toggle), true);
            builder.AddAttribute(2, nameof(BitButtonGroup<BitButtonGroupOption>.DefaultToggleKey), "b");
            builder.AddAttribute(3, nameof(BitButtonGroup<BitButtonGroupOption>.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<BitButtonGroupOption>(0);
                b.AddAttribute(1, nameof(BitButtonGroupOption.Text), "A");
                b.AddAttribute(2, nameof(BitButtonGroupOption.Key), "a");
                b.AddAttribute(3, nameof(BitButtonGroupOption.OnClick), EventCallback.Factory.Create<BitButtonGroupOption>(this, o => clicked = o));
                b.CloseComponent();

                b.OpenComponent<BitButtonGroupOption>(4);
                b.AddAttribute(5, nameof(BitButtonGroupOption.Text), "B");
                b.AddAttribute(6, nameof(BitButtonGroupOption.Key), "b");
                b.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var buttons = comp.FindAll("button");

        Assert.AreEqual("true", buttons[1].GetAttribute("aria-checked"));
        Assert.AreEqual("false", buttons[0].GetAttribute("aria-checked"));

        comp.FindAll("button")[0].Click();

        Assert.IsNotNull(clicked);
        Assert.AreEqual("a", clicked!.Key);
        Assert.IsTrue(clicked.IsToggled);
        Assert.AreEqual("true", comp.FindAll("button")[0].GetAttribute("aria-checked"));
        Assert.AreEqual("false", comp.FindAll("button")[1].GetAttribute("aria-checked"));
    }

    private static List<BitButtonGroupItem> BasicItems =>
    [
        new() { Text = "Bold", Key = "bold" },
        new() { Text = "Italic", Key = "italic" }
    ];

    private static List<BitButtonGroupItem> BasicItemsWithKeysAAndB =>
    [
        new() { Text = "A", Key = "a" },
        new() { Text = "B", Key = "b" }
    ];

    public class KeylessButtonGroupItem
    {
        public string? Text { get; set; }
    }

    public class CustomButtonGroupItem
    {
        public string? Text { get; set; }

        public string? Key { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsToggled { get; set; }

        public string? Href { get; set; }

        public string? Target { get; set; }
    }
}
