using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Card;

/// <summary>
/// Covers the BitParams cascade of the Card: that every parameter which is not about one card has a counterpart on
/// BitCardParams, that each of them reaches the card - its classes, its inline styles, its parts, its stretched link
/// and the root attributes it derives from them - that a value the card wrote for itself is kept, and that an empty
/// params object leaves the card exactly as it was. The look-and-feel properties the BitParams region of
/// BitCardTests already renders (Color, Variant, Size, Elevation, the cover and the loading placeholder) are not
/// repeated here.
/// </summary>
[TestClass]
public class BitCardParamsTests : BunitTestContext
{
    // What a card is about rather than how every card looks - its content, its parts, its destination, its state
    // and what it answers - is written per card, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitCard.CascadingParameters),
        nameof(BitCard.Actions),
        nameof(BitCard.ChildContent),
        nameof(BitCard.Cover),
        nameof(BitCard.FloatingActions),
        nameof(BitCard.Footer),
        nameof(BitCard.HeaderTemplate),
        nameof(BitCard.Href),
        nameof(BitCard.Icon),
        nameof(BitCard.IconName),
        nameof(BitCard.IconTemplate),
        nameof(BitCard.ImageAlt),
        nameof(BitCard.ImageUrl),
        nameof(BitCard.OnClick),
        nameof(BitCard.Selected),
        nameof(BitCard.Subtitle),
        nameof(BitCard.Title),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitCardParams cardParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { cardParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitCard.ChildContent), (RenderFragment)(b => b.AddContent(0, "Body")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCardParamsShouldCarryEveryParameterThatIsNotPerCard()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitCard).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                        .Select(p => p.Name)
                                        .Where(n => baseParameters.Contains(n) is false
                                                 && _notCascaded.Contains(n) is false
                                                 && n.EndsWith("Changed", StringComparison.Ordinal) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitCardParams).GetProperty(name), $"BitCardParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitCardParamsShouldNotCascadeWhatBelongsToOneCard()
    {
        foreach (var name in _notCascaded.Where(n => n != nameof(BitCard.CascadingParameters)))
        {
            Assert.IsNull(typeof(BitCardParams).GetProperty(name), $"BitCardParams cascades {name}, which belongs to one card.");
        }
    }

