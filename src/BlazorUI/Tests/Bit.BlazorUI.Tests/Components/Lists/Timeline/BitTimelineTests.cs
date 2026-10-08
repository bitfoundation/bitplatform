using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Timeline;

[TestClass]
public class BitTimelineTests : BunitTestContext
{
    private class TimelineEvent
    {
        public string? FirstText { get; set; }
        public string? SecondText { get; set; }
        public string? IconClass { get; set; }
        public bool Off { get; set; }
        public bool Flipped { get; set; }
        public bool NoDot { get; set; }
        public BitColor? DotColor { get; set; }
        public BitSize? DotSize { get; set; }
        public BitVariant? DotVariant { get; set; }
        public BitLineStyle? LineStyle { get; set; }
        public string? Label { get; set; }
        public string? Tooltip { get; set; }
        public Action<TimelineEvent>? Select { get; set; }
    }

    // Mirrors the property names of BitTimelineItem, so the timeline reads it through reflection alone.
    private class TimelineRecord
    {
        public string? PrimaryText { get; set; }
        public BitLineStyle? LineStyle { get; set; }
        public bool HideDot { get; set; }
    }

    // Counts how many times the timeline walks the collection it is given, so a lazily evaluated Items
    // (a LINQ query, an iterator, ...) is known not to be enumerated more than once per parameter set.
    private class CountingSource(IEnumerable<BitTimelineItem> items) : IEnumerable<BitTimelineItem>
    {
        public int EnumerationCount { get; private set; }

        public IEnumerator<BitTimelineItem> GetEnumerator()
        {
            EnumerationCount++;
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static BitTimelineNameSelectors<TimelineEvent> EventSelectors => new()
    {
        PrimaryText = { Selector = i => i.FirstText },
        SecondaryText = { Selector = i => i.SecondText },
        IconName = { Selector = i => i.IconClass },
        IsDisabled = { Selector = i => i.Off },
        Reversed = { Selector = i => i.Flipped },
        HideDot = { Selector = i => i.NoDot },
        Color = { Selector = i => i.DotColor },
        Size = { Selector = i => i.DotSize },
        Variant = { Selector = i => i.DotVariant },
        LineStyle = { Selector = i => i.LineStyle },
        AriaLabel = { Selector = i => i.Label },
        Title = { Selector = i => i.Tooltip },
        OnClick = { Selector = i => i.Select },
    };

    private static Action<ComponentParameterCollectionBuilder<BitTimeline<BitTimelineOption>>> TwoOptions(
        Action<RenderTreeBuilder>? extra = null)
    {
        return parameters => parameters.AddChildContent(builder =>
        {
            builder.OpenComponent<BitTimelineOption>(0);
            builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), "First");
            builder.AddAttribute(2, nameof(BitTimelineOption.SecondaryText), "First second");
            builder.CloseComponent();

            builder.OpenComponent<BitTimelineOption>(3);
            builder.AddAttribute(4, nameof(BitTimelineOption.PrimaryText), "Second");
            builder.AddAttribute(5, nameof(BitTimelineOption.SecondaryText), "Second second");
            builder.CloseComponent();

            extra?.Invoke(builder);
        });
    }



    #region rendering

    [TestMethod]
    public void BitTimelineShouldRenderItemsFromChildContent()
    {
        var component = RenderComponent(TwoOptions());

        var items = component.FindAll(".bit-tln-itm");

        Assert.AreEqual(2, items.Count);

        Assert.IsTrue(component.Markup.Contains("First"));
        Assert.IsTrue(component.Markup.Contains("Second"));
    }

