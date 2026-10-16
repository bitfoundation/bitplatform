using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccordionList;

[TestClass]
public class BitAccordionListTests : BunitTestContext
{
    private static List<BitAccordionListItem> GetItems() =>
    [
        new() { Key = "a", Title = "Item A", Body = Content("Body A") },
        new() { Key = "b", Title = "Item B", Body = Content("Body B") },
        new() { Key = "c", Title = "Item C", Body = Content("Body C") },
    ];

    private static RenderFragment<BitAccordionListItem> Content(string text) => item => builder => builder.AddContent(0, text);


    [TestMethod]
    public void BitAccordionListShouldRenderAllItems()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        Assert.IsNotNull(component.Find(".bit-acl"));
        Assert.AreEqual(3, component.FindAll(".bit-acd").Count);
        Assert.AreEqual(3, component.FindAll(".bit-acd-ttl").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldExpandItemOnHeaderClick()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        var headers = component.FindAll(".bit-acd-hdr");
        headers[0].Click();

        var contents = component.FindAll(".bit-acd-con");
        Assert.IsTrue(contents[0].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListSingleExpandShouldCollapseOthers()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        component.FindAll(".bit-acd-hdr")[1].Click();

        var contents = component.FindAll(".bit-acd-con");
        Assert.IsFalse(contents[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(contents[1].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListMultipleShouldKeepMultipleExpanded()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        component.FindAll(".bit-acd-hdr")[1].Click();

        var contents = component.FindAll(".bit-acd-con");
        Assert.IsTrue(contents[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(contents[1].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListShouldHonorDefaultExpandedKey()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DefaultExpandedKey, "b");
        });

        var contents = component.FindAll(".bit-acd-con");
        Assert.IsFalse(contents[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(contents[1].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListShouldHonorDefaultExpandedKeysInMultiple()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DefaultExpandedKeys, ["a", "c"]);
        });

        var contents = component.FindAll(".bit-acd-con");
        Assert.IsTrue(contents[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsFalse(contents[1].ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(contents[2].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListShouldRaiseExpandCollapseAndToggleEvents()
    {
        string? expanded = null;
        string? collapsed = null;
        string? clicked = null;
        var toggleCount = 0;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.OnExpand, (BitAccordionListItem i) => expanded = i.Key);
            parameters.Add(p => p.OnCollapse, (BitAccordionListItem i) => collapsed = i.Key);
            parameters.Add(p => p.OnToggle, (BitAccordionListItem i) => toggleCount++);
            parameters.Add(p => p.OnItemClick, (BitAccordionListItem i) => clicked = i.Key);
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        Assert.AreEqual("a", expanded);
        Assert.AreEqual("a", clicked);
        Assert.AreEqual(1, toggleCount);

        component.FindAll(".bit-acd-hdr")[0].Click();
        Assert.AreEqual("a", collapsed);
        Assert.AreEqual("a", clicked);
        Assert.AreEqual(2, toggleCount);
    }

    [TestMethod]
    public void BitAccordionListShouldTwoWayBindExpandedKey()
    {
        string? boundKey = null;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Bind(p => p.ExpandedKey, boundKey, v => boundKey = v);
        });

        component.FindAll(".bit-acd-hdr")[2].Click();

        component.WaitForAssertion(() => Assert.AreEqual("c", boundKey));
    }

    [TestMethod]
    public void BitAccordionListShouldTwoWayBindExpandedKeysInMultiple()
    {
        IEnumerable<string>? boundKeys = null;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Bind(p => p.ExpandedKeys, boundKeys, v => boundKeys = v);
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        component.FindAll(".bit-acd-hdr")[2].Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsNotNull(boundKeys);
            CollectionAssert.AreEquivalent(new[] { "a", "c" }, boundKeys!.ToList());
        });
    }

    [TestMethod]
    public async Task BitAccordionListExpandAllAndCollapseAllShouldWorkInMultiple()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
        });

        await component.InvokeAsync(() => component.Instance.ExpandAll());
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(3, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
        });

        await component.InvokeAsync(() => component.Instance.CollapseAll());
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
        });
    }

    [TestMethod]
    public void BitAccordionListShouldRenderOptionsAsChildContent()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListOption>>(parameters =>
        {
            parameters.AddChildContent<BitAccordionListOption>(p => p.Add(o => o.Title, "Option 1"));
            parameters.AddChildContent<BitAccordionListOption>(p =>
            {
                p.Add(o => o.Title, "Option 2");
                p.Add(o => o.IsExpanded, true);
            });
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, component.FindAll(".bit-acd").Count);
            var contents = component.FindAll(".bit-acd-con");
            Assert.IsTrue(contents[1].ClassList.Contains("bit-acd-cex"));
        });
    }

    [TestMethod]
    public void BitAccordionListShouldHonorDefaultExpandedKeyForOptions()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListOption>>(parameters =>
        {
            parameters.Add(p => p.DefaultExpandedKey, "b");
            parameters.AddChildContent<BitAccordionListOption>(p =>
            {
                p.Add(o => o.Key, "a");
                p.Add(o => o.Title, "Option A");
            });
            parameters.AddChildContent<BitAccordionListOption>(p =>
            {
                p.Add(o => o.Key, "b");
                p.Add(o => o.Title, "Option B");
            });
        });

        component.WaitForAssertion(() =>
        {
            var contents = component.FindAll(".bit-acd-con");
            Assert.IsFalse(contents[0].ClassList.Contains("bit-acd-cex"));
            Assert.IsTrue(contents[1].ClassList.Contains("bit-acd-cex"));
        });
    }

    [TestMethod]
    public void BitAccordionListShouldHonorDefaultExpandedKeysForOptionsInMultiple()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListOption>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.DefaultExpandedKeys, ["a", "c"]);
            parameters.AddChildContent<BitAccordionListOption>(p =>
            {
                p.Add(o => o.Key, "a");
                p.Add(o => o.Title, "Option A");
            });
            parameters.AddChildContent<BitAccordionListOption>(p =>
            {
                p.Add(o => o.Key, "b");
                p.Add(o => o.Title, "Option B");
            });
            parameters.AddChildContent<BitAccordionListOption>(p =>
            {
                p.Add(o => o.Key, "c");
                p.Add(o => o.Title, "Option C");
            });
        });

        component.WaitForAssertion(() =>
        {
            var contents = component.FindAll(".bit-acd-con");
            Assert.IsTrue(contents[0].ClassList.Contains("bit-acd-cex"));
            Assert.IsFalse(contents[1].ClassList.Contains("bit-acd-cex"));
            Assert.IsTrue(contents[2].ClassList.Contains("bit-acd-cex"));
        });
    }

    [TestMethod]
    public void BitAccordionListShouldSupportCustomTypeWithNameSelectors()
    {
        var items = new List<CustomItem>
        {
            new() { Id = "x", Name = "X", Open = false },
            new() { Id = "y", Name = "Y", Open = true },
        };

        var nameSelectors = new BitAccordionListNameSelectors<CustomItem>
        {
            Key = { Selector = i => i.Id },
            Title = { Selector = i => i.Name },
            IsExpanded = { Selector = i => i.Open },
        };

        var component = RenderComponent<BitAccordionList<CustomItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, nameSelectors);
        });

        Assert.AreEqual(2, component.FindAll(".bit-acd").Count);
        var contents = component.FindAll(".bit-acd-con");
        Assert.IsFalse(contents[0].ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(contents[1].ClassList.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionListOptionsShouldNotReportBoundExpandedKeysThePageAlreadyHolds()
    {
        var component = RenderComponent<BitAccordionListBoundOptionsTest>();

        Assert.AreEqual(1, component.FindAll(".bit-acd-con.bit-acd-cex").Count);
        CollectionAssert.AreEqual(new[] { "second" }, component.Instance.ExpandedKeys.ToArray());
        Assert.AreEqual(0, component.Instance.ChangeCount);

        component.FindAll(".bit-acd-hdr")[0].Click();

        component.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new[] { "first", "second" }, component.Instance.ExpandedKeys.ToArray());
            Assert.AreEqual(1, component.Instance.ChangeCount);
        });
    }

    [TestMethod]
    public void BitAccordionListMultipleShouldSetRootClass()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Items, GetItems());
        });

        Assert.IsTrue(component.Find(".bit-acl").ClassList.Contains("bit-acl-mlt"));
    }


    public class CustomItem
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public bool Open { get; set; }
    }

    [TestMethod]
    public void BitAccordionListShouldKeepASplattedAriaLabel()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitAccordionList<BitAccordionListItem>>(0);
            builder.AddAttribute(1, "aria-label", "Main");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-acl");

        Assert.AreEqual("Main", root.GetAttribute("aria-label"));
        // A label on a plain container is dropped by a screen reader, so a splatted one makes the list a group too.
        Assert.AreEqual("group", root.GetAttribute("role"));
    }

    [TestMethod]
    public void BitAccordionListShouldLeaveItsExpandedKeysAloneWhileItIsDisposedTogetherWithItsOptions()
    {
        // The script answers later, the way it does in a browser: the renderer disposes the options while the lists
        // are still awaiting their own teardown, before the base class has marked them disposed.
        var dispose = Context.JSInterop.SetupVoid("BitBlazorUI.Extras.disposePreventKeys", _ => true);

        var component = RenderComponent<BitAccordionListTeardownTest>();

        component.Render(parameters => parameters.Add(p => p.Show, false));

        Assert.AreEqual(2, dispose.Invocations.Count);
        Assert.AreEqual(0, component.Instance.ExpandedKeyChanges.Count);
        Assert.AreEqual(0, component.Instance.ExpandedKeysChanges.Count);
        Assert.AreEqual("first", component.Instance.ExpandedKey);
        CollectionAssert.AreEqual(new[] { "first" }, component.Instance.ExpandedKeys!.ToArray());

        dispose.SetVoidResult();
    }

    [TestMethod]
    public void BitAccordionListShouldStillCollapseTheExpandedKeysWhenTheExpandedOptionIsRemovedFromAListThatStays()
    {
        var component = RenderComponent<BitAccordionListTeardownTest>();

        component.Render(parameters => parameters.Add(p => p.ShowFirst, false));

        component.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new string?[] { null }, component.Instance.ExpandedKeyChanges);
            Assert.AreEqual(1, component.Instance.ExpandedKeysChanges.Count);
            Assert.AreEqual(0, component.Instance.ExpandedKeysChanges[0]!.Count());
        });
    }

    [TestMethod]
    public void BitAccordionListShouldDisableEveryItemWhileItIsDisabled()
    {
        var clicked = 0;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.OnItemClick, (BitAccordionListItem _) => clicked++);
        });

        Assert.IsTrue(component.Find(".bit-acl").ClassList.Contains("bit-dis"));

        // The list's Disabled reaches every header the way an item's own IsDisabled does.
        var headers = component.FindAll(".bit-acd-hdr");
        Assert.AreEqual(3, headers.Count);
        Assert.IsTrue(headers.All(h => h.GetAttribute("aria-disabled") == "true"));
        Assert.AreEqual(3, component.FindAll(".bit-acd.bit-dis").Count);

        // A click still delivered to a header (a script, an assistive technology) does nothing: no toggle, no report.
        headers[0].Click();

        Assert.AreEqual(0, clicked);
        Assert.AreEqual(0, component.FindAll(".bit-acd-exp").Count);

        component.Render(parameters => parameters.Add(p => p.Disabled, false));

        Assert.IsFalse(component.Find(".bit-acl").ClassList.Contains("bit-dis"));
        Assert.AreEqual(0, component.FindAll(".bit-acd.bit-dis").Count);

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.AreEqual(1, clicked);
        Assert.AreEqual(1, component.FindAll(".bit-acd-exp").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheDescriptionClassAndStyleOfAnItem()
    {
        var items = GetItems();
        items[1].Description = "What B is about";
        items[1].Class = "item-b";
        items[1].Style = "color:red";

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var accordions = component.FindAll(".bit-acd");

        Assert.AreEqual(1, component.FindAll(".bit-acd-des").Count);
        Assert.AreEqual("What B is about", accordions[1].QuerySelector(".bit-acd-des")!.TextContent.Trim());
        Assert.IsTrue(accordions[1].ClassList.Contains("item-b"));
        StringAssert.Contains(accordions[1].GetAttribute("style"), "color:red");

        Assert.IsFalse(accordions[0].ClassList.Contains("item-b"));
        Assert.IsNull(accordions[0].QuerySelector(".bit-acd-des"));
    }

    [TestMethod]
    public void BitAccordionListShouldRaiseTheOnClickOfTheItemAlongWithTheListsOne()
    {
        var items = GetItems();
        BitAccordionListItem? ownClick = null;
        items[1].OnClick = item => ownClick = item;

        BitAccordionListItem? listClick = null;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnItemClick, (BitAccordionListItem item) => listClick = item);
        });

        component.FindAll(".bit-acd-hdr")[1].Click();

        Assert.AreSame(items[1], ownClick);
        Assert.AreSame(items[1], listClick);

        component.FindAll(".bit-acd-hdr")[0].Click();

        // The item's own handler is the item's alone.
        Assert.AreSame(items[1], ownClick);
        Assert.AreSame(items[0], listClick);
    }

    [TestMethod]
    public void BitAccordionListShouldRenderTheBaseParameters()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Id, "faq");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Style, "margin:1px");
            parameters.Add(p => p.Class, "faq-list");
            parameters.Add(p => p.Visibility, BitVisibility.Hidden);
        });

        var root = component.Find(".bit-acl");

        Assert.AreEqual("faq", root.Id);
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("faq-list"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");

        // The direction is handed to every item, which lays out its header and expander by it.
        Assert.AreEqual(3, component.FindAll(".bit-acd[dir=\"rtl\"]").Count);

        component.Render(parameters => parameters.Add(p => p.Visibility, BitVisibility.Collapsed));

        StringAssert.Contains(component.Find(".bit-acl").GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitAccordionListShouldReportTheExpandedStateThroughIsExpanded()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DefaultExpandedKey, "b");
        });

        var list = component.Instance;

        Assert.IsTrue(list.IsExpanded("b"));
        Assert.IsFalse(list.IsExpanded("a"));
        Assert.IsFalse(list.IsExpanded(null));
        Assert.IsFalse(list.IsExpanded(string.Empty));
        Assert.IsFalse(list.IsExpanded("unknown"));

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.IsTrue(list.IsExpanded("a"));
        Assert.IsFalse(list.IsExpanded("b"), "Single-expand mode closes the one that was open.");
    }
}
