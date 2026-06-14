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
}