    [TestMethod]
    public void BitTimelineShouldRenderItemsFromOptionsParameter()
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.Options, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), "Option");
                builder.CloseComponent();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-tln-itm").Count);
        Assert.IsTrue(component.Markup.Contains("Option"));
    }

    [TestMethod]
    public void BitTimelineShouldUpdateARenderedOptionWhenItsOwnParametersChange()
    {
        static Action<ComponentParameterCollectionBuilder<BitTimeline<BitTimelineOption>>> OneOption(string text)
        {
            return parameters => parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), text);
                builder.CloseComponent();
            });
        }

        var component = RenderComponent(OneOption("First"));

        Assert.AreEqual("First", component.Find(".bit-tln-pcn .bit-tln-ttx").TextContent);

        component.Render(OneOption("Changed"));

        Assert.AreEqual("Changed", component.Find(".bit-tln-pcn .bit-tln-ttx").TextContent);
    }

    [TestMethod]
    public void BitTimelineShouldUpdateARenderedOptionWhenTheTimelineParametersChange()
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.Styles, new BitTimelineClassStyles { Item = "color: green" });
            TwoOptions()(parameters);
        });

        Assert.AreEqual("color: green", component.FindAll(".bit-tln-itm")[0].GetAttribute("style"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Styles, new BitTimelineClassStyles { Item = "color: red" });
            TwoOptions()(parameters);
        });

        Assert.AreEqual("color: red", component.FindAll(".bit-tln-itm")[0].GetAttribute("style"));
    }

    [TestMethod]
    public void BitTimelineShouldRenderItemsFromItemsParameter()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem { PrimaryText = "One", SecondaryText = "One second" },
                new BitTimelineItem { PrimaryText = "Two" }
            ]);
        });

        var texts = component.FindAll(".bit-tln-pcn .bit-tln-ttx").Select(e => e.TextContent).ToArray();

        CollectionAssert.AreEqual(new[] { "One", "Two" }, texts);
        Assert.AreEqual("One second", component.Find(".bit-tln-scn .bit-tln-ttx").TextContent);
    }

    [TestMethod]
    public void BitTimelineShouldNotRenderTextElementWhenTextIsEmpty()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "Only primary" }]);
        });

        // The secondary side has no text, so no empty span is emitted for it.
        Assert.AreEqual(1, component.FindAll(".bit-tln-ttx").Count);
        Assert.AreEqual(0, component.Find(".bit-tln-scn").ChildElementCount);
    }

    [TestMethod]
    public void BitTimelineShouldClearItemsWhenTheCollectionBecomesEmpty()
    {
        var items = new List<BitTimelineItem> { new() { PrimaryText = "One" } };

        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual(1, component.FindAll(".bit-tln-itm").Count);

        component.Render(parameters => parameters.Add(p => p.Items, new List<BitTimelineItem>()));

        Assert.AreEqual(0, component.FindAll(".bit-tln-itm").Count);
    }

    [TestMethod]
    public void BitTimelineShouldWalkTheItemsCollectionOnlyOncePerParameterSet()
    {
        var source = new CountingSource([new BitTimelineItem { PrimaryText = "One" }]);

        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, source);
        });

        Assert.AreEqual(1, component.FindAll(".bit-tln-itm").Count);
        Assert.AreEqual(1, source.EnumerationCount);
    }

    [TestMethod]
    public void BitTimelineShouldDetectInPlaceChangesOfTheSameItemsInstance()
    {
        var items = new List<BitTimelineItem> { new() { PrimaryText = "One" } };

        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        items.Add(new BitTimelineItem { PrimaryText = "Two" });

        component.Render(parameters => parameters.Add(p => p.Items, items));

        Assert.AreEqual(2, component.FindAll(".bit-tln-itm").Count);
    }

    #endregion

    #region root

    [TestMethod]
    public void BitTimelineShouldRenderTheRootAsAList()
    {
        var component = RenderComponent(TwoOptions());

        var root = component.Find(".bit-tln");

        // An ordered list, which keeps its list role spelled out for WebKit (it hides the markers).
        Assert.AreEqual("OL", root.TagName);
        Assert.AreEqual("list", root.GetAttribute("role"));
    }

    [TestMethod]
    public void BitTimelineShouldRenderTheAriaLabelOfTheRoot()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "The history");
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        Assert.AreEqual("The history", component.Find(".bit-tln").GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTimelineShouldRespectDisabled(bool isEnabled)
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
            TwoOptions()(parameters);
        });

        var root = component.Find(".bit-tln");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
            // aria-disabled does not apply to a list: the items announce the state themselves.
            Assert.IsNull(root.GetAttribute("aria-disabled"));
        }

        foreach (var item in component.FindAll(".bit-tln-itm"))
        {
            Assert.AreEqual(isEnabled ? null : "true", item.GetAttribute("aria-disabled"));
        }
    }

    [TestMethod]
    public void BitTimelineShouldApplyRootClassesFromParameters()
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Secondary);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Variant, BitVariant.Outline);
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.Alternate, true);
            parameters.Add(p => p.ReverseOrder, true);
            TwoOptions()(parameters);
        });

        var root = component.Find(".bit-tln");

        Assert.IsTrue(root.ClassList.Contains("bit-tln-sec"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-hrz"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-rvs"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-alt"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-rvo"));
    }

    [TestMethod]
    public void BitTimelineShouldApplyTheDefaultRootClassesWhenNothingIsProvided()
    {
        var component = RenderComponent(TwoOptions());

        var root = component.Find(".bit-tln");

        // An unset Color or Size publishes no class, so the public --bit-Timeline-* variables restyle the primary,
        // medium timeline they stand for while an explicit value - which does publish its class - wins over them.
        foreach (var cssClass in new[] { "bit-tln-pri", "bit-tln-sec", "bit-tln-ter", "bit-tln-inf", "bit-tln-suc", "bit-tln-wrn", "bit-tln-swr", "bit-tln-err",
                                         "bit-tln-pbg", "bit-tln-sbg", "bit-tln-tbg", "bit-tln-pfg", "bit-tln-sfg", "bit-tln-tfg", "bit-tln-pbr", "bit-tln-sbr", "bit-tln-tbr",
                                         "bit-tln-sm", "bit-tln-md", "bit-tln-lg" })
        {
            Assert.IsFalse(root.ClassList.Contains(cssClass), $"An unset parameter published {cssClass}.");
        }

        Assert.IsTrue(root.ClassList.Contains("bit-tln-fil"));
        Assert.IsFalse(root.ClassList.Contains("bit-tln-hrz"));
        Assert.IsFalse(root.ClassList.Contains("bit-tln-rvs"));
        Assert.IsFalse(root.ClassList.Contains("bit-tln-alt"));
        Assert.IsFalse(root.ClassList.Contains("bit-tln-rvo"));
    }

    [TestMethod,
        DataRow(BitTimelineTruncateLine.None, false, null),
        DataRow(BitTimelineTruncateLine.Start, false, "bit-tln-tls"),
        DataRow(BitTimelineTruncateLine.End, false, "bit-tln-tle"),
        DataRow(BitTimelineTruncateLine.Both, false, "bit-tln-tlb"),
        // Start and End keep referring to the first and the last item of the list in a reversed order,
        // where only the position of those items in the timeline (and so the painted half of their
        // connector, which the stylesheet flips) changes.
        DataRow(BitTimelineTruncateLine.Start, true, "bit-tln-tls"),
        DataRow(BitTimelineTruncateLine.End, true, "bit-tln-tle"),
        DataRow(BitTimelineTruncateLine.Both, true, "bit-tln-tlb")]
    public void BitTimelineShouldApplyTheTruncateLineClass(BitTimelineTruncateLine truncate, bool reverseOrder, string? expectedClass)
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.TruncateLine, truncate);
            parameters.Add(p => p.ReverseOrder, reverseOrder);
            TwoOptions()(parameters);
        });

        var root = component.Find(".bit-tln");

        foreach (var cls in new[] { "bit-tln-tls", "bit-tln-tle", "bit-tln-tlb" })
        {
            Assert.AreEqual(cls == expectedClass, root.ClassList.Contains(cls), cls);
        }

        // The stylesheet flips the truncated half of the connector by combining the truncation class
        // with the reverse-order one, so the pair has to be present together on the root.
        Assert.AreEqual(reverseOrder, root.ClassList.Contains("bit-tln-rvo"));
    }

    [TestMethod,
        // A solid line is the default paint of the stylesheet, so it carries no class of its own.
        DataRow(null, null),
        DataRow(BitLineStyle.Solid, null),
        DataRow(BitLineStyle.Dashed, "bit-tln-ldd"),
        DataRow(BitLineStyle.Dotted, "bit-tln-ldt")]
    public void BitTimelineShouldApplyTheLineVariantClass(BitLineStyle? lineVariant, string? expectedClass)
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.LineStyle, lineVariant);
            TwoOptions()(parameters);
        });

        var root = component.Find(".bit-tln");

        foreach (var cls in new[] { "bit-tln-ldd", "bit-tln-ldt" })
        {
            Assert.AreEqual(cls == expectedClass, root.ClassList.Contains(cls), cls);
        }
    }

    [TestMethod,
        // A centered line is the default layout of the stylesheet, so it carries no class of its own.
        DataRow(null, null),
        DataRow(BitPlacement.Center, null),
        DataRow(BitPlacement.Start, "bit-tln-lps"),
        DataRow(BitPlacement.End, "bit-tln-lpe"),
        // A value the line does not honour renders the default centered line, so it carries no class either.
        DataRow(BitPlacement.Left, null),
        DataRow(BitPlacement.TopAndBottom, null)]
    public void BitTimelineShouldApplyTheLinePlacementClass(BitPlacement? linePlacement, string? expectedClass)
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.LinePlacement, linePlacement);
            TwoOptions()(parameters);
        });

        var root = component.Find(".bit-tln");

        foreach (var cls in new[] { "bit-tln-lps", "bit-tln-lpe" })
        {
            Assert.AreEqual(cls == expectedClass, root.ClassList.Contains(cls), cls);
        }
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-tln-pri", "bit-tln-ipr"),
        DataRow(BitColor.Error, "bit-tln-err", "bit-tln-ier"),
        DataRow(BitColor.PrimaryBackground, "bit-tln-pbg", "bit-tln-ipbg"),
        DataRow(BitColor.SecondaryBackground, "bit-tln-sbg", "bit-tln-isbg"),
        DataRow(BitColor.TertiaryBackground, "bit-tln-tbg", "bit-tln-itbg"),
        DataRow(BitColor.PrimaryForeground, "bit-tln-pfg", "bit-tln-ipfg"),
        DataRow(BitColor.SecondaryForeground, "bit-tln-sfg", "bit-tln-isfg"),
        DataRow(BitColor.TertiaryForeground, "bit-tln-tfg", "bit-tln-itfg"),
        DataRow(BitColor.PrimaryBorder, "bit-tln-pbr", "bit-tln-ipbr"),
        DataRow(BitColor.SecondaryBorder, "bit-tln-sbr", "bit-tln-isbr"),
        DataRow(BitColor.TertiaryBorder, "bit-tln-tbr", "bit-tln-itbr")]
    public void BitTimelineShouldApplyTheColorClassOfTheTimelineAndOfTheItem(BitColor color, string rootClass, string itemClass)
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Color, color);
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", Color = color }]);
        });

        Assert.IsTrue(component.Find(".bit-tln").ClassList.Contains(rootClass));
        Assert.IsTrue(component.Find(".bit-tln-itm").ClassList.Contains(itemClass));
    }

    [TestMethod]
    public void BitTimelineShouldApplyStyleAndClassOfTheComponent()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Style, "color: red");
            parameters.Add(p => p.Class, "custom-root");
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        var root = component.Find(".bit-tln");

        Assert.IsTrue(root.GetAttribute("style")!.Contains("color: red"));
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
    }

    #endregion

    #region items

    [TestMethod]
    public void BitTimelineShouldApplyItemLevelClasses()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem
                {
                    PrimaryText = "One",
                    Color = BitColor.Success,
                    Size = BitSize.Large,
                    Variant = BitVariant.Outline,
                    Reversed = true,
                    Class = "my-item"
                }
            ]);
        });

        var item = component.Find(".bit-tln-itm");

        Assert.IsTrue(item.ClassList.Contains("bit-tln-isu"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ilg"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ivo"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-irv"));
        Assert.IsTrue(item.ClassList.Contains("my-item"));
    }

    [TestMethod,
        // Unlike the timeline-level solid line, an explicitly solid item needs a class of its own so that
        // it can win over a dashed or a dotted timeline.
        DataRow(null, null),
        DataRow(BitLineStyle.Solid, "bit-tln-ils"),
        DataRow(BitLineStyle.Dashed, "bit-tln-ild"),
        DataRow(BitLineStyle.Dotted, "bit-tln-ilt")]
    public void BitTimelineShouldApplyTheLineVariantClassOfTheItem(BitLineStyle? lineVariant, string? expectedClass)
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.LineStyle, BitLineStyle.Dashed);
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", LineStyle = lineVariant }]);
        });

        var item = component.Find(".bit-tln-itm");

        foreach (var cls in new[] { "bit-tln-ils", "bit-tln-ild", "bit-tln-ilt" })
        {
            Assert.AreEqual(cls == expectedClass, item.ClassList.Contains(cls), cls);
        }

        // The item-level class only overrides the timeline-level one, which stays on the root either way.
        Assert.IsTrue(component.Find(".bit-tln").ClassList.Contains("bit-tln-ldd"));
    }

    [TestMethod]
    public void BitTimelineShouldApplyTheLineVariantClassOfTheOption()
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), "First");
                builder.AddAttribute(2, nameof(BitTimelineOption.LineStyle), BitLineStyle.Dotted);
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-tln-itm").ClassList.Contains("bit-tln-ilt"));
    }

    [TestMethod]
    public void BitTimelineShouldKeepAnExplicitlyReversedItemOutOfTheAlternation()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Alternate, true);
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem { PrimaryText = "One", Reversed = true },
                new BitTimelineItem { PrimaryText = "Two" }
            ]);
        });

        var items = component.FindAll(".bit-tln-itm");

        // The alternation is painted from the DOM position by the stylesheet, which excludes the items that
        // carry the reversed class, so both markers have to reach the item for that rule to be able to fire.
        Assert.IsTrue(component.Find(".bit-tln").ClassList.Contains("bit-tln-alt"));
        Assert.IsTrue(items[0].ClassList.Contains("bit-tln-irv"));
        Assert.IsFalse(items[1].ClassList.Contains("bit-tln-irv"));
    }

    [TestMethod]
    public void BitTimelineShouldNotRenderAStyleAttributeForAnItemThatCarriesNoStyle()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        Assert.IsNull(component.Find(".bit-tln-itm").GetAttribute("style"));
    }

    [TestMethod]
    public void BitTimelineShouldCombineTheStyleOfTheItemWithTheStyleOfTheParts()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Styles, new BitTimelineClassStyles { Item = "color: green" });
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem { PrimaryText = "One", Style = "color: blue" },
                new BitTimelineItem { PrimaryText = "Two" }
            ]);
        });

        var items = component.FindAll(".bit-tln-itm");

        Assert.AreEqual("color: blue;color: green", items[0].GetAttribute("style"));
        Assert.AreEqual("color: green", items[1].GetAttribute("style"));
    }

    [TestMethod]
    public void BitTimelineShouldApplyStyleAndTitleAndAriaLabelOfTheItem()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem
                {
                    PrimaryText = "One",
                    Style = "color: blue",
                    Title = "The tooltip",
                    AriaLabel = "The label"
                }
            ]);
        });

        var item = component.Find(".bit-tln-itm");

        Assert.IsTrue(item.GetAttribute("style")!.Contains("color: blue"));
        Assert.AreEqual("The tooltip", item.GetAttribute("title"));
        Assert.AreEqual("The label", item.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitTimelineShouldRenderDotOrHideWhenRequested()
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(TwoOptions(builder =>
        {
            builder.OpenComponent<BitTimelineOption>(6);
            builder.AddAttribute(7, nameof(BitTimelineOption.HideDot), true);
            builder.AddAttribute(8, nameof(BitTimelineOption.PrimaryText), "HideDot");
            builder.CloseComponent();
        }));

        Assert.AreEqual(3, component.FindAll(".bit-tln-itm").Count);
        Assert.AreEqual(2, component.FindAll(".bit-tln-dot").Count);

        // The item without a dot still reserves its space, so its connector keeps the axis of the
        // rest of the timeline, and the placeholder stays out of the accessibility tree.
        var placeholders = component.FindAll(".bit-tln-hdd");

        Assert.AreEqual(1, placeholders.Count);
        Assert.AreEqual("true", placeholders[0].GetAttribute("aria-hidden"));
        Assert.IsTrue(component.FindAll(".bit-tln-itm")[2].QuerySelector(".bit-tln-hdd") is not null);

        // The divider of the hidden dot is marked, so the two halves of its line meet across the placeholder.
        var dividers = component.FindAll(".bit-tln-dvd");

        Assert.IsFalse(dividers[0].ClassList.Contains("bit-tln-dvh"));
        Assert.IsFalse(dividers[1].ClassList.Contains("bit-tln-dvh"));
        Assert.IsTrue(dividers[2].ClassList.Contains("bit-tln-dvh"));
    }

    [TestMethod]
    public void BitTimelineShouldRenderIconWhenProvided()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", IconName = "Add" }]);
        });

        var icon = component.Find(".bit-tln-ico");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitTimelineIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem
                {
                    PrimaryText = "One",
                    IconName = "Add",
                    Icon = BitIconInfo.Css("fa-solid fa-plus")
                }
            ]);
        });

        var icon = component.Find(".bit-tln-ico");

        Assert.IsTrue(icon.ClassList.Contains("fa-plus"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitTimelineShouldUseTheKeyOfTheItem()
    {
        var items = new List<BitTimelineItem>
        {
            new() { Key = "a", PrimaryText = "One", Class = "item-a" },
            new() { Key = "b", PrimaryText = "Two", Class = "item-b" }
        };

        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var markersBefore = component.FindAll(".bit-tln-itm").Select(e => e.ClassName).ToArray();

        Assert.IsTrue(markersBefore[0]!.Contains("item-a"));
        Assert.IsTrue(markersBefore[1]!.Contains("item-b"));

        // Reordering with stable keys moves the items, instead of keeping them in place and swapping
        // only their contents, so the per-item markers travel along with the texts.
        component.Render(parameters => parameters.Add(p => p.Items, items.AsEnumerable().Reverse().ToList()));

        var rendered = component.FindAll(".bit-tln-itm");

        Assert.IsTrue(rendered[0].ClassList.Contains("item-b"));
        Assert.IsTrue(rendered[1].ClassList.Contains("item-a"));
        Assert.AreEqual("Two", rendered[0].QuerySelector(".bit-tln-pcn .bit-tln-ttx")!.TextContent);
        Assert.AreEqual("One", rendered[1].QuerySelector(".bit-tln-pcn .bit-tln-ttx")!.TextContent);

        var texts = component.FindAll(".bit-tln-pcn .bit-tln-ttx").Select(e => e.TextContent).ToArray();

        CollectionAssert.AreEqual(new[] { "Two", "One" }, texts);
    }

    #endregion

    #region interactivity

    [TestMethod]
    public void BitTimelineShouldRenderNonInteractiveItemsAsListItems()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        var item = component.Find(".bit-tln-itm");

        Assert.AreEqual("LI", item.TagName);
        Assert.IsNull(item.GetAttribute("tabindex"));
        Assert.IsFalse(item.ClassList.Contains("bit-tln-int"));
        // A presentational item hosts no button at all, so it stays out of the tab order entirely.
        Assert.AreEqual(0, component.FindAll(".bit-tln-btn").Count);
    }

    [TestMethod]
    public void BitTimelineShouldRenderClickableItemsAsButtons()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineItem _) => { });
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", AriaLabel = "The label" }]);
        });

        var item = component.Find(".bit-tln-itm");
        var button = component.Find(".bit-tln-btn");

        // The item keeps the list semantics and the nested element carries the button ones.
        Assert.AreEqual("LI", item.TagName);
        Assert.IsNull(item.GetAttribute("tabindex"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-int"));

        Assert.AreEqual("button", button.GetAttribute("role"));
        Assert.AreEqual("0", button.GetAttribute("tabindex"));
        Assert.AreEqual("The label", button.GetAttribute("aria-label"));
        Assert.IsNull(item.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitTimelineShouldRenderAnItemWithItsOwnClickHandlerAsAButton()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem { PrimaryText = "One", OnClick = _ => { } },
                new BitTimelineItem { PrimaryText = "Two" }
            ]);
        });

        var items = component.FindAll(".bit-tln-itm");

        Assert.AreEqual("LI", items[0].TagName);
        Assert.AreEqual("LI", items[1].TagName);

        // Only the item that answers to a click hosts a button.
        Assert.AreEqual(1, component.FindAll(".bit-tln-btn").Count);
        Assert.AreEqual(1, items[0].QuerySelectorAll(".bit-tln-btn").Length);
        Assert.AreEqual(0, items[1].QuerySelectorAll(".bit-tln-btn").Length);
    }

    [TestMethod]
    public void BitTimelineShouldNotMakeADisabledItemFocusable()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineItem _) => { });
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", IsDisabled = true }]);
        });

        var item = component.Find(".bit-tln-itm");
        var button = component.Find(".bit-tln-btn");

        Assert.AreEqual("button", button.GetAttribute("role"));
        Assert.IsNull(button.GetAttribute("tabindex"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        // The button announces the state, so the item does not announce it a second time.
        Assert.IsNull(item.GetAttribute("aria-disabled"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ids"));
    }

    [TestMethod]
    public void BitTimelineShouldAnnounceTheDisabledStateOfANonInteractiveItem()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem { PrimaryText = "One", IsDisabled = true },
                new BitTimelineItem { PrimaryText = "Two" }
            ]);
        });

        var items = component.FindAll(".bit-tln-itm");

        // With no button to carry it, the item itself is all there is to announce the state.
        Assert.AreEqual("true", items[0].GetAttribute("aria-disabled"));
        Assert.IsNull(items[1].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitTimelineItemClickShouldInvokeCallbackWhenEnabled()
    {
        BitTimelineOption? clicked = null;

        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineOption item) => clicked = item);
            TwoOptions()(parameters);
        });

        component.Find(".bit-tln-btn").Click();

        Assert.IsNotNull(clicked);
        Assert.AreEqual("First", clicked!.PrimaryText);
    }

    [TestMethod]
    public void BitTimelineShouldInvokeBothTheTimelineAndTheItemClickHandlers()
    {
        var clickedItem = string.Empty;
        var onItemClickCalled = false;

        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineItem _) => onItemClickCalled = true);
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", OnClick = i => clickedItem = i.PrimaryText! }]);
        });

        component.Find(".bit-tln-btn").Click();

        Assert.IsTrue(onItemClickCalled);
        Assert.AreEqual("One", clickedItem);
    }

    [TestMethod]
    public void BitTimelineDisabledItemShouldNotInvokeCallback()
    {
        BitTimelineOption? clicked = null;

        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineOption item) => clicked = item);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.IsDisabled), true);
                builder.AddAttribute(2, nameof(BitTimelineOption.PrimaryText), "First");
                builder.CloseComponent();
            });
        });

        var item = component.Find(".bit-tln-itm");

        component.Find(".bit-tln-btn").Click();

        Assert.IsNull(clicked);
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ids"));
    }

    [TestMethod]
    public void BitTimelineDisabledComponentShouldNotInvokeCallback()
    {
        BitTimelineOption? clicked = null;

        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.OnItemClick, (BitTimelineOption item) => clicked = item);
            TwoOptions()(parameters);
        });

        component.Find(".bit-tln-btn").Click();

        Assert.IsNull(clicked);
    }

    [TestMethod]
    public void BitTimelineShouldRegisterTheButtonKeysOfAClickableItemInTheBrowser()
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineOption _) => { });
            TwoOptions()(parameters);
        });

        // The keys are wired up in the browser, where a key pressed on the item can be told apart from one typed
        // into a control of a custom template; the item holds no keyboard handler of its own to fire twice.
        Assert.IsTrue(component.FindAll(".bit-tln-btn").All(b => b.HasAttribute("blazor:onkeydown") is false));
        Assert.AreEqual(2, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Utils.registerButtonKeys"));

        component.Render();

        // A re-render keeps the same elements, so they are not registered again.
        Assert.AreEqual(2, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Utils.registerButtonKeys"));
    }

    [TestMethod]
    public void BitTimelineShouldNotRegisterButtonKeysForANonInteractiveItem()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        var item = component.Find(".bit-tln-itm");

        // A presentational item hosts no button, so there is nothing that answers to the keyboard and a
        // key press of the page is never swallowed by the timeline.
        Assert.AreEqual("LI", item.TagName);
        Assert.AreEqual(0, component.FindAll(".bit-tln-btn").Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Utils.registerButtonKeys"));
    }

    #endregion

    #region templates

    [TestMethod]
    public void BitTimelineShouldRenderTheTemplateOfTheItemInsteadOfItsContents()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem
                {
                    PrimaryText = "One",
                    Template = item => builder =>
                    {
                        builder.OpenElement(0, "span");
                        builder.AddAttribute(1, "class", "the-template");
                        builder.AddContent(2, item.PrimaryText);
                        builder.CloseElement();
                    }
                }
            ]);
        });

        Assert.AreEqual("One", component.Find(".the-template").TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-tln-dvd").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tln-pcn").Count);
        // The template brings its own layout, so the item is not marked as holding the parts.
        Assert.AreEqual(0, component.FindAll(".bit-tln-prt").Count);
    }

    [TestMethod]
    public void BitTimelineShouldMarkWhateverHoldsThePartsOfTheItem()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem { PrimaryText = "One" },
                new BitTimelineItem { PrimaryText = "Two", OnClick = _ => { } }
            ]);
        });

        var items = component.FindAll(".bit-tln-itm");

        // A presentational item holds its parts itself, a clickable one through its button.
        Assert.IsTrue(items[0].ClassList.Contains("bit-tln-prt"));
        Assert.IsFalse(items[1].ClassList.Contains("bit-tln-prt"));
        Assert.IsTrue(component.Find(".bit-tln-btn").ClassList.Contains("bit-tln-prt"));
    }

    [TestMethod]
    public void BitTimelineShouldFallBackToTheItemTemplateOfTheComponent()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.ItemTemplate, item => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "shared-template");
                builder.AddContent(2, item.PrimaryText);
                builder.CloseElement();
            });
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem { PrimaryText = "One" },
                new BitTimelineItem
                {
                    PrimaryText = "Two",
                    Template = item => builder =>
                    {
                        builder.OpenElement(0, "span");
                        builder.AddAttribute(1, "class", "own-template");
                        builder.CloseElement();
                    }
                }
            ]);
        });

        Assert.AreEqual(1, component.FindAll(".shared-template").Count);
        Assert.AreEqual("One", component.Find(".shared-template").TextContent);
        Assert.AreEqual(1, component.FindAll(".own-template").Count);
    }

    [TestMethod]
    public void BitTimelineShouldRenderTheDotTemplateInsteadOfTheDot()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem
                {
                    PrimaryText = "One",
                    DotTemplate = item => builder =>
                    {
                        builder.OpenElement(0, "span");
                        builder.AddAttribute(1, "class", "the-dot");
                        builder.CloseElement();
                    }
                }
            ]);
        });

        Assert.AreEqual(1, component.FindAll(".the-dot").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tln-dot").Count);
    }

    [TestMethod]
    public void BitTimelineShouldFallBackToTheDotTemplateOfTheComponent()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.DotTemplate, item => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "shared-dot");
                builder.CloseElement();
            });
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem { PrimaryText = "One" },
                new BitTimelineItem { PrimaryText = "Two", HideDot = true }
            ]);
        });

        // The hidden dot stays hidden even when the timeline provides a dot template.
        Assert.AreEqual(1, component.FindAll(".shared-dot").Count);
    }

    [TestMethod]
    public void BitTimelineShouldRenderThePrimaryAndSecondaryContents()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items,
            [
                new BitTimelineItem
                {
                    PrimaryText = "ignored",
                    SecondaryText = "ignored",
                    PrimaryContent = item => builder =>
                    {
                        builder.OpenElement(0, "span");
                        builder.AddAttribute(1, "class", "the-primary");
                        builder.CloseElement();
                    },
                    SecondaryContent = item => builder =>
                    {
                        builder.OpenElement(0, "span");
                        builder.AddAttribute(1, "class", "the-secondary");
                        builder.CloseElement();
                    }
                }
            ]);
        });

        Assert.AreEqual(1, component.FindAll(".bit-tln-pcn .the-primary").Count);
        Assert.AreEqual(1, component.FindAll(".bit-tln-scn .the-secondary").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tln-ttx").Count);
    }

    #endregion

    #region classes and styles

    [TestMethod]
    public void BitTimelineShouldApplyTheClassesOfTheParts()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitTimelineClassStyles
            {
                Root = "c-root",
                Item = "c-item",
                PrimaryContent = "c-pcn",
                PrimaryText = "c-ptx",
                SecondaryContent = "c-scn",
                SecondaryText = "c-stx",
                Divider = "c-dvd",
                Dot = "c-dot",
                Icon = "c-ico"
            });
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", SecondaryText = "Two", IconName = "Add" }]);
        });

        Assert.IsTrue(component.Find(".bit-tln").ClassList.Contains("c-root"));
        Assert.IsTrue(component.Find(".bit-tln-itm").ClassList.Contains("c-item"));
        Assert.IsTrue(component.Find(".bit-tln-pcn").ClassList.Contains("c-pcn"));
        Assert.IsTrue(component.Find(".bit-tln-pcn .bit-tln-ttx").ClassList.Contains("c-ptx"));
        Assert.IsTrue(component.Find(".bit-tln-scn").ClassList.Contains("c-scn"));
        Assert.IsTrue(component.Find(".bit-tln-scn .bit-tln-ttx").ClassList.Contains("c-stx"));
        Assert.IsTrue(component.Find(".bit-tln-dvd").ClassList.Contains("c-dvd"));
        Assert.IsTrue(component.Find(".bit-tln-dot").ClassList.Contains("c-dot"));
        Assert.IsTrue(component.Find(".bit-tln-ico").ClassList.Contains("c-ico"));
    }

    [TestMethod]
    public void BitTimelineShouldApplyTheStylesOfTheParts()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Styles, new BitTimelineClassStyles
            {
                Root = "color: red",
                Item = "color: green",
                PrimaryContent = "color: blue",
                PrimaryText = "color: teal",
                SecondaryContent = "color: olive",
                SecondaryText = "color: navy",
                Divider = "color: gray",
                Dot = "color: pink",
                Icon = "color: gold"
            });
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", SecondaryText = "Two", IconName = "Add" }]);
        });

        Assert.IsTrue(component.Find(".bit-tln").GetAttribute("style")!.Contains("color: red"));
        Assert.IsTrue(component.Find(".bit-tln-itm").GetAttribute("style")!.Contains("color: green"));
        Assert.IsTrue(component.Find(".bit-tln-pcn").GetAttribute("style")!.Contains("color: blue"));
        Assert.IsTrue(component.Find(".bit-tln-pcn .bit-tln-ttx").GetAttribute("style")!.Contains("color: teal"));
        Assert.IsTrue(component.Find(".bit-tln-scn").GetAttribute("style")!.Contains("color: olive"));
        Assert.IsTrue(component.Find(".bit-tln-scn .bit-tln-ttx").GetAttribute("style")!.Contains("color: navy"));
        Assert.IsTrue(component.Find(".bit-tln-dvd").GetAttribute("style")!.Contains("color: gray"));
        Assert.IsTrue(component.Find(".bit-tln-dot").GetAttribute("style")!.Contains("color: pink"));
        Assert.IsTrue(component.Find(".bit-tln-ico").GetAttribute("style")!.Contains("color: gold"));
    }

    [TestMethod]
    public void BitTimelineShouldRenderDirWhenRtl()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        var root = component.Find(".bit-tln");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    #endregion

    #region custom items

    [TestMethod]
    public void BitTimelineShouldReadEveryValueOfACustomItemThroughTheNameSelectors()
    {
        var component = RenderComponent<BitTimeline<TimelineEvent>>(parameters =>
        {
            parameters.Add(p => p.NameSelectors, EventSelectors);
            parameters.Add(p => p.Items,
            [
                new TimelineEvent
                {
                    FirstText = "One",
                    SecondText = "Two",
                    IconClass = "Add",
                    Flipped = true,
                    DotColor = BitColor.Error,
                    DotSize = BitSize.Small,
                    DotVariant = BitVariant.Text,
                    LineStyle = BitLineStyle.Dashed,
                    Label = "The label",
                    Tooltip = "The tooltip"
                }
            ]);
        });

        var item = component.Find(".bit-tln-itm");

        Assert.AreEqual("One", component.Find(".bit-tln-pcn .bit-tln-ttx").TextContent);
        Assert.AreEqual("Two", component.Find(".bit-tln-scn .bit-tln-ttx").TextContent);
        Assert.IsTrue(component.Find(".bit-tln-ico").ClassList.Contains("bit-icon--Add"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-irv"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ier"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ism"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ivt"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ild"));
        Assert.AreEqual("The label", item.GetAttribute("aria-label"));
        Assert.AreEqual("The tooltip", item.GetAttribute("title"));
    }

    [TestMethod]
    public void BitTimelineShouldReadACustomItemThroughTheDefaultNamesWhenNoSelectorIsGiven()
    {
        var component = RenderComponent<BitTimeline<TimelineRecord>>(parameters =>
        {
            // An empty NameSelectors keeps every default field name, so the values are read by reflection
            // from the properties that carry the same names as the ones of BitTimelineItem.
            parameters.Add(p => p.NameSelectors, new BitTimelineNameSelectors<TimelineRecord>());
            parameters.Add(p => p.Items,
            [
                new TimelineRecord { PrimaryText = "One", LineStyle = BitLineStyle.Dotted },
                new TimelineRecord { PrimaryText = "Two", HideDot = true }
            ]);
        });

        var items = component.FindAll(".bit-tln-itm");

        Assert.AreEqual("One", component.Find(".bit-tln-pcn .bit-tln-ttx").TextContent);
        Assert.IsTrue(items[0].ClassList.Contains("bit-tln-ilt"));
        Assert.IsFalse(items[1].ClassList.Contains("bit-tln-ilt"));
        Assert.AreEqual(1, component.FindAll(".bit-tln-dot").Count);
    }

    [TestMethod]
    public void BitTimelineShouldHideTheDotOfACustomItemOnlyWhenRequested()
    {
        var component = RenderComponent<BitTimeline<TimelineEvent>>(parameters =>
        {
            parameters.Add(p => p.NameSelectors, EventSelectors);
            parameters.Add(p => p.Items,
            [
                new TimelineEvent { FirstText = "One" },
                new TimelineEvent { FirstText = "Two", NoDot = true }
            ]);
        });

        Assert.AreEqual(1, component.FindAll(".bit-tln-dot").Count);
    }

    [TestMethod]
    public void BitTimelineShouldUseTheDefaultsOfACustomItemWithoutNameSelectors()
    {
        var component = RenderComponent<BitTimeline<TimelineEvent>>(parameters =>
        {
            parameters.Add(p => p.Items, [new TimelineEvent { FirstText = "One" }]);
        });

        var item = component.Find(".bit-tln-itm");

        // Without name selectors, none of the optional values are readable, so the item must fall back
        // to a visible dot and to the direction of the timeline instead of hiding and flipping itself.
        Assert.AreEqual(1, component.FindAll(".bit-tln-dot").Count);
        Assert.IsFalse(item.ClassList.Contains("bit-tln-irv"));
        Assert.IsFalse(item.ClassList.Contains("bit-tln-ids"));
        Assert.IsFalse(item.ClassList.Contains("bit-tln-ils"));
        Assert.AreEqual("LI", item.TagName);
    }

    [TestMethod]
    public void BitTimelineShouldDisableACustomItemThroughTheNameSelectors()
    {
        var clicked = false;

        var component = RenderComponent<BitTimeline<TimelineEvent>>(parameters =>
        {
            parameters.Add(p => p.NameSelectors, EventSelectors);
            parameters.Add(p => p.OnItemClick, (TimelineEvent _) => clicked = true);
            parameters.Add(p => p.Items, [new TimelineEvent { FirstText = "One", Off = true }]);
        });

        var item = component.Find(".bit-tln-itm");

        component.Find(".bit-tln-btn").Click();

        Assert.IsFalse(clicked);
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ids"));
    }

    [TestMethod]
    public void BitTimelineShouldInvokeTheClickHandlerOfACustomItem()
    {
        var clickedText = string.Empty;

        var component = RenderComponent<BitTimeline<TimelineEvent>>(parameters =>
        {
            parameters.Add(p => p.NameSelectors, EventSelectors);
            parameters.Add(p => p.Items, [new TimelineEvent { FirstText = "One", Select = i => clickedText = i.FirstText! }]);
        });

        var item = component.Find(".bit-tln-itm");
        var button = component.Find(".bit-tln-btn");

        Assert.AreEqual("LI", item.TagName);
        Assert.AreEqual("button", button.GetAttribute("role"));

        button.Click();

        Assert.AreEqual("One", clickedText);
    }

    #endregion

    #region dot alignment and cascading parameters

    [TestMethod,
        DataRow(null, null),
        DataRow(BitPlacement.Center, null),
        DataRow(BitPlacement.Start, "bit-tln-das"),
        DataRow(BitPlacement.End, "bit-tln-dae"),
        // A value the dot does not honour renders the default centered dot, so it carries no class either.
        DataRow(BitPlacement.Left, null),
        DataRow(BitPlacement.TopAndBottom, null)]
    public void BitTimelineShouldRespectDotAlignment(BitPlacement? alignment, string? expectedClass)
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.DotAlignment, alignment);
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        var root = component.Find(".bit-tln");

        // The centered dot is the default the stylesheet paints on its own, so it carries no class.
        Assert.AreEqual(expectedClass == "bit-tln-das", root.ClassList.Contains("bit-tln-das"));
        Assert.AreEqual(expectedClass == "bit-tln-dae", root.ClassList.Contains("bit-tln-dae"));
    }

    [TestMethod]
    public void BitTimelineShouldHideTheDecorativeDotFromAssistiveTechnologies()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", IconName = "Add" }]);
        });

        // The dot and its icon only decorate the item; its contents are what gets announced.
        Assert.AreEqual("true", component.Find(".bit-tln-dot").GetAttribute("aria-hidden"));
        Assert.IsNotNull(component.Find(".bit-tln-dot .bit-tln-ico"));
    }

    [TestMethod]
    public void BitTimelineShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitTimelineCascadingParamsTest>();

        var timelines = component.FindAll(".bit-tln");

        Assert.AreEqual(3, timelines.Count);

        // The first timeline takes everything from the cascading parameters.
        var first = timelines[0];
        foreach (var cls in new[] { "bit-tln-suc", "bit-tln-otl", "bit-tln-lg", "bit-tln-hrz", "bit-tln-alt", "bit-tln-rvs",
                                    "bit-tln-rvo", "bit-tln-das", "bit-tln-ldd", "bit-tln-lps", "bit-tln-tlb", "cascaded-root" })
        {
            Assert.IsTrue(first.ClassList.Contains(cls), cls);
        }
        StringAssert.Contains(first.GetAttribute("style")!, "margin: 1px;");
        Assert.IsTrue(first.QuerySelector(".bit-tln-dot")!.ClassList.Contains("cascaded-dot"));

        // The second one sets its own color and orientation, which the cascading parameters must not overwrite.
        var second = timelines[1];
        Assert.IsTrue(second.ClassList.Contains("bit-tln-err"));
        Assert.IsFalse(second.ClassList.Contains("bit-tln-suc"));
        Assert.IsFalse(second.ClassList.Contains("bit-tln-hrz"));
        Assert.IsTrue(second.ClassList.Contains("bit-tln-otl"));

        // One cascade reaches a timeline of another item type as well.
        var third = timelines[2];
        Assert.IsTrue(third.ClassList.Contains("bit-tln-suc"));
        Assert.IsTrue(third.QuerySelector(".bit-tln-dot")!.ClassList.Contains("cascaded-dot"));
    }

    [TestMethod]
    public void BitTimelineShouldRespectACascadedDisabled()
    {
        var clicked = false;

        var component = RenderComponent<CascadingValue<BitTimelineParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitTimelineParams.ParamName);
            parameters.Add(p => p.Value, new BitTimelineParams { Disabled = true });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitTimeline<BitTimelineItem>>(0);
                builder.AddComponentParameter(1, nameof(BitTimeline<BitTimelineItem>.Items),
                    new[] { new BitTimelineItem { PrimaryText = "One", OnClick = _ => clicked = true } });
                builder.CloseComponent();
            }));
        });

        var button = component.Find(".bit-tln-btn");

        Assert.IsTrue(component.Find(".bit-tln").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        Assert.IsNull(button.GetAttribute("tabindex"));

        button.Click();

        Assert.IsFalse(clicked);
    }

    #endregion
}
