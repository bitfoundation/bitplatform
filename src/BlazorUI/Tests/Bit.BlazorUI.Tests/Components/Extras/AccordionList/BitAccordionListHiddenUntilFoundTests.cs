using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        new() { Key = "c", Title = "Item C", Body = Content("Body C"), IsDisabled = true },
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

    [TestMethod]
    public void BitAccordionListHiddenUntilFoundShouldHoldTheRevealedItemInPlaceOnlyWhenItClosesOthers()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.HiddenUntilFound, true);
            parameters.Add(p => p.ScrollIntoViewOnExpand, true);
        });

        // Nothing is open, so nothing closes above the match and the browser's own scroll is left alone.
        component.FindAll(".bit-acd-con")[0].TriggerEvent("onbeforematch", EventArgs.Empty);

        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Extras.keepInPlace"));

        // Item A closes as B is revealed, so B is held where the browser scrolled it.
        component.FindAll(".bit-acd-con")[1].TriggerEvent("onbeforematch", EventArgs.Empty);

        CollectionAssert.AreEqual(new[] { "b" }, component.Instance.GetExpandedKeys().ToArray());
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Extras.keepInPlace"));

        // A revealed panel is never scrolled to again: the browser has already scrolled to the match inside it.
        component.WaitForAssertion(() => Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Extras.scrollIntoView")));
    }

    [TestMethod]
    public void BitAccordionListHiddenUntilFoundShouldCloseTheOldestPanelBeyondMaxExpanded()
    {
        string[]? boundKeys = null;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitAccordionListItem>
            {
                new() { Key = "a", Title = "Item A" },
                new() { Key = "b", Title = "Item B" },
                new() { Key = "c", Title = "Item C" },
            });
            parameters.Add(p => p.HiddenUntilFound, true);
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxExpanded, 2);
            parameters.Add(p => p.DefaultExpandedKeys, ["a", "b"]);
            parameters.Add(p => p.ExpandedKeysChanged, (IEnumerable<string>? keys) => boundKeys = keys?.ToArray());
        });

        component.FindAll(".bit-acd-con")[2].TriggerEvent("onbeforematch", EventArgs.Empty);

        CollectionAssert.AreEquivalent(new[] { "b", "c" }, component.Instance.GetExpandedKeys().ToArray());
        CollectionAssert.AreEquivalent(new[] { "b", "c" }, boundKeys);
        Assert.AreEqual("until-found", component.FindAll(".bit-acd-con")[0].GetAttribute("hidden"));
    }

    [TestMethod]
    public async Task BitAccordionListHiddenUntilFoundShouldWaitForTheWholeToggleInFlight()
    {
        var toggling = new TaskCompletionSource();
        var expanding = new TaskCompletionSource();
        var events = new List<string>();
        string? boundKey = null;

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.HiddenUntilFound, true);
            parameters.Bind(p => p.ExpandedKey, boundKey, v => { boundKey = v; events.Add($"bound:{v}"); });
            parameters.Add(p => p.OnToggling, EventCallback.Factory.Create<BitAccordionListToggleArgs<BitAccordionListItem>>(this, async args =>
            {
                events.Add($"toggling:{args.Key}");
                if (args.Key == "a") await toggling.Task;
            }));
            parameters.Add(p => p.OnExpand, EventCallback.Factory.Create<BitAccordionListItem>(this, async item =>
            {
                events.Add($"expand:{item.Key}");
                if (item.Key == "a") await expanding.Task;
            }));
            parameters.Add(p => p.OnCollapse, (BitAccordionListItem item) => events.Add($"collapse:{item.Key}"));
        });

        var click = component.InvokeAsync(() => component.FindAll(".bit-acd-hdr")[0].Click());
        var reveal = component.InvokeAsync(() => component.FindAll(".bit-acd-con")[1].TriggerEventAsync("onbeforematch", EventArgs.Empty));

        // The click is waiting on OnToggling, and the reveal waits behind it rather than being turned away.
        Assert.IsFalse(reveal.IsCompleted);

        toggling.SetResult();

        // The click is through OnToggling but still in OnExpand: its state is only half applied, so the reveal
        // keeps on waiting rather than starting a toggle of its own from it.
        component.WaitForAssertion(() => CollectionAssert.Contains(events, "expand:a"));
        Assert.IsFalse(events.Contains("toggling:b"));
        Assert.IsFalse(reveal.IsCompleted);

        expanding.SetResult();

        await click;
        await reveal;

        CollectionAssert.AreEqual(new[] { "toggling:a", "expand:a", "bound:a", "toggling:b", "expand:b", "collapse:a", "bound:b" }, events);
        CollectionAssert.AreEqual(new[] { "b" }, component.Instance.GetExpandedKeys().ToArray());
    }

    [TestMethod]
    public async Task BitAccordionListHiddenUntilFoundShouldNotRevealAnItemTurnedOffWhileWaiting()
    {
        var toggling = new TaskCompletionSource();
        var items = GetItems();

        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.HiddenUntilFound, true);
            parameters.Add(p => p.OnToggling, EventCallback.Factory.Create<BitAccordionListToggleArgs<BitAccordionListItem>>(this, async args =>
            {
                if (args.Key == "a") await toggling.Task;
            }));
        });

        var click = component.InvokeAsync(() => component.FindAll(".bit-acd-hdr")[0].Click());
        var reveal = component.InvokeAsync(() => component.FindAll(".bit-acd-con")[1].TriggerEventAsync("onbeforematch", EventArgs.Empty));

        items[1].IsDisabled = true;

        toggling.SetResult();

        await click;
        await reveal;

        CollectionAssert.AreEqual(new[] { "a" }, component.Instance.GetExpandedKeys().ToArray());
    }
}
