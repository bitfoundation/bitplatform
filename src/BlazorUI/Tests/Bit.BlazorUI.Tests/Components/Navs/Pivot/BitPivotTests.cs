using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Components.Navs.Pivot;

[TestClass]
public class BitPivotTests : BunitTestContext
{
    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod,
         DataRow(BitPivotHeaderType.Link, BitSize.Large, BitPivotOverflowBehavior.None),
         DataRow(BitPivotHeaderType.Tab, BitSize.Medium, BitPivotOverflowBehavior.Scroll),
         DataRow(BitPivotHeaderType.Tab, BitSize.Small, BitPivotOverflowBehavior.Menu),
         DataRow(BitPivotHeaderType.Link, BitSize.Small, BitPivotOverflowBehavior.Slide)
     ]
    public void BitPivotShouldRespectParameterRelatedCssClasses(BitPivotHeaderType headerType, BitSize size, BitPivotOverflowBehavior overflowBehavior)
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Size, size);
            parameters.Add(p => p.HeaderType, headerType);
            parameters.Add(p => p.OverflowBehavior, overflowBehavior);
        });

        var sizeClass = $"bit-pvt-{size switch { BitSize.Small => "sm", BitSize.Medium => "md", BitSize.Large => "lg", _ => "md" }}";
        var headerTypeClass = $"bit-pvt-{headerType switch { BitPivotHeaderType.Link => "lnk", BitPivotHeaderType.Tab => "tab", _ => "lnk" }}";
        var overflowBehaviorClass = $"bit-pvt-{overflowBehavior switch { BitPivotOverflowBehavior.Menu => "mnu", BitPivotOverflowBehavior.Scroll => "scr", BitPivotOverflowBehavior.Slide => "sld", BitPivotOverflowBehavior.None => "non", _ => "non" }}";

        var bitPivot = component.Find(".bit-pvt");

        Assert.IsTrue(bitPivot.ClassList.Contains(sizeClass));
        Assert.IsTrue(bitPivot.ClassList.Contains(headerTypeClass));
        Assert.IsTrue(bitPivot.ClassList.Contains(overflowBehaviorClass));
    }

    [TestMethod,
         DataRow(BitPivotPosition.Top),
         DataRow(BitPivotPosition.Bottom),
         DataRow(BitPivotPosition.Start),
         DataRow(BitPivotPosition.End)
    ]
    public void BitPivotShouldRespectPosition(BitPivotPosition position)
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Position, position);
        });

        var positionClass = position switch
        {
            BitPivotPosition.Top => "bit-pvt-top",
            BitPivotPosition.Bottom => "bit-pvt-btm",
            BitPivotPosition.Start => "bit-pvt-sta",
            BitPivotPosition.End => "bit-pvt-end",
            _ => string.Empty
        };

        var bitPivot = component.Find(".bit-pvt");

        Assert.IsTrue(bitPivot.ClassList.Contains(positionClass));
    }

    [TestMethod,
         DataRow(BitColor.Primary, "bit-pvt-pri"),
         DataRow(BitColor.Success, "bit-pvt-suc"),
         DataRow(BitColor.Error, "bit-pvt-err"),
         DataRow(BitColor.TertiaryBorder, "bit-pvt-tbr")
    ]
    public void BitPivotShouldRespectColor(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-pvt").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitPivotShouldRespectFullWidthAndStackedAndDismissible()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.FullWidth, true);
            parameters.Add(p => p.Stacked, true);
            parameters.Add(p => p.Dismissible, true);
        });

        var bitPivot = component.Find(".bit-pvt");

        Assert.IsTrue(bitPivot.ClassList.Contains("bit-pvt-fwd"));
        Assert.IsTrue(bitPivot.ClassList.Contains("bit-pvt-stk"));
        Assert.IsTrue(bitPivot.ClassList.Contains("bit-pvt-dsm"));
    }

    [TestMethod,
         DataRow(BitAlignment.Start, "flex-start"),
         DataRow(BitAlignment.Center, "center"),
         DataRow(BitAlignment.End, "flex-end"),
         DataRow(BitAlignment.SpaceBetween, "space-between")
    ]
    public void BitPivotShouldRespectAlignment(BitAlignment alignment, string expectedValue)
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Alignment, alignment);
        });

        Assert.IsTrue(component.Find(".bit-pvt").GetAttribute("style")!.Contains($"--bit-pvt-hal:{expectedValue}"));
    }



    [TestMethod]
    public void BitPivotShouldFollowTheAriaTabsPattern()
    {
        var component = RenderPivot(3);

        // The tablist is the header, not the root: a role in between would break the association
        // between the tabs and the panel they control.
        var tablist = component.Find(".bit-pvt-hct");
        Assert.AreEqual("tablist", tablist.GetAttribute("role"));
        Assert.AreEqual("horizontal", tablist.GetAttribute("aria-orientation"));
        Assert.IsNull(component.Find(".bit-pvt").GetAttribute("role"));

        var tabs = component.FindAll("[role=tab]");
        Assert.AreEqual(3, tabs.Count);
        Assert.AreEqual("true", tabs[0].GetAttribute("aria-selected"));
        Assert.AreEqual("false", tabs[1].GetAttribute("aria-selected"));

        var panel = component.Find("[role=tabpanel]");
        Assert.AreEqual("0", panel.GetAttribute("tabindex"));
        Assert.AreEqual(tabs[0].Id, panel.GetAttribute("aria-labelledby"));
        Assert.AreEqual(panel.Id, tabs[0].GetAttribute("aria-controls"));

        // Only the selected tab has a panel to point at while the others are not rendered at all.
        Assert.IsNull(tabs[1].GetAttribute("aria-controls"));
    }

    [TestMethod]
    public void BitPivotShouldReportAVerticalOrientationForAVerticalPosition()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Position, BitPivotPosition.Start);
        });

        Assert.AreEqual("vertical", component.Find(".bit-pvt-hct").GetAttribute("aria-orientation"));
    }

    [TestMethod]
    public void BitPivotShouldPutItsAriaLabelOnTheTablist()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Workspace sections");
        });

        Assert.AreEqual("Workspace sections", component.Find(".bit-pvt-hct").GetAttribute("aria-label"));
    }

    [TestMethod, DataRow("Detailed label")]
    public void BitPivotAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent<BitPivotItem>();
            parameters.AddChildContent<BitPivotItem>(parameters => parameters.Add(p => p.AriaLabel, ariaLabel));
        });

        var items = com.FindAll("[role=tab]");

        Assert.AreEqual(ariaLabel, items[1].GetAttribute("aria-label"));
    }

    [TestMethod, DataRow("More options"), DataRow(null)]
    public void BitPivotOverflowAriaLabelTest(string? overflowAriaLabel)
    {
        var com = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.OverflowBehavior, BitPivotOverflowBehavior.Menu);
            parameters.Add(p => p.OverflowAriaLabel, overflowAriaLabel);
            parameters.AddChildContent<BitPivotItem>();
        });

        var overflowButton = com.Find(".bit-pvt-mor");

        Assert.AreEqual(overflowAriaLabel ?? "More", overflowButton.GetAttribute("aria-label"));
    }

    [TestMethod, DataRow("Next label"), DataRow(null)]
    public void BitPivotSlideAriaLabelsTest(string? label)
    {
        var com = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.OverflowBehavior, BitPivotOverflowBehavior.Slide);
            parameters.Add(p => p.NextAriaLabel, label);
            parameters.Add(p => p.PreviousAriaLabel, label);
            parameters.AddChildContent<BitPivotItem>();
        });

        Assert.AreEqual(label ?? "Previous", com.Find(".bit-pvt-spv").GetAttribute("aria-label"));
        Assert.AreEqual(label ?? "Next", com.Find(".bit-pvt-snx").GetAttribute("aria-label"));
    }



    [TestMethod]
    public void BitPivotShouldSelectTheFirstItemByDefault()
    {
        var component = RenderPivot(3);

        Assert.AreEqual("true", component.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));
        Assert.IsTrue(component.FindAll("[role=tab]")[0].ClassList.Contains("bit-pvti-sel"));
    }

    [TestMethod]
    public void BitPivotShouldSkipADisabledFirstItemWhenPickingTheInitialSelection()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.IsEnabled, false).Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
        });

        var tabs = component.FindAll("[role=tab]");

        Assert.AreEqual("false", tabs[0].GetAttribute("aria-selected"));
        Assert.AreEqual("true", tabs[1].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPivotShouldSelectTheItemOfTheDefaultSelectedKey()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.DefaultSelectedKey, "b");
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "a"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "b"));
        });

        Assert.AreEqual("true", component.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPivotShouldFallBackToTheFirstItemWhenTheKeyMatchesNothing()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.DefaultSelectedKey, "nothing");
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "a"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "b"));
        });

        // A key nobody answers to would otherwise leave the pivot showing an empty panel.
        Assert.AreEqual("true", component.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPivotShouldSelectTheItemThatDeclaresItselfSelected()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "a"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "b").Add(i => i.IsSelected, true));
        });

        var tabs = component.FindAll("[role=tab]");

        Assert.AreEqual("false", tabs[0].GetAttribute("aria-selected"));
        Assert.AreEqual("true", tabs[1].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPivotShouldSelectTheClickedItemAndRaiseItsCallbacks()
    {
        BitPivotItem? changed = null;
        BitPivotItem? clicked = null;

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.OnChange, (BitPivotItem i) => changed = i);
            parameters.Add(p => p.OnItemClick, (BitPivotItem i) => clicked = i);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
        });

        component.FindAll("[role=tab]")[1].Click();

        Assert.AreEqual("true", component.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
        Assert.AreEqual("B", changed?.HeaderText);
        Assert.AreEqual("B", clicked?.HeaderText);
    }

    [TestMethod]
    public void BitPivotShouldNotRaiseOnChangeForTheItemThatIsAlreadySelected()
    {
        var changeCount = 0;
        var clickCount = 0;

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.OnChange, (BitPivotItem _) => changeCount++);
            parameters.Add(p => p.OnItemClick, (BitPivotItem _) => clickCount++);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
        });

        component.FindAll("[role=tab]")[0].Click();

        // Clicking the selected tab is still a click, but nothing about the selection changed.
        Assert.AreEqual(0, changeCount);
        Assert.AreEqual(1, clickCount);
    }

    [TestMethod]
    public void BitPivotShouldRaiseTheOnClickOfTheItemItself()
    {
        var count = 0;

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B").Add(i => i.OnClick, () => count++));
        });

        component.FindAll("[role=tab]")[1].Click();

        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void BitPivotShouldNotSelectADisabledItem()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B").Add(i => i.IsEnabled, false));
        });

        var tabs = component.FindAll("[role=tab]");

        Assert.AreEqual("true", tabs[1].GetAttribute("aria-disabled"));
        Assert.IsNull(tabs[0].GetAttribute("aria-disabled"));

        tabs[1].Click();

        Assert.AreEqual("true", component.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPivotShouldNotSelectAnythingWhenTheWholePivotIsDisabled()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
        });

        component.FindAll("[role=tab]")[1].Click();

        Assert.AreEqual("true", component.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPivotShouldFollowTheBoundSelectedKeyInBothDirections()
    {
        var key = "a";

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Bind(p => p.SelectedKey, key, v => key = v!);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "a"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "b"));
        });

        component.FindAll("[role=tab]")[1].Click();

        Assert.AreEqual("b", key);

        component.Render(parameters => parameters.Bind(p => p.SelectedKey, "a", v => key = v!));

        Assert.AreEqual("true", component.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPivotShouldRevertABoundKeyThatCannotBeHonored()
    {
        var key = "a";

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Bind(p => p.SelectedKey, key, v => key = v!);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "a"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "b").Add(i => i.IsEnabled, false));
        });

        component.Render(parameters => parameters.Bind(p => p.SelectedKey, "b", v => key = v!));

        // The disabled tab refuses the selection, so the bound value goes back to the tab that holds it.
        Assert.AreEqual("a", key);
        Assert.AreEqual("true", component.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));
    }



    [TestMethod]
    public void BitPivotShouldHoldASingleTabStopWithTheSelectedItemOwningIt()
    {
        var component = RenderPivot(3);

        var tabs = component.FindAll("[role=tab]");

        Assert.AreEqual("0", tabs[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", tabs[1].GetAttribute("tabindex"));
        Assert.AreEqual("-1", tabs[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPivotNotNavigableShouldGiveEveryItemItsOwnTabStop()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Navigable, false);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "C").Add(i => i.IsEnabled, false));
        });

        var tabs = component.FindAll("[role=tab]");

        Assert.AreEqual("0", tabs[0].GetAttribute("tabindex"));
        Assert.AreEqual("0", tabs[1].GetAttribute("tabindex"));
        Assert.AreEqual("-1", tabs[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPivotShouldMoveTheFocusWithTheArrowHomeAndEndKeys()
    {
        var component = RenderPivot(3);

        var tablist = component.Find(".bit-pvt-hct");

        // The focused item owns the tabindex, so the roving tabindex is what the navigation is observed through.
        tablist.KeyDown("ArrowRight");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[1].GetAttribute("tabindex"));

        tablist.KeyDown("ArrowRight");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[2].GetAttribute("tabindex"));

        // The navigation wraps around at both ends.
        tablist.KeyDown("ArrowRight");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[0].GetAttribute("tabindex"));

        tablist.KeyDown("ArrowLeft");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[2].GetAttribute("tabindex"));

        tablist.KeyDown("Home");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[0].GetAttribute("tabindex"));

        tablist.KeyDown("End");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[2].GetAttribute("tabindex"));

        // The vertical arrows belong to a vertical header only.
        tablist.KeyDown("ArrowUp");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPivotShouldNavigateAVerticalHeaderWithTheVerticalArrows()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Position, BitPivotPosition.Start);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
        });

        var tablist = component.Find(".bit-pvt-hct");

        tablist.KeyDown("ArrowRight");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[0].GetAttribute("tabindex"));

        tablist.KeyDown("ArrowDown");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPivotShouldMirrorTheHorizontalArrowsInARightToLeftHeader()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "C"));
        });

        var tablist = component.Find(".bit-pvt-hct");

        tablist.KeyDown("ArrowLeft");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[1].GetAttribute("tabindex"));

        tablist.KeyDown("ArrowRight");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[0].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPivotShouldStepOverTheItemsThatCannotTakeTheFocus()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B").Add(i => i.IsEnabled, false));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "C").Add(i => i.Visibility, BitVisibility.Collapsed));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "D"));
        });

        component.Find(".bit-pvt-hct").KeyDown("ArrowRight");

        Assert.AreEqual("0", component.FindAll("[role=tab]")[3].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPivotShouldSelectTheFocusedItemWithEnterAndSpace()
    {
        var component = RenderPivot(3);

        var tablist = component.Find(".bit-pvt-hct");

        tablist.KeyDown("ArrowRight");

        // Manual activation: the navigation moved the focus, nothing is selected until a key says so.
        Assert.AreEqual("false", component.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));

        tablist.KeyDown("Enter");
        Assert.AreEqual("true", component.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));

        tablist.KeyDown("End");
        tablist.KeyDown(" ");
        Assert.AreEqual("true", component.FindAll("[role=tab]")[2].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPivotSelectOnFocusShouldSelectTheItemTheNavigationLandsOn()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.SelectOnFocus, true);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "C"));
        });

        var tablist = component.Find(".bit-pvt-hct");

        tablist.KeyDown("ArrowRight");
        Assert.AreEqual("true", component.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));

        tablist.KeyDown("End");
        Assert.AreEqual("true", component.FindAll("[role=tab]")[2].GetAttribute("aria-selected"));
    }



    [TestMethod]
    public void BitPivotShouldRenderADismissButtonOnlyWhereItIsAskedFor()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B").Add(i => i.Dismissible, false));
        });

        Assert.AreEqual(1, component.FindAll(".bit-pvti-dbt").Count);
        Assert.AreEqual("Remove A", component.Find(".bit-pvti-dbt").GetAttribute("aria-label"));
        Assert.AreEqual("-1", component.Find(".bit-pvti-dbt").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPivotShouldRaiseOnItemDismissFromTheButtonAndFromTheDeleteKey()
    {
        var dismissed = new List<string?>();

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.OnItemDismiss, (BitPivotItem i) => dismissed.Add(i.HeaderText));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
        });

        component.FindAll(".bit-pvti-dbt")[1].Click();

        // The dismiss button never changes the selection along the way.
        Assert.AreEqual(1, dismissed.Count);
        Assert.AreEqual("B", dismissed[0]);
        Assert.AreEqual("true", component.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));

        component.Find(".bit-pvt-hct").KeyDown("Delete");

        Assert.AreEqual(2, dismissed.Count);
        Assert.AreEqual("A", dismissed[1]);
    }

    [TestMethod]
    public void BitPivotShouldNotDismissAnItemThatIsNotDismissible()
    {
        var count = 0;

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.OnItemDismiss, (BitPivotItem _) => count++);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
        });

        component.Find(".bit-pvt-hct").KeyDown("Delete");

        Assert.AreEqual(0, count);
        Assert.AreEqual(0, component.FindAll(".bit-pvti-dbt").Count);
    }

    [TestMethod]
    public void BitPivotShouldMoveTheSelectionOffAnItemThatIsRemoved()
    {
        var tabs = new List<string> { "a", "b", "c" };

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent(builder =>
            {
                foreach (var tab in tabs)
                {
                    builder.OpenComponent<BitPivotItem>(0);
                    builder.SetKey(tab);
                    builder.AddComponentParameter(1, nameof(BitPivotItem.Key), tab);
                    builder.AddComponentParameter(2, nameof(BitPivotItem.HeaderText), tab);
                    builder.CloseComponent();
                }
            });
        });

        component.FindAll("[role=tab]")[1].Click();
        Assert.AreEqual("true", component.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));

        tabs.Remove("b");
        component.Render();

        // The selected tab left, so the selection moved to the one that took its place.
        var remaining = component.FindAll("[role=tab]");
        Assert.AreEqual(2, remaining.Count);
        Assert.AreEqual("true", remaining[1].GetAttribute("aria-selected"));
    }



    [TestMethod]
    public void BitPivotHeaderOnlyShouldNotRenderAPanel()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.HeaderOnly, true);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
        });

        Assert.AreEqual(0, component.FindAll("[role=tabpanel]").Count);
        Assert.IsNull(component.Find("[role=tab]").GetAttribute("aria-controls"));
    }

    [TestMethod]
    public void BitPivotMountAllShouldRenderEveryPanelAndHideTheOnesThatAreNotSelected()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.MountAll, true);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A").AddChildContent("first"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B").AddChildContent("second"));
        });

        var panels = component.FindAll("[role=tabpanel]");

        Assert.AreEqual(2, panels.Count);
        Assert.IsFalse(panels[0].HasAttribute("hidden"));
        Assert.IsNull(panels[0].GetAttribute("aria-hidden"));
        Assert.IsTrue(panels[1].HasAttribute("hidden"));
        Assert.AreEqual("true", panels[1].GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitPivotShouldRenderTheHeaderStartAndHeaderEndContent()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.HeaderStart, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span id=\"hs\"></span>")));
            parameters.Add(p => p.HeaderEnd, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span id=\"he\"></span>")));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
        });

        Assert.AreEqual(1, component.FindAll(".bit-pvt-hst #hs").Count);
        Assert.AreEqual(1, component.FindAll(".bit-pvt-hnd #he").Count);
    }

    [TestMethod]
    public void BitPivotItemShouldRenderItsIconCountAndTitle()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent<BitPivotItem>(p => p
                .Add(i => i.HeaderText, "A")
                .Add(i => i.IconName, "Info")
                .Add(i => i.ItemCount, 12)
                .Add(i => i.Title, "The first tab"));
        });

        var tab = component.Find("[role=tab]");

        Assert.AreEqual("The first tab", tab.GetAttribute("title"));
        Assert.AreEqual("(12)", component.Find(".bit-pvti-cnr").TextContent);
        Assert.AreEqual(1, component.FindAll(".bit-icon--Info").Count);

        // The invisible bold copy that keeps the tab from changing width when it becomes selected.
        Assert.AreEqual("A", component.Find(".bit-pvti-txt").GetAttribute("data-content"));
    }

    [TestMethod]
    public void BitPivotShouldApplyItsClassesAndStyles()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitPivotClassStyles
            {
                Root = "custom-root",
                Header = "custom-header",
                HeaderItem = "custom-item",
                SelectedItem = "custom-selected",
                Body = "custom-body"
            });
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
        });

        Assert.IsTrue(component.Find(".bit-pvt").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-pvt-hct").ClassList.Contains("custom-header"));
        Assert.IsTrue(component.Find("[role=tab]").ClassList.Contains("custom-item"));
        Assert.IsTrue(component.Find("[role=tab]").ClassList.Contains("custom-selected"));
        Assert.IsTrue(component.Find("[role=tabpanel]").ClassList.Contains("custom-body"));
    }



    [TestMethod]
    public void BitPivotShouldRespectTheWrapOverflowBehavior()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.OverflowBehavior, BitPivotOverflowBehavior.Wrap);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
        });

        Assert.IsTrue(component.Find(".bit-pvt").ClassList.Contains("bit-pvt-wrp"));
    }

    [TestMethod]
    public void BitPivotLoopOffShouldStopTheNavigationAtBothEndsOfTheHeader()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Loop, false);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
        });

        var tablist = component.Find(".bit-pvt-hct");

        tablist.KeyDown("ArrowLeft");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[0].GetAttribute("tabindex"));

        tablist.KeyDown("ArrowRight");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[1].GetAttribute("tabindex"));

        tablist.KeyDown("ArrowRight");
        Assert.AreEqual("0", component.FindAll("[role=tab]")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPivotKeepMountedShouldKeepThePanelOfEveryTabThatHasBeenShown()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.KeepMounted, true);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A").AddChildContent("first"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B").AddChildContent("second"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "C").AddChildContent("third"));
        });

        // Only the tab that has been shown so far has a panel of its own.
        Assert.AreEqual(1, component.FindAll("[role=tabpanel]").Count);

        component.FindAll("[role=tab]")[1].Click();

        var panels = component.FindAll("[role=tabpanel]");

        Assert.AreEqual(2, panels.Count);
        Assert.IsTrue(panels[0].HasAttribute("hidden"));
        Assert.AreEqual("true", panels[0].GetAttribute("aria-hidden"));
        Assert.IsFalse(panels[1].HasAttribute("hidden"));
        Assert.IsTrue(panels[0].TextContent.Contains("first"));
        Assert.IsTrue(panels[1].TextContent.Contains("second"));
    }

    [TestMethod]
    public void BitPivotAutoHideSlideButtonsShouldOnlyRenderThemWithSomethingToSlideTo()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.AutoHideSlideButtons, true);
            parameters.Add(p => p.OverflowBehavior, BitPivotOverflowBehavior.Slide);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
        });

        Assert.AreEqual(0, component.FindAll(".bit-pvt-sbt").Count);

        component.InvokeAsync(() => component.Instance.OnSetSlideState(true, true, false));

        Assert.AreEqual(2, component.FindAll(".bit-pvt-sbt").Count);
    }

    [TestMethod]
    public void BitPivotShouldKeepTheItemsInTheOrderTheyAreDeclaredIn()
    {
        // The header is what says where a tab sits, so the order is read back off its own elements.
        Context.JSInterop.Setup<string[]>("BitBlazorUI.Pivot.getItemsOrder", _ => true).SetResult(["a", "b", "c"]);

        var showMiddle = false;

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent(builder =>
            {
                AddItem(builder, 0, "a");

                if (showMiddle)
                {
                    AddItem(builder, 10, "b");
                }

                AddItem(builder, 20, "c");
            });
        });

        showMiddle = true;
        component.Render();

        // The item was created after the other two, but it is declared between them.
        Assert.AreEqual("a,b,c", string.Join(",", component.Instance.Items.Select(i => i.Key)));

        var tablist = component.Find(".bit-pvt-hct");
        tablist.KeyDown("ArrowRight");

        Assert.AreEqual("0", component.FindAll("[role=tab]")[1].GetAttribute("tabindex"));
    }



    [TestMethod]
    public void BitPivotReorderableShouldMakeEveryItemDraggableButThePinnedOnes()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Reorderable, true);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B").Add(i => i.Reorderable, false));
        });

        var tabs = component.FindAll("[role=tab]");

        Assert.AreEqual("true", tabs[0].GetAttribute("draggable"));
        Assert.IsNull(tabs[1].GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitPivotShouldNotMakeTheItemsDraggableWithoutReorderable()
    {
        var component = RenderPivot(2);

        Assert.IsNull(component.Find("[role=tab]").GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitPivotShouldRaiseOnItemReorderWhenAnItemIsDroppedOnAnother()
    {
        BitPivotReorderEventArgs? args = null;

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Reorderable, true);
            parameters.Add(p => p.OnItemReorder, (BitPivotReorderEventArgs a) => args = a);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "a").Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "b").Add(i => i.HeaderText, "B"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.Key, "c").Add(i => i.HeaderText, "C"));
        });

        var tabs = component.FindAll("[role=tab]");

        tabs[0].DragStart();
        tabs[2].DragEnter();

        // The tab the drag is over says where the drop would land, on the edge it would come to rest on.
        Assert.IsTrue(component.FindAll("[role=tab]")[2].ClassList.Contains("bit-pvti-dro"));
        Assert.IsTrue(component.FindAll("[role=tab]")[2].ClassList.Contains("bit-pvti-dro-end"));
        Assert.IsTrue(component.FindAll("[role=tab]")[0].ClassList.Contains("bit-pvti-drg"));

        // Only one tab at a time carries the indicator.
        component.FindAll("[role=tab]")[1].DragEnter();
        Assert.IsFalse(component.FindAll("[role=tab]")[2].ClassList.Contains("bit-pvti-dro"));

        component.FindAll("[role=tab]")[2].DragEnter();

        component.FindAll("[role=tab]")[2].Drop();

        Assert.IsNotNull(args);
        Assert.AreEqual("a", args.Item.Key);
        Assert.AreEqual(0, args.OldIndex);
        Assert.AreEqual(2, args.NewIndex);

        // The drag state is dropped along with the item.
        Assert.IsFalse(component.FindAll("[role=tab]")[0].ClassList.Contains("bit-pvti-drg"));
        Assert.IsFalse(component.FindAll("[role=tab]")[2].ClassList.Contains("bit-pvti-dro"));
    }

    [TestMethod]
    public void BitPivotShouldDrawTheDropIndicatorOnTheNearEdgeOfATabTheDragTravelsBackTo()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Reorderable, true);
            parameters.Add(p => p.OnItemReorder, (BitPivotReorderEventArgs _) => { });
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "C"));
        });

        var tabs = component.FindAll("[role=tab]");

        tabs[2].DragStart();
        tabs[0].DragEnter();

        Assert.IsTrue(component.FindAll("[role=tab]")[0].ClassList.Contains("bit-pvti-dro"));
        Assert.IsFalse(component.FindAll("[role=tab]")[0].ClassList.Contains("bit-pvti-dro-end"));
    }

    [TestMethod]
    public void BitPivotShouldRaiseOnItemReorderFromTheCtrlArrowKeys()
    {
        var moves = new List<(int, int)>();

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Reorderable, true);
            parameters.Add(p => p.OnItemReorder, (BitPivotReorderEventArgs a) => moves.Add((a.OldIndex, a.NewIndex)));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
        });

        var tablist = component.Find(".bit-pvt-hct");

        tablist.KeyDown(new KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });

        Assert.AreEqual(1, moves.Count);
        Assert.AreEqual((0, 1), moves[0]);

        // The focus stays on the item that is being moved rather than following the arrow.
        Assert.AreEqual("0", component.FindAll("[role=tab]")[0].GetAttribute("tabindex"));

        // There is nothing before the first item to move it to.
        tablist.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", CtrlKey = true });

        Assert.AreEqual(1, moves.Count);
    }

    [TestMethod]
    public void BitPivotShouldNotReorderOntoAnItemThatIsPinnedInPlace()
    {
        var count = 0;

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Reorderable, true);
            parameters.Add(p => p.OnItemReorder, (BitPivotReorderEventArgs _) => count++);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B").Add(i => i.Reorderable, false));
        });

        var tabs = component.FindAll("[role=tab]");

        tabs[0].DragStart();
        tabs[1].Drop();

        Assert.AreEqual(0, count);

        component.Find(".bit-pvt-hct").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });

        Assert.AreEqual(0, count);
    }



    [TestMethod]
    public void BitPivotOverflowMenuShouldRenderTheFoldedItemsAsASingleChoice()
    {
        var component = RenderOverflowPivot();

        Assert.AreEqual(2, component.FindAll(".bit-pvt-mni").Count);

        var items = component.FindAll(".bit-pvt-mni");

        Assert.AreEqual("menuitemradio", items[0].GetAttribute("role"));
        Assert.AreEqual("false", items[0].GetAttribute("aria-checked"));
        Assert.AreEqual("-1", items[0].GetAttribute("tabindex"));

        // The folded items are out of the header, so the arrow keys cannot reach them anymore.
        Assert.AreEqual("-1", component.FindAll("[role=tab]")[2].GetAttribute("tabindex"));

        Assert.AreEqual("menu", component.Find(".bit-pvt-mnc").GetAttribute("role"));
        Assert.AreEqual("A", component.Instance.SelectedItem?.HeaderText);
    }

    [TestMethod]
    public void BitPivotOverflowMenuShouldNavigateWithTheArrowHomeAndEndKeys()
    {
        var component = RenderOverflowPivot();

        component.Find(".bit-pvt-mor").Click();

        var menu = component.Find(".bit-pvt-mnc");
        var menuId = menu.GetAttribute("id");

        // The menu opens on its first item, since the selected tab is not one of the folded ones.
        Assert.AreEqual($"{menuId}-0", menu.GetAttribute("aria-activedescendant"));

        menu.KeyDown("ArrowDown");
        Assert.AreEqual($"{menuId}-1", component.Find(".bit-pvt-mnc").GetAttribute("aria-activedescendant"));

        // The navigation of the menu wraps around like the navigation of the header does.
        component.Find(".bit-pvt-mnc").KeyDown("ArrowDown");
        Assert.AreEqual($"{menuId}-0", component.Find(".bit-pvt-mnc").GetAttribute("aria-activedescendant"));

        component.Find(".bit-pvt-mnc").KeyDown("End");
        Assert.AreEqual($"{menuId}-1", component.Find(".bit-pvt-mnc").GetAttribute("aria-activedescendant"));

        component.Find(".bit-pvt-mnc").KeyDown("Home");
        Assert.AreEqual($"{menuId}-0", component.Find(".bit-pvt-mnc").GetAttribute("aria-activedescendant"));

        Assert.IsTrue(component.FindAll(".bit-pvt-mni")[0].ClassList.Contains("bit-pvt-mni-act"));
    }

    [TestMethod]
    public void BitPivotOverflowMenuShouldSelectTheItemItIsOnWithEnter()
    {
        var component = RenderOverflowPivot();

        component.Find(".bit-pvt-mor").Click();
        component.Find(".bit-pvt-mnc").KeyDown("ArrowDown");
        component.Find(".bit-pvt-mnc").KeyDown("Enter");

        Assert.AreEqual("true", component.FindAll("[role=tab]")[3].GetAttribute("aria-selected"));

        // Selecting from the menu closes it.
        Assert.IsTrue(component.Find(".bit-pvt-mnc").GetAttribute("style")!.Contains("display:none"));
    }

    [TestMethod]
    public void BitPivotOverflowMenuShouldCloseOnEscapeFromTheMenuAndFromTheButton()
    {
        var component = RenderOverflowPivot();

        component.Find(".bit-pvt-mor").Click();
        Assert.AreEqual("true", component.Find(".bit-pvt-mor").GetAttribute("aria-expanded"));

        component.Find(".bit-pvt-mnc").KeyDown("Escape");
        Assert.AreEqual("false", component.Find(".bit-pvt-mor").GetAttribute("aria-expanded"));

        // The button that opens the menu closes it as well, since it is where the focus goes back to.
        component.Find(".bit-pvt-mor").Click();
        component.Find(".bit-pvt-mor").KeyDown("Escape");
        Assert.AreEqual("false", component.Find(".bit-pvt-mor").GetAttribute("aria-expanded"));

        // The down arrow opens the menu from the button.
        component.Find(".bit-pvt-mor").KeyDown("ArrowDown");
        Assert.AreEqual("true", component.Find(".bit-pvt-mor").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitPivotShouldMarkTheOverflowButtonWhenTheSelectedTabIsFoldedAway()
    {
        var component = RenderOverflowPivot();

        Assert.IsFalse(component.Find(".bit-pvt-mor").ClassList.Contains("bit-pvt-mor-sel"));

        component.Find(".bit-pvt-mor").Click();
        component.FindAll(".bit-pvt-mni")[0].Click();

        Assert.IsTrue(component.Find(".bit-pvt-mor").ClassList.Contains("bit-pvt-mor-sel"));
        Assert.AreEqual("true", component.FindAll(".bit-pvt-mni")[0].GetAttribute("aria-checked"));
    }



    private static void AddItem(RenderTreeBuilder builder, int sequence, string key)
    {
        builder.OpenComponent<BitPivotItem>(sequence);
        builder.SetKey(key);
        builder.AddComponentParameter(sequence + 1, nameof(BitPivotItem.Id), key);
        builder.AddComponentParameter(sequence + 2, nameof(BitPivotItem.Key), key);
        builder.AddComponentParameter(sequence + 3, nameof(BitPivotItem.HeaderText), key);
        builder.CloseComponent();
    }

    // A Menu pivot whose last two items the (unavailable) JS half of the overflow behavior reports as
    // the folded ones, which is the state every assertion about the overflow menu starts from.
    private IRenderedComponent<BitPivot> RenderOverflowPivot()
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.OverflowBehavior, BitPivotOverflowBehavior.Menu);
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "A"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "B"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "C"));
            parameters.AddChildContent<BitPivotItem>(p => p.Add(i => i.HeaderText, "D"));
        });

        component.InvokeAsync(() => component.Instance.OnSetOverflowItems([2, 3]));

        return component;
    }

    private IRenderedComponent<BitPivot> RenderPivot(int count)
    {
        return RenderComponent<BitPivot>(parameters =>
        {
            for (var i = 0; i < count; i++)
            {
                var header = ((char)('A' + i)).ToString();
                parameters.AddChildContent<BitPivotItem>(p => p.Add(item => item.HeaderText, header));
            }
        });
    }
}
