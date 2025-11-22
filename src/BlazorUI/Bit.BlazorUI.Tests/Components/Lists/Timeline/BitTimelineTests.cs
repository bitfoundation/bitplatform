using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Timeline;

[TestClass]
public class BitTimelineTests : BunitTestContext
{
    [TestMethod]
    public void BitTimelineShouldRenderItemsFromChildContent()
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), "First");
                builder.AddAttribute(2, nameof(BitTimelineOption.SecondaryText), "First second");
                builder.CloseComponent();

                builder.OpenComponent<BitTimelineOption>(3);
                builder.AddAttribute(4, nameof(BitTimelineOption.PrimaryText), "Second");
                builder.AddAttribute(5, nameof(BitTimelineOption.SecondaryText), "Second second");
                builder.CloseComponent();
            });
        });

        var items = component.FindAll(".bit-tln-itm");

        Assert.AreEqual(2, items.Count);

        Assert.IsTrue(component.Markup.Contains("First"));
        Assert.IsTrue(component.Markup.Contains("Second"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTimelineShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), "First");
                builder.AddAttribute(2, nameof(BitTimelineOption.SecondaryText), "First second");
                builder.CloseComponent();

                builder.OpenComponent<BitTimelineOption>(3);
                builder.AddAttribute(4, nameof(BitTimelineOption.PrimaryText), "Second");
                builder.AddAttribute(5, nameof(BitTimelineOption.SecondaryText), "Second second");
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-tln");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitTimelineItemClickShouldInvokeCallbackWhenEnabled()
    {
        BitTimelineOption? clicked = null;

        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitTimelineOption item) => clicked = item);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), "First");
                builder.AddAttribute(2, nameof(BitTimelineOption.SecondaryText), "First second");
                builder.CloseComponent();

                builder.OpenComponent<BitTimelineOption>(3);
                builder.AddAttribute(4, nameof(BitTimelineOption.PrimaryText), "Second");
                builder.AddAttribute(5, nameof(BitTimelineOption.SecondaryText), "Second second");
                builder.CloseComponent();
            });
        });

        var item = component.Find(".bit-tln-itm");

        item.Click();

        Assert.IsNotNull(clicked);
        Assert.AreEqual("First", clicked!.PrimaryText);
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
                builder.AddAttribute(3, nameof(BitTimelineOption.SecondaryText), "First second");
                builder.CloseComponent();

                builder.OpenComponent<BitTimelineOption>(4);
                builder.AddAttribute(5, nameof(BitTimelineOption.PrimaryText), "Second");
                builder.AddAttribute(6, nameof(BitTimelineOption.SecondaryText), "Second second");
                builder.CloseComponent();
            });
        });

        var item = component.Find(".bit-tln-itm");

        item.Click();

        Assert.IsNull(clicked);
        Assert.IsTrue(item.ClassList.Contains("bit-tln-ids"));
    }

    [TestMethod]
    public void BitTimelineShouldRenderDotOrHideWhenRequested()
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.AddChildContent(@"
<BitTimelineOption PrimaryText=""ShowDot"" />
<BitTimelineOption PrimaryText=""HideDot"" HideDot=""true"" />");
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), "ShowDot");
                builder.AddAttribute(2, nameof(BitTimelineOption.SecondaryText), "ShowDot second");
                builder.CloseComponent();

                builder.OpenComponent<BitTimelineOption>(3);
                builder.AddAttribute(4, nameof(BitTimelineOption.HideDot), true);
                builder.AddAttribute(5, nameof(BitTimelineOption.PrimaryText), "HideDot");
                builder.AddAttribute(6, nameof(BitTimelineOption.SecondaryText), "HideDot second");
                builder.CloseComponent();
            });
        });

        var dots = component.FindAll(".bit-tln-dot");

        Assert.AreEqual(1, dots.Count);
    }

    [TestMethod]
    public void BitTimelineShouldRenderIconWhenProvided()
    {
        var component = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.IconName), "Add");
                builder.AddAttribute(2, nameof(BitTimelineOption.PrimaryText), "First");
                builder.AddAttribute(3, nameof(BitTimelineOption.SecondaryText), "First second");
                builder.CloseComponent();

                builder.OpenComponent<BitTimelineOption>(4);
                builder.AddAttribute(5, nameof(BitTimelineOption.PrimaryText), "Second");
                builder.AddAttribute(6, nameof(BitTimelineOption.SecondaryText), "Second second");
                builder.CloseComponent();
            });
        });

        var icon = component.Find(".bit-tln-ico");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Add"));
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
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), "First");
                builder.AddAttribute(2, nameof(BitTimelineOption.SecondaryText), "First second");
                builder.CloseComponent();

                builder.OpenComponent<BitTimelineOption>(3);
                builder.AddAttribute(4, nameof(BitTimelineOption.PrimaryText), "Second");
                builder.AddAttribute(5, nameof(BitTimelineOption.SecondaryText), "Second second");
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-tln");

        Assert.IsTrue(root.ClassList.Contains("bit-tln-sec"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-hrz"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-rvs"));
    }
}
