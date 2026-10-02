using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccordionList;

/// <summary>
/// Covers HiddenUntilFound on the AccordionList: the list owns the expansion of its items, so a find-in-page reveal
/// goes through it - the single-expand mode, MaxExpanded and OnToggling all have their say - rather than through the
/// accordion of the item alone, which could not move a state it is only handed.
/// </summary>
[TestClass]
public class BitAccordionListHiddenUntilFoundTests : BunitTestContext
{
    private static List<BitAccordionListItem> GetItems() =>
    [
        new() { Key = "a", Title = "Item A", Body = Content("Body A") },
        new() { Key = "b", Title = "Item B", Body = Content("Body B") },
        new() { Key = "c", Title = "Item C", Body = Content("Body C"), IsEnabled = false },
    ];

    private static RenderFragment<BitAccordionListItem> Content(string text) => item => builder => builder.AddContent(0, text);

    [TestMethod]
    public void BitAccordionListHiddenUntilFoundShouldOfferTheCollapsedPanelsToFindInPage()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.HiddenUntilFound, true);
            parameters.Add(p => p.LazyContent, true);
        });

        var panels = component.FindAll(".bit-acd-con");

        Assert.AreEqual("until-found", panels[0].GetAttribute("hidden"));
        Assert.AreEqual("until-found", panels[1].GetAttribute("hidden"));

        // The panel has to be in the DOM to be found, so LazyContent is set aside.
        StringAssert.Contains(panels[0].TextContent, "Body A");

        // A disabled item cannot open around the match, so it is not offered to find-in-page.
        Assert.IsFalse(panels[2].HasAttribute("hidden") && panels[2].GetAttribute("hidden") == "until-found");
    }

    [TestMethod]
    public void BitAccordionListHiddenUntilFoundShouldBeOffByDefault()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters => parameters.Add(p => p.Items, GetItems()));

        Assert.IsFalse(component.Markup.Contains("until-found"));
        Assert.IsFalse(component.Markup.Contains("onbeforematch"));
    }

    [TestMethod]
    public void BitAccordionListHiddenUntilFoundShouldExpandTheItemThroughTheList()
    {
        BitAccordionListToggleArgs<BitAccordionListItem>? toggling = null;
        string? boundKey = "a";

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.HiddenUntilFound, true);
            parameters.Bind(p => p.ExpandedKey, boundKey, v => boundKey = v);
            parameters.Add(p => p.OnToggling, (BitAccordionListToggleArgs<BitAccordionListItem> args) => toggling = args);
        });

        component.FindAll(".bit-acd-con")[1].TriggerEvent("onbeforematch", EventArgs.Empty);

        Assert.IsNotNull(toggling);
        Assert.AreEqual("b", toggling.Key);
        Assert.IsTrue(toggling.IsExpanding);
        Assert.AreEqual(BitAccordionToggleReason.Reveal, toggling.Reason);

        // Single-expand mode closes the panel that was open, and the binding hears about it like a click.
        Assert.AreEqual("b", boundKey);
        CollectionAssert.AreEqual(new[] { "b" }, component.Instance.GetExpandedKeys().ToArray());
        Assert.IsFalse(component.FindAll(".bit-acd-con")[1].HasAttribute("hidden"));
        Assert.AreEqual("until-found", component.FindAll(".bit-acd-con")[0].GetAttribute("hidden"));
    }

    [TestMethod]
    public void BitAccordionListHiddenUntilFoundShouldStopOfferingAPanelWhoseRevealIsRefused()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.HiddenUntilFound, true);
            parameters.Add(p => p.OnToggling, (BitAccordionListToggleArgs<BitAccordionListItem> args) => args.Cancel = true);
        });

        component.FindAll(".bit-acd-con")[0].TriggerEvent("onbeforematch", EventArgs.Empty);

        var panel = component.FindAll(".bit-acd-con")[0];

        Assert.AreEqual(0, component.Instance.GetExpandedKeys().Count);
        Assert.IsFalse(panel.HasAttribute("hidden"));
        Assert.IsTrue(panel.ClassList.Contains("bit-acd-cco"));
    }

    [TestMethod]
    public void BitAccordionListHiddenUntilFoundShouldNotOfferReadOnlyItems()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.HiddenUntilFound, true);
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.IsFalse(component.Markup.Contains("until-found"));
    }

    [TestMethod]
    public void BitAccordionListHiddenUntilFoundShouldWorkWithOptions()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListOption>>(parameters =>
        {
            parameters.Add(p => p.HiddenUntilFound, true);
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<BitAccordionListOption>(0);
                b.AddAttribute(1, nameof(BitAccordionListOption.Key), "one");
                b.AddAttribute(2, nameof(BitAccordionListOption.Title), "One");
                b.CloseComponent();
                b.OpenComponent<BitAccordionListOption>(3);
                b.AddAttribute(4, nameof(BitAccordionListOption.Key), "two");
                b.AddAttribute(5, nameof(BitAccordionListOption.Title), "Two");
                b.CloseComponent();
            }));
        });

        component.FindAll(".bit-acd-con")[1].TriggerEvent("onbeforematch", EventArgs.Empty);

        CollectionAssert.AreEqual(new[] { "two" }, component.Instance.GetExpandedKeys().ToArray());
        Assert.AreEqual("until-found", component.FindAll(".bit-acd-con")[0].GetAttribute("hidden"));
    }
}
