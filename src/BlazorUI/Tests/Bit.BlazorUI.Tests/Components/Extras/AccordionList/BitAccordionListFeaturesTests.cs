using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccordionList;

[TestClass]
public class BitAccordionListFeaturesTests : BunitTestContext
{
    private static List<BitAccordionListItem> GetItems() =>
    [
        new() { Key = "a", Title = "Item A", Body = Content("Body A") },
        new() { Key = "b", Title = "Item B", Body = Content("Body B") },
        new() { Key = "c", Title = "Item C", Body = Content("Body C") },
    ];

    private static RenderFragment<BitAccordionListItem> Content(string text) => item => builder => builder.AddContent(0, text);


    [TestMethod]
    public void BitAccordionListShouldNotCollapseTheLastItemWhenNotCollapsible()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Collapsible, false);
            parameters.Add(p => p.DefaultExpandedKey, "a");
        });

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.IsTrue(component.FindAll(".bit-acd-con")[0].ClassList.Contains("bit-acd-cex"));
        Assert.AreEqual("true", component.FindAll(".bit-acd-hdr")[0].GetAttribute("aria-disabled"));

        // Another item can still take its place, and the one that opens becomes the one that is locked.
        component.FindAll(".bit-acd-hdr")[1].Click();

        Assert.IsFalse(component.FindAll(".bit-acd-con")[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(component.FindAll(".bit-acd-con")[1].ClassList.Contains("bit-acd-cex"));
        Assert.IsNull(component.FindAll(".bit-acd-hdr")[0].GetAttribute("aria-disabled"));
        Assert.AreEqual("true", component.FindAll(".bit-acd-hdr")[1].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitAccordionListNotCollapsibleShouldOnlyLockTheLastExpandedItemInMultiple()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Collapsible, false);
            parameters.Add(p => p.DefaultExpandedKeys, ["a", "b"]);
        });

        // Two are open, so either of them can still be closed.
        component.FindAll(".bit-acd-hdr")[0].Click();
        Assert.IsFalse(component.FindAll(".bit-acd-con")[0].ClassList.Contains("bit-acd-cex"));

        // The one that is left cannot.
        component.FindAll(".bit-acd-hdr")[1].Click();
        Assert.IsTrue(component.FindAll(".bit-acd-con")[1].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public async Task BitAccordionListNotCollapsibleShouldStillCollapseFromTheCollapseAllMethod()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Collapsible, false);
            parameters.Add(p => p.DefaultExpandedKey, "a");
        });

        await component.InvokeAsync(() => component.Instance.CollapseAll());

        component.WaitForAssertion(() => Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count));
    }

    [TestMethod]
    public void BitAccordionListReadOnlyShouldReportTheClickWithoutToggling()
    {
        var clicked = 0;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnItemClick, (BitAccordionListItem i) => clicked++);
        });

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.AreEqual(1, clicked);
        Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
        Assert.AreEqual("true", component.FindAll(".bit-acd-hdr")[0].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitAccordionListItemReadOnlyShouldOverrideTheListValue()
    {
        var items = GetItems();
        items[0].ReadOnly = true;
        items[1].ReadOnly = false;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, items);
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        component.FindAll(".bit-acd-hdr")[1].Click();

        Assert.IsFalse(component.FindAll(".bit-acd-con")[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(component.FindAll(".bit-acd-con")[1].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListShouldNotToggleADisabledItem()
    {
        var items = GetItems();
        items[0].IsEnabled = false;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
        Assert.IsTrue(component.FindAll(".bit-acd")[0].ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public async Task BitAccordionListExpandAllShouldSkipTheDisabledItems()
    {
        var items = GetItems();
        items[1].IsEnabled = false;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, items);
        });

        await component.InvokeAsync(() => component.Instance.ExpandAll());

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll(".bit-acd-con.bit-acd-cex").Count));
    }

    [TestMethod]
    public void BitAccordionListShouldApplyTheSizeToEveryItem()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Size, BitSize.Large);
        });

        Assert.AreEqual(3, component.FindAll(".bit-acd.bit-acd-lg").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldApplyTheExpanderIconPositionToEveryItem()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.ExpanderIconPosition, BitIconPosition.Start);
        });

        Assert.AreEqual(3, component.FindAll(".bit-acd.bit-acd-sei").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldHideTheExpanderIconAndLetAnItemOptOut()
    {
        var items = GetItems();
        items[0].HideExpanderIcon = false;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.HideExpanderIcon, true);
        });

        Assert.AreEqual(1, component.FindAll(".bit-acd-eiw").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheItemIconAndTheExpandedExpanderIcon()
    {
        var items = GetItems();
        items[0].IconName = "Settings";

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultExpandedKey, "a");
            parameters.Add(p => p.ExpandedExpanderIconName, "Remove");
        });

        Assert.AreEqual(1, component.FindAll(".bit-acd-ico").Count);
        Assert.IsTrue(component.FindAll(".bit-acd-eic")[0].ClassList.Contains("bit-icon--Remove"));
        Assert.IsFalse(component.FindAll(".bit-acd-eic")[1].ClassList.Contains("bit-icon--Remove"));
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheActionsBesideTheHeader()
    {
        var items = GetItems();
        items[0].Actions = item => builder => builder.AddContent(0, "action");

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual(1, component.FindAll(".bit-acd-act").Count);
        Assert.AreEqual("action", component.Find(".bit-acd-act").TextContent.Trim());
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheActionsTemplateForEveryItem()
    {
        RenderFragment<BitAccordionListItem> actionsTemplate = item => builder => builder.AddContent(0, item.Key);

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.ActionsTemplate, actionsTemplate);
        });

        Assert.AreEqual(3, component.FindAll(".bit-acd-act").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheTitleAndExpanderTemplates()
    {
        RenderFragment<BitAccordionListItem> titleTemplate = item => builder => builder.AddContent(0, $"T-{item.Key}");
        RenderFragment<BitAccordionListItem> expanderTemplate = item => builder => builder.AddContent(0, $"E-{item.Key}");

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.TitleTemplate, titleTemplate);
            parameters.Add(p => p.ExpanderTemplate, expanderTemplate);
        });

        Assert.AreEqual("T-a", component.FindAll(".bit-acd-ttl")[0].TextContent.Trim());
        Assert.AreEqual("E-a", component.FindAll(".bit-acd-eiw")[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitAccordionListShouldApplyTheHeadingLevelToEveryItem()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.HeadingLevel, 2);
        });

        Assert.AreEqual("2", component.FindAll(".bit-acd-hed")[0].GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitAccordionListShouldDropTheContentRegionRole()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.NoContentRegion, true);
        });

        Assert.IsNull(component.FindAll(".bit-acd-con")[0].GetAttribute("role"));
    }

    [TestMethod]
    public void BitAccordionListShouldApplyTheLayoutParametersToEveryItem()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MaxHeight, "100px");
            parameters.Add(p => p.TransitionDuration, 250);
            parameters.Add(p => p.ExpandOnPrint, true);
            parameters.Add(p => p.Gap, 8);
        });

        var first = component.FindAll(".bit-acd")[0];
        Assert.IsTrue(first.ClassList.Contains("bit-acd-mxh"));
        Assert.IsTrue(first.ClassList.Contains("bit-acd-eop"));
        Assert.IsTrue(first.GetAttribute("style")!.Contains("--bit-acd-max-h:100px"));
        Assert.IsTrue(first.GetAttribute("style")!.Contains("--bit-acd-dur-full:250ms"));
        Assert.IsTrue(component.Find(".bit-acl").GetAttribute("style")!.Contains("gap:8px"));
    }

    [TestMethod]
    public void BitAccordionListLazyContentShouldDelayTheFirstRenderOfTheBody()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.LazyContent, true);
        });

        Assert.AreEqual(string.Empty, component.FindAll(".bit-acd-con")[0].TextContent.Trim());

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.AreEqual("Body A", component.FindAll(".bit-acd-con")[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitAccordionListUnmountOnCollapseShouldRemoveTheBodyAgain()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.UnmountOnCollapse, true);
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        Assert.AreEqual("Body A", component.FindAll(".bit-acd-con")[0].TextContent.Trim());

        component.FindAll(".bit-acd-hdr")[0].Click();
        Assert.AreEqual(string.Empty, component.FindAll(".bit-acd-con")[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitAccordionListShouldCancelTheToggleFromOnToggling()
    {
        BitAccordionListToggleArgs<BitAccordionListItem>? received = null;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.OnToggling, (BitAccordionListToggleArgs<BitAccordionListItem> args) =>
            {
                received = args;
                args.Cancel = true;
            });
        });

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.IsNotNull(received);
        Assert.AreEqual("a", received!.Key);
        Assert.IsTrue(received.IsExpanding);
        Assert.AreEqual(BitAccordionToggleReason.Click, received.Reason);
        Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
    }

    [TestMethod]
    public void BitAccordionListOnTogglingShouldLeaveThePreviouslyExpandedItemAloneWhenCancelled()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DefaultExpandedKey, "a");
            parameters.Add(p => p.OnToggling, (BitAccordionListToggleArgs<BitAccordionListItem> args) => args.Cancel = true);
        });

        component.FindAll(".bit-acd-hdr")[1].Click();

        Assert.IsTrue(component.FindAll(".bit-acd-con")[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsFalse(component.FindAll(".bit-acd-con")[1].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public async Task BitAccordionListOnTogglingShouldReportTheMethodReason()
    {
        BitAccordionToggleReason? reason = null;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.OnToggling, (BitAccordionListToggleArgs<BitAccordionListItem> args) => reason = args.Reason);
        });

        await component.InvokeAsync(() => component.Instance.Expand("b"));

        Assert.AreEqual(BitAccordionToggleReason.Method, reason);
    }

    [TestMethod]
    public async Task BitAccordionListShouldExpandCollapseAndToggleByKey()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        await component.InvokeAsync(() => component.Instance.Expand("b"));
        Assert.IsTrue(component.Instance.IsExpanded("b"));
        Assert.IsTrue(component.FindAll(".bit-acd-con")[1].ClassList.Contains("bit-acd-cex"));

        // Single-expand mode collapses the previously expanded item along the way.
        await component.InvokeAsync(() => component.Instance.Expand("c"));
        Assert.IsFalse(component.Instance.IsExpanded("b"));
        CollectionAssert.AreEqual(new[] { "c" }, component.Instance.GetExpandedKeys().ToArray());

        await component.InvokeAsync(() => component.Instance.Toggle("c"));
        Assert.IsFalse(component.Instance.IsExpanded("c"));

        await component.InvokeAsync(() => component.Instance.Toggle("c"));
        Assert.IsTrue(component.Instance.IsExpanded("c"));

        await component.InvokeAsync(() => component.Instance.Collapse("c"));
        Assert.AreEqual(0, component.Instance.GetExpandedKeys().Count);
    }

    [TestMethod]
    public async Task BitAccordionListShouldIgnoreAnUnknownKeyInTheMethods()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        await component.InvokeAsync(() => component.Instance.Expand("nope"));

        Assert.AreEqual(0, component.Instance.GetExpandedKeys().Count);
    }

    [TestMethod]
    public async Task BitAccordionListCollapseAllShouldDropTheKeysThatMapToNoItem()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DefaultExpandedKeys, ["a", "orphan"]);
        });

        CollectionAssert.AreEqual(new[] { "a", "orphan" }, component.Instance.GetExpandedKeys().ToArray());

        await component.InvokeAsync(() => component.Instance.CollapseAll());

        Assert.AreEqual(0, component.Instance.GetExpandedKeys().Count);
    }

    [TestMethod]
    public void BitAccordionListShouldNoticeAMutationOfTheVeryCollectionItWasGiven()
    {
        var items = GetItems();

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        items.Add(new BitAccordionListItem { Key = "d", Title = "Item D" });

        component.Render();

        Assert.AreEqual(4, component.FindAll(".bit-acd").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldKeepTheExpandedStateWhenTheItemsChange()
    {
        var items = GetItems();

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        component.FindAll(".bit-acd-hdr")[1].Click();
        Assert.IsTrue(component.Instance.IsExpanded("b"));

        items.Add(new BitAccordionListItem { Key = "d", Title = "Item D" });
        component.Render();

        Assert.IsTrue(component.Instance.IsExpanded("b"));
        Assert.IsTrue(component.FindAll(".bit-acd-con")[1].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListShouldWorkWithACustomTypeThatCarriesNoKey()
    {
        var items = new List<KeylessItem> { new() { Name = "X" }, new() { Name = "Y" } };

        var component = RenderComponent<BitAccordionList<KeylessItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, new BitAccordionListNameSelectors<KeylessItem>
            {
                Title = { Selector = i => i.Name },
            });
        });

        component.FindAll(".bit-acd-hdr")[1].Click();

        Assert.IsTrue(component.FindAll(".bit-acd-con")[1].ClassList.Contains("bit-acd-cex"));
        Assert.IsFalse(component.FindAll(".bit-acd-con")[0].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListShouldWorkWithACustomTypeWhoseStateIsReadOnly()
    {
        var items = new List<ReadOnlyStateItem> { new() { Name = "X" }, new() { Name = "Y" } };

        var component = RenderComponent<BitAccordionList<ReadOnlyStateItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, new BitAccordionListNameSelectors<ReadOnlyStateItem>
            {
                Title = { Selector = i => i.Name },
            });
        });

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.IsTrue(component.FindAll(".bit-acd-con")[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(component.Instance.IsExpanded("X"));
    }

    [TestMethod]
    public void BitAccordionListShouldMoveTheFocusBetweenTheHeadersWithTheArrowKeys()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        var wrappers = component.FindAll(".bit-acl-itm");
        Assert.AreEqual(3, wrappers.Count);

        // The focus itself is a JS call the loose interop swallows, so what is asserted here is that the
        // navigation runs over the headers without throwing and leaves the expanded state alone.
        wrappers[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        wrappers[0].KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        wrappers[0].KeyDown(new KeyboardEventArgs { Key = "Home" });
        wrappers[0].KeyDown(new KeyboardEventArgs { Key = "End" });

        Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldIgnoreTheNavigationKeysWhenNotNavigable()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Navigable, false);
        });

        component.FindAll(".bit-acl-itm")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
    }

    [TestMethod]
    public async Task BitAccordionListShouldFocusAnItemByKey()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        await component.InvokeAsync(() => component.Instance.FocusItem("b"));
        await component.InvokeAsync(() => component.Instance.FocusAsync());
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheAriaLabelOnTheRoot()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.AriaLabel, "settings");
        });

        Assert.AreEqual("settings", component.Find(".bit-acl").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAccordionListShouldPassTheClassesAndStylesToEveryPartOfTheItems()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Classes, new BitAccordionListClassStyles
            {
                ItemHeaderWrapper = "custom-hwr",
                ItemHeading = "custom-hed",
                ItemContentWrapper = "custom-cwr",
            });
            parameters.Add(p => p.Styles, new BitAccordionListClassStyles
            {
                ItemTitle = "color: red;",
            });
        });

        Assert.AreEqual(3, component.FindAll(".bit-acd-hwr.custom-hwr").Count);
        Assert.AreEqual(3, component.FindAll(".bit-acd-hed.custom-hed").Count);
        Assert.AreEqual(3, component.FindAll(".bit-acd-cwr.custom-cwr").Count);
        Assert.AreEqual("color: red;", component.FindAll(".bit-acd-ttl")[0].GetAttribute("style"));
    }

    [TestMethod]
    public void BitAccordionListOptionShouldExpandFromItsOwnHeaderClick()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListOption>>(parameters =>
        {
            parameters.AddChildContent<BitAccordionListOption>(p => p.Add(o => o.Title, "Option A"));
            parameters.AddChildContent<BitAccordionListOption>(p => p.Add(o => o.Title, "Option B"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll(".bit-acd-hdr").Count));

        component.FindAll(".bit-acd-hdr")[1].Click();

        component.WaitForAssertion(() => Assert.IsTrue(component.FindAll(".bit-acd-con")[1].ClassList.Contains("bit-acd-cex")));
    }

    [TestMethod]
    public void BitAccordionListOptionShouldRenderItsOwnIconAndActions()
    {
        RenderFragment<BitAccordionListOption> actions = option => builder => builder.AddContent(0, "act");

        var component = RenderComponent<BitAccordionList<BitAccordionListOption>>(parameters =>
        {
            parameters.AddChildContent<BitAccordionListOption>(p =>
            {
                p.Add(o => o.Title, "Option A");
                p.Add(o => o.IconName, "Settings");
                p.Add(o => o.Actions, actions);
            });
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, component.FindAll(".bit-acd-ico").Count);
            Assert.AreEqual("act", component.Find(".bit-acd-act").TextContent.Trim());
        });
    }

    [TestMethod]
    public void BitAccordionListOptionReadOnlyShouldKeepThePanelWhereItIs()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListOption>>(parameters =>
        {
            parameters.AddChildContent<BitAccordionListOption>(p =>
            {
                p.Add(o => o.Title, "Option A");
                p.Add(o => o.ReadOnly, true);
            });
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-acd-hdr").Count));

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
    }


    [TestMethod]
    public void BitAccordionListShouldNameAHeaderThatDoesNotNameItself()
    {
        var items = GetItems();
        items[0].HeaderAriaLabel = "Notifications";

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual("Notifications", component.FindAll(".bit-acd-hdr")[0].GetAttribute("aria-label"));
        Assert.IsNull(component.FindAll(".bit-acd-hdr")[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheHeaderAndBodyTemplatesOfTheList()
    {
        RenderFragment<BitAccordionListItem> headerTemplate = item => builder => builder.AddContent(0, $"H-{item.Key}");
        RenderFragment<BitAccordionListItem> bodyTemplate = item => builder => builder.AddContent(0, $"B-{item.Key}");

        // The templates of the list stand in for the items that bring none of their own.
        List<BitAccordionListItem> items = [new() { Key = "a", Title = "Item A" }, new() { Key = "b", Title = "Item B" }];

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.HeaderTemplate, headerTemplate);
            parameters.Add(p => p.BodyTemplate, bodyTemplate);
        });

        Assert.AreEqual("H-a", component.FindAll(".bit-acd-hdr")[0].TextContent.Trim());
        Assert.AreEqual("B-a", component.FindAll(".bit-acd-con")[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitAccordionListItemTemplatesShouldWinOverTheTemplatesOfTheList()
    {
        RenderFragment<BitAccordionListItem> listTemplate = item => builder => builder.AddContent(0, "from-the-list");

        var items = GetItems();
        items[0].HeaderTemplate = item => builder => builder.AddContent(0, "from-the-item");
        items[0].Actions = item => builder => builder.AddContent(0, "item-actions");

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.HeaderTemplate, listTemplate);
            parameters.Add(p => p.ActionsTemplate, listTemplate);
        });

        Assert.AreEqual("from-the-item", component.FindAll(".bit-acd-hdr")[0].TextContent.Trim());
        Assert.AreEqual("from-the-list", component.FindAll(".bit-acd-hdr")[1].TextContent.Trim());
        Assert.AreEqual("item-actions", component.FindAll(".bit-acd-act")[0].TextContent.Trim());
        Assert.AreEqual("from-the-list", component.FindAll(".bit-acd-act")[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitAccordionListShouldKeepTheExpanderIconStill()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.NoExpanderRotation, true);
            parameters.Add(p => p.DefaultExpandedKey, "a");
        });

        Assert.IsFalse(component.FindAll(".bit-acd-eiw")[0].ClassList.Contains("bit-ico--r180"));
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheOptionsAlias()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListOption>>(parameters =>
        {
            parameters.Add(p => p.Options, builder =>
            {
                builder.OpenComponent<BitAccordionListOption>(0);
                builder.AddComponentParameter(1, nameof(BitAccordionListOption.Title), "Option A");
                builder.CloseComponent();
            });
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-acd").Count));
    }

    [TestMethod]
    public void BitAccordionListShouldReadTheNewMembersOfACustomTypeThroughNameSelectors()
    {
        var items = new List<RichItem>
        {
            new() { Id = "x", Name = "X", Glyph = "Settings", Locked = true, NoChevron = true },
            new() { Id = "y", Name = "Y" },
        };

        var component = RenderComponent<BitAccordionList<RichItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, new BitAccordionListNameSelectors<RichItem>
            {
                Key = { Selector = i => i.Id },
                Title = { Selector = i => i.Name },
                IconName = { Selector = i => i.Glyph },
                ReadOnly = { Selector = i => i.Locked },
                HideExpanderIcon = { Selector = i => i.NoChevron },
            });
        });

        Assert.AreEqual(1, component.FindAll(".bit-acd-ico").Count);
        Assert.AreEqual(1, component.FindAll(".bit-acd-eiw").Count);

        // The read-only item does not answer the click, the other one does.
        component.FindAll(".bit-acd-hdr")[0].Click();
        Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count);

        component.FindAll(".bit-acd-hdr")[1].Click();
        Assert.AreEqual(1, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
    }

    [TestMethod]
    public async Task BitAccordionListShouldIgnoreAnEmptyKeyInTheMethods()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        await component.InvokeAsync(() => component.Instance.Expand(string.Empty));
        await component.InvokeAsync(() => component.Instance.Toggle(string.Empty));

        Assert.AreEqual(0, component.Instance.GetExpandedKeys().Count);
        Assert.IsFalse(component.Instance.IsExpanded(null));
    }


    public class RichItem
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Glyph { get; set; }

        public bool? Locked { get; set; }

        public bool? NoChevron { get; set; }
    }

    [TestMethod]
    public void BitAccordionListShouldKeepOnlyOnePanelOpenWhenLeavingTheMultipleExpandMode()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        component.FindAll(".bit-acd-hdr")[2].Click();
        Assert.AreEqual(2, component.FindAll(".bit-acd-con.bit-acd-cex").Count);

        component.Render(parameters => parameters.Add(p => p.Multiple, false));

        Assert.AreEqual(1, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
        CollectionAssert.AreEqual(new[] { "a" }, component.Instance.GetExpandedKeys().ToArray());
    }

    [TestMethod]
    public async Task BitAccordionListCollapseAllShouldCloseTheDisabledItemsAsWell()
    {
        var items = GetItems();
        items[1].IsEnabled = false;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultExpandedKeys, ["a", "b"]);
        });

        Assert.AreEqual(2, component.FindAll(".bit-acd-con.bit-acd-cex").Count);

        await component.InvokeAsync(() => component.Instance.CollapseAll());

        component.WaitForAssertion(() => Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count));
    }

    [TestMethod]
    public void BitAccordionListMaxExpandedShouldCloseTheOldestPanel()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MaxExpanded, 2);
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        component.FindAll(".bit-acd-hdr")[1].Click();

        CollectionAssert.AreEqual(new[] { "a", "b" }, component.Instance.GetExpandedKeys().ToArray());

        // The third one is opened all the same - nothing is turned away - and the one that was opened first
        // is the one that closes for it.
        component.FindAll(".bit-acd-hdr")[2].Click();

        CollectionAssert.AreEqual(new[] { "b", "c" }, component.Instance.GetExpandedKeys().ToArray());
        Assert.IsFalse(component.FindAll(".bit-acd-con")[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(component.FindAll(".bit-acd-con")[2].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListMaxExpandedShouldCapTheDefaultExpandedKeys()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MaxExpanded, 2);
            parameters.Add(p => p.DefaultExpandedKeys, ["a", "b", "c"]);
        });

        CollectionAssert.AreEqual(new[] { "a", "b" }, component.Instance.GetExpandedKeys().ToArray());
    }

    [TestMethod]
    public async Task BitAccordionListMaxExpandedShouldCapExpandAll()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MaxExpanded, 2);
        });

        await component.InvokeAsync(() => component.Instance.ExpandAll());

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll(".bit-acd-con.bit-acd-cex").Count));
    }

    [TestMethod]
    public void BitAccordionListMaxExpandedShouldMeanNothingOutsideOfTheMultipleExpandMode()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MaxExpanded, 2);
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        component.FindAll(".bit-acd-hdr")[1].Click();

        CollectionAssert.AreEqual(new[] { "b" }, component.Instance.GetExpandedKeys().ToArray());
    }

    [TestMethod]
    public void BitAccordionListMaxExpandedBelowOneShouldBeNoCapAtAll()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MaxExpanded, 0);
            parameters.Add(p => p.DefaultExpandedKeys, ["a", "b", "c"]);
        });

        Assert.AreEqual(3, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
    }

    [TestMethod]
    public void BitAccordionListMaxExpandedShouldCloseTheOldestPanelsWhenItIsLowered()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DefaultExpandedKeys, ["a", "b", "c"]);
        });

        Assert.AreEqual(3, component.FindAll(".bit-acd-con.bit-acd-cex").Count);

        component.Render(parameters => parameters.Add(p => p.MaxExpanded, 1));

        CollectionAssert.AreEqual(new[] { "c" }, component.Instance.GetExpandedKeys().ToArray());
    }

    [TestMethod]
    public void BitAccordionListShouldPushBackABoundSetThatTheCapTrimmed()
    {
        var bound = new List<string> { "a", "b", "c" };

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MaxExpanded, 2);
            parameters.Add(p => p.ExpandedKeys, bound);
            parameters.Add(p => p.ExpandedKeysChanged, keys => bound = [.. keys ?? []]);
        });

        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "a", "b" }, bound.ToArray()));
        Assert.AreEqual(2, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
    }

    [TestMethod]
    public void BitAccordionListNoNavigationLoopShouldStopAtTheEndsOfTheList()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.NoNavigationLoop, true);
        });

        // The focus itself is a JS call the loose interop swallows, so what is asserted here is that the
        // navigation past either end runs without throwing and leaves the expanded state alone.
        var wrappers = component.FindAll(".bit-acl-itm");
        wrappers[0].KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        wrappers[2].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheEmptyContentOnlyWhileTheListIsEmpty()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, []);
            parameters.Add(p => p.EmptyContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"no-items\">Nothing here</span>")));
        });

        Assert.AreEqual(1, component.FindAll(".no-items").Count);
        Assert.AreEqual(0, component.FindAll(".bit-acd").Count);

        component.Render(parameters => parameters.Add(p => p.Items, GetItems()));

        Assert.AreEqual(0, component.FindAll(".no-items").Count);
        Assert.AreEqual(3, component.FindAll(".bit-acd").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheEmptyContentOfAListOfOptions()
    {
        // A list of options only knows it is empty once its options have had their turn to register, so the
        // empty content needs a render of its own - and an empty list has no option to ask for one.
        var component = RenderComponent<BitAccordionList<BitAccordionListOption>>(parameters =>
        {
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => { }));
            parameters.Add(p => p.EmptyContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"no-items\">Nothing here</span>")));
        });

        Assert.AreEqual(1, component.FindAll(".no-items").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldNameItselfAsAGroupOnlyWhenItCarriesALabel()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        // A label on a plain container is dropped by a screen reader, so the role comes with the label
        // rather than being there for a list that has nothing to be named by.
        Assert.IsNull(component.Find(".bit-acl").GetAttribute("role"));

        component.Render(parameters => parameters.Add(p => p.AriaLabel, "settings"));

        Assert.AreEqual("group", component.Find(".bit-acl").GetAttribute("role"));
    }

    [TestMethod]
    public void BitAccordionListShouldLeaveARoleOfThePagesOwnAlone()
    {
        var component = RenderComponent<BitAccordionListHtmlAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        Assert.AreEqual("region", component.Find(".bit-acl").GetAttribute("role"));
    }

    [TestMethod]
    public async Task BitAccordionListShouldReportTheItemAsBusyWhileAnAwaitedOnTogglingRuns()
    {
        var gate = new TaskCompletionSource();

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.OnToggling, EventCallback.Factory.Create<BitAccordionListToggleArgs<BitAccordionListItem>>(this, async _ => await gate.Task));
        });

        var click = component.InvokeAsync(() => component.Find(".bit-acd-hdr").Click());

        // The header of the item the callback was asked about says it is busy; the rest of the list is only
        // refusing to start something else while this one is being decided.
        component.WaitForAssertion(() => Assert.AreEqual("true", component.FindAll(".bit-acd-hdr")[0].GetAttribute("aria-busy")));
        Assert.IsTrue(component.FindAll(".bit-acd-hdr")[0].ClassList.Contains("bit-acd-bsy"));
        Assert.IsNull(component.FindAll(".bit-acd-hdr")[1].GetAttribute("aria-busy"));

        gate.SetResult();
        await click;

        component.WaitForAssertion(() => Assert.IsNull(component.FindAll(".bit-acd-hdr")[0].GetAttribute("aria-busy")));
        Assert.IsTrue(component.FindAll(".bit-acd-con")[0].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListShouldSuppressTheDefaultActionOfItsNavigationKeys()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        // The keys are suppressed on a listener of the browser's own - Blazor's preventDefault directive
        // cannot be decided per key - and only for a key pressed on one of this list's own headers.
        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Extras.setPreventKeys");

        CollectionAssert.AreEqual(new[] { "ArrowDown", "ArrowUp", "Home", "End" }, (string[])invocation.Arguments[1]!);
        Assert.AreEqual(".bit-acd-hdr", invocation.Arguments[2]);
        Assert.AreEqual(".bit-acl", invocation.Arguments[3]);
    }

    [TestMethod]
    public void BitAccordionListShouldNotSuppressAnyKeyWhenItIsNotNavigable()
    {
        RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Navigable, false);
        });

        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Extras.setPreventKeys"));
    }

    [TestMethod]
    public void BitAccordionListShouldScrollAnExpandedItemIntoView()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.ScrollIntoViewOnExpand, true);
        });

        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Extras.scrollIntoView"));

        component.FindAll(".bit-acd-hdr")[1].Click();

        component.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Extras.scrollIntoView")));

        // A collapse takes nothing off the screen that was not already there.
        component.FindAll(".bit-acd-hdr")[1].Click();

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Extras.scrollIntoView"));
    }

    [TestMethod]
    public void BitAccordionListShouldNotScrollAnythingIntoViewWithoutBeingAskedTo()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        component.FindAll(".bit-acd-hdr")[1].Click();

        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Extras.scrollIntoView"));
    }

    [TestMethod]
    public void BitAccordionListShouldNotScrollToThePanelsTheMaxExpandedCapKeptClosed()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MaxExpanded, 1);
            parameters.Add(p => p.ScrollIntoViewOnExpand, true);
            parameters.Add(p => p.ExpandedKeys, (IEnumerable<string>)[]);
        });

        // Only the first key of the set fits under the cap, and the panels the cap kept closed are nothing
        // to scroll to.
        component.Render(parameters => parameters.Add(p => p.ExpandedKeys, (IEnumerable<string>)["a", "b"]));

        component.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Extras.scrollIntoView")));
    }

    [TestMethod]
    public void BitAccordionListShouldScrollToAPanelTheSameChangeAddedToTheList()
    {
        var component = RenderComponent<BitAccordionListScrollOnExpandTest>(parameters => parameters.Add(p => p.ShowSecond, false));

        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Extras.scrollIntoView"));

        // The panel this change opens belongs to the option the same change adds, so it registers itself -
        // and then its element - only in the renders that follow the one that asked for the scroll.
        component.Render(parameters =>
        {
            parameters.Add(p => p.ShowSecond, true);
            parameters.Add(p => p.ExpandedKey, "second");
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Extras.scrollIntoView")));
    }

    [TestMethod]
    public void BitAccordionListShouldLeaveTheNavigationKeysToAPanelThatScrollsAndHoldsTheFocus()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MaxHeight, "100px");
            parameters.Add(p => p.DefaultExpandedKey, "a");
        });

        // A panel with a MaxHeight is a tab stop of its own, so the arrow keys pressed on it are its own
        // scroll: the stop inside it never sees them, and the navigation would otherwise move the reader
        // twice - once down the list and once down the panel.
        var panel = component.FindAll(".bit-acd-con")[0];
        panel.FocusIn(new FocusEventArgs());
        panel.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual(0, FocusCalls());

        // The same key pressed on the header of the item is the navigation it has always been.
        component.FindAll(".bit-acd-con")[0].FocusOut(new FocusEventArgs());
        component.FindAll(".bit-acl-itm")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual(1, FocusCalls());

        int FocusCalls() => Context.JSInterop.Invocations.Count(i => i.Identifier == "Blazor._internal.domWrapper.focus");
    }

    public class KeylessItem
    {
        public string? Name { get; set; }
    }

    public class ReadOnlyStateItem
    {
        public string? Name { get; set; }

        public string? Key => Name;

        public bool IsExpanded => false;
    }
}
