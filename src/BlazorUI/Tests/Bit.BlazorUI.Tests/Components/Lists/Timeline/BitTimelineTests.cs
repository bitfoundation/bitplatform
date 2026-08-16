using System;
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
        public string? Label { get; set; }
        public string? Tooltip { get; set; }
        public Action<TimelineEvent>? Select { get; set; }
    }

    private static BitTimelineNameSelectors<TimelineEvent> EventSelectors => new()
    {
        PrimaryText = { Selector = i => i.FirstText },
        SecondaryText = { Selector = i => i.SecondText },
        IconName = { Selector = i => i.IconClass },
        IsEnabled = { Selector = i => i.Off is false },
        Reversed = { Selector = i => i.Flipped },
        HideDot = { Selector = i => i.NoDot },
        Color = { Selector = i => i.DotColor },
        Size = { Selector = i => i.DotSize },
        Variant = { Selector = i => i.DotVariant },
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
    public void BitTimelineShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            TwoOptions()(parameters);
        });

        var root = component.Find(".bit-tln");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
            Assert.IsNull(root.GetAttribute("aria-disabled"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
            Assert.AreEqual("true", root.GetAttribute("aria-disabled"));
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

        Assert.IsTrue(root.ClassList.Contains("bit-tln-pri"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-md"));
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
        // Reversing the rendered order swaps which end of the line Start and End refer to.
        DataRow(BitTimelineTruncateLine.Start, true, "bit-tln-tle"),
        DataRow(BitTimelineTruncateLine.End, true, "bit-tln-tls"),
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
            new() { Key = "a", PrimaryText = "One" },
            new() { Key = "b", PrimaryText = "Two" }
        };

        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        // Reordering with stable keys keeps the same elements, only in the new order.
        component.Render(parameters => parameters.Add(p => p.Items, items.AsEnumerable().Reverse().ToList()));

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

        Assert.AreEqual("listitem", item.GetAttribute("role"));
        Assert.IsNull(item.GetAttribute("tabindex"));
        Assert.IsFalse(item.ClassList.Contains("bit-tln-int"));
    }

    [TestMethod]
    public void BitTimelineShouldRenderClickableItemsAsButtons()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineItem _) => { });
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        var item = component.Find(".bit-tln-itm");

        Assert.AreEqual("button", item.GetAttribute("role"));
        Assert.AreEqual("0", item.GetAttribute("tabindex"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-int"));
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

        Assert.AreEqual("button", items[0].GetAttribute("role"));
        Assert.AreEqual("listitem", items[1].GetAttribute("role"));
    }

    [TestMethod]
    public void BitTimelineShouldNotMakeADisabledItemFocusable()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineItem _) => { });
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One", IsEnabled = false }]);
        });

        var item = component.Find(".bit-tln-itm");

        Assert.AreEqual("button", item.GetAttribute("role"));
        Assert.IsNull(item.GetAttribute("tabindex"));
        Assert.AreEqual("true", item.GetAttribute("aria-disabled"));
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ids"));
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

        component.Find(".bit-tln-itm").Click();

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

        component.Find(".bit-tln-itm").Click();

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
                builder.AddAttribute(1, nameof(BitTimelineOption.IsEnabled), false);
                builder.AddAttribute(2, nameof(BitTimelineOption.PrimaryText), "First");
                builder.CloseComponent();
            });
        });

        var item = component.Find(".bit-tln-itm");

        item.Click();

        Assert.IsNull(clicked);
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ids"));
    }

    [TestMethod]
    public void BitTimelineDisabledComponentShouldNotInvokeCallback()
    {
        BitTimelineOption? clicked = null;

        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnItemClick, (BitTimelineOption item) => clicked = item);
            TwoOptions()(parameters);
        });

        component.Find(".bit-tln-itm").Click();

        Assert.IsNull(clicked);
    }

    [TestMethod,
        DataRow("Enter"),
        DataRow(" "),
        DataRow("Spacebar")]
    public void BitTimelineShouldInvokeCallbackOnActivationKeys(string key)
    {
        BitTimelineOption? clicked = null;

        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineOption item) => clicked = item);
            TwoOptions()(parameters);
        });

        component.Find(".bit-tln-itm").KeyDown(key);

        Assert.IsNotNull(clicked);
        Assert.AreEqual("First", clicked!.PrimaryText);
    }

    [TestMethod]
    public void BitTimelineShouldIgnoreOtherKeys()
    {
        BitTimelineOption? clicked = null;

        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineOption item) => clicked = item);
            TwoOptions()(parameters);
        });

        component.Find(".bit-tln-itm").KeyDown("A");

        Assert.IsNull(clicked);
    }

    [TestMethod]
    public void BitTimelineShouldIgnoreKeysOnANonInteractiveItem()
    {
        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        // No handler is attached anywhere, so this must simply be a no-op instead of throwing.
        component.Find(".bit-tln-itm").KeyDown("Enter");

        Assert.AreEqual("listitem", component.Find(".bit-tln-itm").GetAttribute("role"));
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
        Assert.AreEqual("The label", item.GetAttribute("aria-label"));
        Assert.AreEqual("The tooltip", item.GetAttribute("title"));
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
        Assert.AreEqual("listitem", item.GetAttribute("role"));
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

        item.Click();

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

        Assert.AreEqual("button", item.GetAttribute("role"));

        item.Click();

        Assert.AreEqual("One", clickedText);
    }

    #endregion
}
