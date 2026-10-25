using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Nav;

[TestClass]
public class BitNavOptionsOrderTests : BunitTestContext
{
    [TestMethod]
    public void BitNavShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally()
    {
        var component = RenderComponent<BitNavOptionsOrderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Child1", "Child2", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "First", "Child1", "ChildMiddle", "Child2", "Middle", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Child1", "Child2", "Last" }, GetItemTexts(component));
    }

    [TestMethod]
    public void BitNavShouldMarkTheOptionsForTheOrderReadBackOnlyWithAutoReorderOptions()
    {
        // The marker is what the DOM read-back recovers the markup order from, so it is only rendered for the
        // navs that asked for the reordering - and for the nested options as much as for the roots.
        var withReorder = RenderComponent<BitNavOptionsOrderTest>(p =>
        {
            p.Add(c => c.ShowMiddle, true);
            p.Add(c => c.AutoReorderOptions, true);
        });

        Assert.AreEqual(6, withReorder.FindAll("[data-bit-nav-opt]").Count);

        var withoutReorder = RenderComponent<BitNavOptionsOrderTest>(p => p.Add(c => c.ShowMiddle, true));

        Assert.AreEqual(0, withoutReorder.FindAll("[data-bit-nav-opt]").Count);
    }

    [TestMethod]
    public void BitNavShouldWalkTheOptionsInTheirRegistrationOrderWithoutAutoReorderOptions()
    {
        // An option that shows up after the first render registers itself at the end of its list, so the
        // keyboard reaches it last even though it is rendered in the middle. That is the very thing
        // AutoReorderOptions is there to correct.
        var component = RenderComponent<BitNavOptionsOrderTest>();
        Remember(component);

        component.Render(p => p.Add(c => c.ShowMiddle, true));
        Remember(component);

        PressKeyOn(component, "Child1", "ArrowDown");
        PressKeyOn(component, "Child2", "ArrowDown");

        AssertFocused(component, "Child2", "ChildMiddle");
    }

    [TestMethod]
    public void BitNavShouldWalkTheOptionsInTheirMarkupOrderWithAutoReorderOptions()
    {
        var component = RenderComponent<BitNavOptionsOrderTest>(p => p.Add(c => c.AutoReorderOptions, true));
        Remember(component);

        component.Render(p => p.Add(c => c.ShowMiddle, true));
        Remember(component);

        // What the browser would have reported: the markers of the options in the order they are rendered.
        var orderedIds = component.FindAll("[data-bit-nav-opt]")
                                  .Select(e => e.GetAttribute("data-bit-nav-opt")!)
                                  .ToArray();

        Assert.AreEqual(6, orderedIds.Length);

        Context.JSInterop.Setup<string[]>("BitBlazorUI.Utils.getChildrenAttributes", _ => true).SetResult(orderedIds);

        // Any render re-runs the read-back, which is what the change that added the options gives the nav in
        // the first place.
        component.Render(p => p.Add(c => c.ShowMiddle, true));
        Remember(component);

        // Both levels follow the markup: the nested option comes between its siblings, the root one between its own.
        PressKeyOn(component, "Child1", "ArrowDown");
        PressKeyOn(component, "Child2", "ArrowDown");
        PressKeyOn(component, "First", "End");

        AssertFocused(component, "ChildMiddle", "Middle", "Last");
    }

    [TestMethod]
    public void BitNavShouldKeepAnOptionThatHasNotRenderedItsMarkerYet()
    {
        // A read-back that only reports a part of the options (one of them has registered but has not rendered
        // its marker yet) leaves the rest at the end of their list rather than dropping them.
        var component = RenderComponent<BitNavOptionsOrderTest>(p => p.Add(c => c.AutoReorderOptions, true));
        Remember(component);

        component.Render(p => p.Add(c => c.ShowMiddle, true));
        Remember(component);

        var reportedIds = component.FindAll("[data-bit-nav-opt]")
                                   .Select(e => e.GetAttribute("data-bit-nav-opt")!)
                                   .Take(3)
                                   .ToArray();

        Context.JSInterop.Setup<string[]>("BitBlazorUI.Utils.getChildrenAttributes", _ => true).SetResult(reportedIds);

        component.Render(p => p.Add(c => c.ShowMiddle, true));
        Remember(component);

        CollectionAssert.AreEqual(new[] { "First", "Child1", "ChildMiddle", "Child2", "Middle", "Last" }, GetItemTexts(component));

        // First, Child1 and ChildMiddle were reported, so ChildMiddle moved ahead of Child2; the roots that were
        // not reported keep the order they registered in.
        PressKeyOn(component, "Child1", "ArrowDown");
        PressKeyOn(component, "First", "End");

        AssertFocused(component, "ChildMiddle", "Middle");
    }

    [TestMethod]
    public void BitNavShouldTakeAutoReorderOptionsFromBitNavParams()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitNavParams { AutoReorderOptions = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitNav<BitNavOption>>(0);
                builder.AddAttribute(1, nameof(BitNav<BitNavOption>.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<BitNavOption>(0);
                    b.AddAttribute(1, nameof(BitNavOption.Text), "First");
                    b.CloseComponent();
                    b.OpenComponent<BitNavOption>(2);
                    b.AddAttribute(3, nameof(BitNavOption.Text), "Last");
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.FindComponent<BitNav<BitNavOption>>().Instance.AutoReorderOptions);
        Assert.AreEqual(2, component.FindAll("[data-bit-nav-opt]").Count);
    }

    private static void PressKeyOn(IRenderedComponent<BitNavOptionsOrderTest> component, string text, string key)
    {
        var item = GetItemElement(component, text);

        item.FocusIn();
        item.KeyDown(new KeyboardEventArgs { Key = key });
    }

    private void AssertFocused(IRenderedComponent<BitNavOptionsOrderTest> component, params string[] texts)
    {
        Remember(component);

        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]
                                       .Select(i => _elementTexts.GetValueOrDefault(((ElementReference)i.Arguments[0]!).Id))
                                       .ToArray();

        CollectionAssert.AreEqual(texts, focused);
    }

    // The element reference each item rendered, by its text. bUnit writes the id of a captured reference into the
    // markup of the render that captured it only, so the ids are collected render by render.
    private readonly Dictionary<string, string> _elementTexts = [];

    private void Remember(IRenderedComponent<BitNavOptionsOrderTest> component)
    {
        foreach (var element in component.FindAll(".bit-nav-ict"))
        {
            var id = element.GetAttribute("blazor:elementReference");
            var text = element.QuerySelector(".bit-nav-itx")?.TextContent;
            if (string.IsNullOrEmpty(id) || text is null) continue;

            _elementTexts[id] = text;
        }
    }

    private static AngleSharp.Dom.IElement GetItemElement(IRenderedComponent<BitNavOptionsOrderTest> component, string text)
    {
        return component.FindAll(".bit-nav-ict").Single(e => e.QuerySelector(".bit-nav-itx")?.TextContent == text);
    }

    private static string[] GetItemTexts(IRenderedComponent<BitNavOptionsOrderTest> component)
    {
        return component.FindAll(".bit-nav-itx").Select(e => e.TextContent).ToArray();
    }
}