    [TestMethod]
    public void BitCardShouldApplyCascadedClassesToEveryPart()
    {
        var component = RenderWithParams(new BitCardParams
        {
            Classes = new()
            {
                Root = "cascaded-root",
                Main = "cascaded-main",
                Header = "cascaded-header",
                HeaderText = "cascaded-header-text",
                Title = "cascaded-title",
                Subtitle = "cascaded-subtitle",
                Body = "cascaded-body",
                Footer = "cascaded-footer",
            },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Title), "Title");
            builder.AddAttribute(2, nameof(BitCard.Subtitle), "Subtitle");
            builder.AddAttribute(3, nameof(BitCard.Footer), (RenderFragment)(b => b.AddContent(0, "Footer")));
        });

        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-crd-mai").ClassList.Contains("cascaded-main"));
        Assert.IsTrue(component.Find(".bit-crd-hdr").ClassList.Contains("cascaded-header"));
        Assert.IsTrue(component.Find(".bit-crd-htx").ClassList.Contains("cascaded-header-text"));
        Assert.IsTrue(component.Find(".bit-crd-ttl").ClassList.Contains("cascaded-title"));
        Assert.IsTrue(component.Find(".bit-crd-sub").ClassList.Contains("cascaded-subtitle"));
        Assert.IsTrue(component.Find(".bit-crd-bdy").ClassList.Contains("cascaded-body"));
        Assert.IsTrue(component.Find(".bit-crd-ftr").ClassList.Contains("cascaded-footer"));
    }

    [TestMethod]
    public void BitCardShouldApplyCascadedStylesToEveryPart()
    {
        var component = RenderWithParams(new BitCardParams
        {
            Styles = new()
            {
                Root = "color: red;",
                Main = "color: blue;",
                Header = "color: green;",
                Title = "color: orange;",
                Body = "color: purple;",
                Footer = "color: brown;",
            },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Title), "Title");
            builder.AddAttribute(2, nameof(BitCard.Footer), (RenderFragment)(b => b.AddContent(0, "Footer")));
        });

        StringAssert.Contains(component.Find(".bit-crd").GetAttribute("style"), "color: red;");
        Assert.AreEqual("color: blue;", component.Find(".bit-crd-mai").GetAttribute("style"));
        Assert.AreEqual("color: green;", component.Find(".bit-crd-hdr").GetAttribute("style"));
        Assert.AreEqual("color: orange;", component.Find(".bit-crd-ttl").GetAttribute("style"));
        Assert.AreEqual("color: purple;", component.Find(".bit-crd-bdy").GetAttribute("style"));
        Assert.AreEqual("color: brown;", component.Find(".bit-crd-ftr").GetAttribute("style"));
    }

    [TestMethod]
    public void BitCardShouldApplyCascadedLayoutFlags()
    {
        var component = RenderWithParams(new BitCardParams
        {
            FullSize = true,
            NoShadow = true,
            Outlined = true,
        });

        var root = component.Find(".bit-crd");

        Assert.IsTrue(root.ClassList.Contains("bit-crd-fhe"));
        Assert.IsTrue(root.ClassList.Contains("bit-crd-fwi"));
        Assert.IsTrue(root.ClassList.Contains("bit-crd-nsd"));
        Assert.IsTrue(root.ClassList.Contains("bit-crd-otl"));
    }

    [TestMethod,
        DataRow(true, false, true, false),
        DataRow(false, true, false, true)
    ]
    public void BitCardShouldApplyCascadedFullHeightAndFullWidthApart(bool fullHeight, bool fullWidth, bool expectHeight, bool expectWidth)
    {
        var component = RenderWithParams(new BitCardParams { FullHeight = fullHeight, FullWidth = fullWidth });

        var root = component.Find(".bit-crd");

        Assert.AreEqual(expectHeight, root.ClassList.Contains("bit-crd-fhe"));
        Assert.AreEqual(expectWidth, root.ClassList.Contains("bit-crd-fwi"));
    }

    [TestMethod]
    public void BitCardShouldApplyCascadedSizes()
    {
        var component = RenderWithParams(new BitCardParams
        {
            Height = "10rem",
            MinHeight = "5rem",
            MinWidth = "6rem",
            MaxHeight = "20rem",
        });

        var style = component.Find(".bit-crd").GetAttribute("style")!;

        StringAssert.Contains(style, "height:10rem");
        StringAssert.Contains(style, "min-height:5rem");
        StringAssert.Contains(style, "min-width:6rem");
        StringAssert.Contains(style, "max-height:20rem");
    }

    [TestMethod]
    public void BitCardShouldApplyCascadedCoverWidthAndImageHeight()
    {
        var component = RenderWithParams(new BitCardParams
        {
            Horizontal = true,
            CoverWidth = "30%",
            ImageHeight = "8rem",
        }, builder => builder.AddAttribute(1, nameof(BitCard.ImageUrl), "/images/a.png"));

        var style = component.Find(".bit-crd").GetAttribute("style")!;

        StringAssert.Contains(style, "--bit-crd-cvr-width:30%");
        StringAssert.Contains(style, "--bit-crd-img-height:8rem");
    }

    [TestMethod]
    public void BitCardShouldApplyCascadedHeadingLevelToTheTitle()
    {
        var component = RenderWithParams(new BitCardParams { HeadingLevel = 2 }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Title), "Title");
        });

        var title = component.Find(".bit-crd-ttl");

        Assert.AreEqual("heading", title.GetAttribute("role"));
        Assert.AreEqual("2", title.GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitCardShouldApplyCascadedRelToTheStretchedLink()
    {
        // Rel runs the hook that resolves the rel attribute, whether the markup or the cascade wrote it.
        var component = RenderWithParams(new BitCardParams { Rel = BitLinkRels.NoFollow }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Href), "https://bitplatform.dev");
        });

        StringAssert.Contains(component.Find(".bit-crd-lnk").GetAttribute("rel"), "nofollow");
    }

    [TestMethod]
    public void BitCardShouldApplyCascadedStopPropagation()
    {
        var component = RenderWithParams(new BitCardParams { StopPropagation = true });

        Assert.IsTrue(component.Find(".bit-crd").HasAttribute("blazor:onclick:stoppropagation"));
    }

    [TestMethod]
    public void BitCardShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitCardParams
        {
            Dir = BitDir.Rtl,
            Disabled = true,
            Class = "cascaded",
            Style = "margin: 1px;",
            AriaLabel = "Cascaded card",
        });

        var root = component.Find(".bit-crd");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");

        // A named card is a group, and a disabled group reports itself as such - whoever named it.
        Assert.AreEqual("Cascaded card", root.GetAttribute("aria-label"));
        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitCardShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitCardParams
        {
            NoShadow = true,
            Square = true,
            Height = "10rem",
            HeadingLevel = 2,
            Classes = new() { Root = "cascaded-root" },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.NoShadow), false);
            builder.AddAttribute(2, nameof(BitCard.Height), "4rem");
            builder.AddAttribute(3, nameof(BitCard.HeadingLevel), 4);
            builder.AddAttribute(4, nameof(BitCard.Title), "Title");
            builder.AddAttribute(5, nameof(BitCard.Classes), new BitCardClassStyles { Root = "own-root" });
        });

        var root = component.Find(".bit-crd");

        // NoShadow written as false by the card opts it out of the cascaded one; what it did not write is filled in.
        Assert.IsFalse(root.ClassList.Contains("bit-crd-nsd"));
        Assert.IsTrue(root.ClassList.Contains("bit-crd-sqr"));
        StringAssert.Contains(root.GetAttribute("style"), "height:4rem");
        Assert.IsFalse(root.GetAttribute("style")!.Contains("height:10rem"));
        Assert.AreEqual("4", component.Find(".bit-crd-ttl").GetAttribute("aria-level"));
        Assert.IsTrue(root.ClassList.Contains("own-root"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-root"));
    }

    [TestMethod]
    public void BitCardShouldGoBackToItsDefaultsOnceTheCascadeStopsCarryingAValue()
    {
        var component = RenderWithParams(new BitCardParams { Square = true, Height = "10rem" });

        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("bit-crd-sqr"));
        StringAssert.Contains(component.Find(".bit-crd").GetAttribute("style"), "height:10rem");

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitCardParams() }));

        var card = component.FindComponent<BitCard>().Instance;

        Assert.IsFalse(card.Square);
        Assert.IsNull(card.Height);
        Assert.IsFalse(component.Find(".bit-crd").ClassList.Contains("bit-crd-sqr"));
        Assert.IsFalse((component.Find(".bit-crd").GetAttribute("style") ?? string.Empty).Contains("height:10rem"));
    }

    [TestMethod]
    public void BitCardShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitCardParams());

        var card = component.FindComponent<BitCard>().Instance;
        var root = component.Find(".bit-crd");

        Assert.IsNull(card.Background);
        Assert.IsNull(card.Border);
        Assert.IsNull(card.Color);
        Assert.IsNull(card.Size);
        Assert.IsNull(card.Elevation);
        Assert.IsNull(card.Variant);
        Assert.IsNull(card.Height);
        Assert.IsNull(card.Width);
        Assert.IsFalse(card.Loading);
        Assert.IsFalse(card.Horizontal);
        Assert.IsFalse(card.Square);
        Assert.IsFalse(card.StopPropagation);

        // A plain card carries nothing but its own class, and nothing but its body.
        Assert.AreEqual(1, root.ClassList.Length);
        Assert.IsTrue(root.ClassList.Contains("bit-crd"));
        Assert.AreEqual("Body", root.TextContent);
        Assert.IsNull(root.GetAttribute("role"));
        Assert.IsNull(root.GetAttribute("tabindex"));
    }
}
