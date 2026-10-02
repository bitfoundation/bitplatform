using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccordionList;

/// <summary>
/// Covers the BitParams cascade of the AccordionList: what a BitAccordionListParams fills in, what it leaves alone
/// because the list wrote it for itself, and that what the list derives from its parameters (the expand mode, its
/// classes and its styles, the items it renders) follows the cascaded values.
/// </summary>
[TestClass]
public class BitAccordionListParamsTests : BunitTestContext
{
    // What belongs to a single list rather than to a group of them: its items, its expanded keys, the templates and
    // the selectors typed over its items, and its event callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAccordionList<BitAccordionListItem>.CascadingParameters),
        nameof(BitAccordionList<BitAccordionListItem>.ActionsTemplate),
        nameof(BitAccordionList<BitAccordionListItem>.BodyTemplate),
        nameof(BitAccordionList<BitAccordionListItem>.ChildContent),
        nameof(BitAccordionList<BitAccordionListItem>.DefaultExpandedKey),
        nameof(BitAccordionList<BitAccordionListItem>.DefaultExpandedKeys),
        nameof(BitAccordionList<BitAccordionListItem>.ExpandedKey),
        nameof(BitAccordionList<BitAccordionListItem>.ExpandedKeyChanged),
        nameof(BitAccordionList<BitAccordionListItem>.ExpandedKeys),
        nameof(BitAccordionList<BitAccordionListItem>.ExpandedKeysChanged),
        nameof(BitAccordionList<BitAccordionListItem>.ExpanderTemplate),
        nameof(BitAccordionList<BitAccordionListItem>.HeaderTemplate),
        nameof(BitAccordionList<BitAccordionListItem>.Items),
        nameof(BitAccordionList<BitAccordionListItem>.NameSelectors),
        nameof(BitAccordionList<BitAccordionListItem>.OnCollapse),
        nameof(BitAccordionList<BitAccordionListItem>.OnExpand),
        nameof(BitAccordionList<BitAccordionListItem>.OnItemClick),
        nameof(BitAccordionList<BitAccordionListItem>.OnToggle),
        nameof(BitAccordionList<BitAccordionListItem>.OnToggling),
        nameof(BitAccordionList<BitAccordionListItem>.Options),
        nameof(BitAccordionList<BitAccordionListItem>.TitleTemplate),
    ];

    private static List<BitAccordionListItem> GetItems() =>
    [
        new() { Key = "a", Title = "Item A" },
        new() { Key = "b", Title = "Item B" },
        new() { Key = "c", Title = "Item C" },
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAccordionListParams listParams, Action<RenderTreeBuilder>? extraAttributes = null, List<BitAccordionListItem>? items = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { listParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAccordionList<BitAccordionListItem>>(0);
                builder.AddAttribute(1, nameof(BitAccordionList<BitAccordionListItem>.Items), items ?? GetItems());
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAccordionListParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAccordionList", BitAccordionListParams.ParamName);
    }

    [TestMethod]
    public void BitAccordionListParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitAccordionListParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitAccordionListParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitAccordionListParamsShouldCarryEveryParameterThatBelongsToAGroupOfLists()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAccordionList<BitAccordionListItem>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                                       .Select(p => p.Name)
                                                                       .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAccordionListParams).GetProperty(name), $"BitAccordionListParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAccordionListShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAccordionListParams
        {
            Background = BitColorKind.Tertiary,
            Border = BitColorKind.Secondary,
            ExpanderIconName = "Add",
            ExpanderIconPosition = BitIconPosition.Start,
            HeadingLevel = 2,
            Joined = true,
            NoContentRegion = true,
            Size = BitSize.Large,
            Class = "cascaded",
            Classes = new() { ItemTitle = "cascaded-title" },
            Styles = new() { Root = "margin:1px" },
        });

        var root = component.Find(".bit-acl");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-acl-jnd"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");

        var item = component.FindAll(".bit-acd")[0];

        Assert.IsTrue(item.ClassList.Contains("bit-acd-tbg"));
        Assert.IsTrue(item.ClassList.Contains("bit-acd-sbr"));
        Assert.IsTrue(item.ClassList.Contains("bit-acd-sei"));
        Assert.IsTrue(item.ClassList.Contains("bit-acd-lg"));
        Assert.AreEqual("2", component.FindAll(".bit-acd-hed")[0].GetAttribute("aria-level"));
        Assert.IsNull(component.FindAll(".bit-acd-con")[0].GetAttribute("role"));
        Assert.AreEqual(3, component.FindAll(".bit-acd-ttl.cascaded-title").Count);
        Assert.AreEqual(3, component.FindAll(".bit-acd-eic.bit-icon--Add").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAccordionListParams
        {
            Background = BitColorKind.Tertiary,
            HeadingLevel = 2,
            Multiple = true,
            Joined = true,
            Gap = 20,
            ExpanderIcon = BitIconInfo.Css("cascaded-icon"),
            Class = "cascaded",
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitAccordionList<BitAccordionListItem>.Background), (BitColorKind?)BitColorKind.Primary);
            builder.AddAttribute(3, nameof(BitAccordionList<BitAccordionListItem>.HeadingLevel), (int?)4);
            builder.AddAttribute(4, nameof(BitAccordionList<BitAccordionListItem>.Multiple), false);
            builder.AddAttribute(5, nameof(BitAccordionList<BitAccordionListItem>.Joined), false);
            builder.AddAttribute(6, nameof(BitAccordionList<BitAccordionListItem>.Gap), (int?)4);
            builder.AddAttribute(7, nameof(BitAccordionList<BitAccordionListItem>.ExpanderIconName), "ChevronDown");
            builder.AddAttribute(8, nameof(BitComponentBase.Class), "own");
        });

        var root = component.Find(".bit-acl");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("bit-acl-mlt"));
        Assert.IsFalse(root.ClassList.Contains("bit-acl-jnd"));
        StringAssert.Contains(root.GetAttribute("style"), "gap:4px");

        Assert.IsTrue(component.FindAll(".bit-acd")[0].ClassList.Contains("bit-acd-pbg"));
        Assert.AreEqual("4", component.FindAll(".bit-acd-hed")[0].GetAttribute("aria-level"));

        // A cascaded icon is only a default for a list that names neither the icon nor the icon name.
        Assert.AreEqual(0, component.FindAll(".cascaded-icon").Count);
        Assert.AreEqual(3, component.FindAll(".bit-icon--ChevronDown").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldTakeACascadedMultipleMode()
    {
        var component = RenderWithParams(new BitAccordionListParams { Multiple = true, MaxExpanded = 2 });

        Assert.IsTrue(component.Find(".bit-acl").ClassList.Contains("bit-acl-mlt"));

        component.FindAll(".bit-acd-hdr")[0].Click();
        component.FindAll(".bit-acd-hdr")[1].Click();
        component.FindAll(".bit-acd-hdr")[2].Click();

        var list = component.FindComponent<BitAccordionList<BitAccordionListItem>>().Instance;

        // Multiple keeps the panels open side by side, and the cascaded cap closes the oldest one.
        CollectionAssert.AreEqual(new[] { "b", "c" }, list.GetExpandedKeys().ToArray());
    }

    [TestMethod]
    public void BitAccordionListShouldTakeACascadedReadOnlyAndCollapsible()
    {
        var component = RenderWithParams(new BitAccordionListParams { ReadOnly = true });

        component.FindAll(".bit-acd-hdr")[0].Click();

        Assert.AreEqual("true", component.FindAll(".bit-acd-hdr")[0].GetAttribute("aria-disabled"));
        Assert.AreEqual(0, component.FindAll(".bit-acd-exp").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldTakeACascadedEmptyContent()
    {
        var component = RenderWithParams(new BitAccordionListParams
        {
            EmptyContent = builder => builder.AddMarkupContent(0, "<span class=\"cascaded-empty\">Nothing</span>"),
        }, items: []);

        Assert.AreEqual(1, component.FindAll(".cascaded-empty").Count);
    }

    [TestMethod]
    public void BitAccordionListShouldTakeACascadedGap()
    {
        var component = RenderWithParams(new BitAccordionListParams { Gap = 12 });

        StringAssert.Contains(component.Find(".bit-acl").GetAttribute("style"), "gap:12px");
    }

    [TestMethod]
    public void BitAccordionListShouldReachTheOptionsApiThroughTheCascade()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitAccordionListParams { Multiple = true, Size = BitSize.Small } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAccordionList<BitAccordionListOption>>(0);
                builder.AddAttribute(1, nameof(BitAccordionList<BitAccordionListOption>.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<BitAccordionListOption>(0);
                    b.AddAttribute(1, nameof(BitAccordionListOption.Title), "One");
                    b.CloseComponent();
                    b.OpenComponent<BitAccordionListOption>(2);
                    b.AddAttribute(3, nameof(BitAccordionListOption.Title), "Two");
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        component.FindAll(".bit-acd-hdr")[0].Click();
        component.FindAll(".bit-acd-hdr")[1].Click();

        Assert.AreEqual(2, component.FindAll(".bit-acd-exp").Count);
        Assert.AreEqual(2, component.FindAll(".bit-acd-sm").Count);
    }
}
