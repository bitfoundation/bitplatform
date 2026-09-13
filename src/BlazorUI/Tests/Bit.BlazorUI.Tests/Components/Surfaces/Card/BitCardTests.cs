using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Card;

[TestClass]
public class BitCardTests : BunitTestContext
{
    [TestMethod]
    public void BitCardShouldRenderChildContent()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Hello Card</div>");
        });

        var card = component.Find(".bit-crd");
        Assert.IsNotNull(card);
        Assert.IsTrue(card.OuterHtml.Contains("Hello Card"));
    }

    [TestMethod]
    public void BitCardShouldRenderTheDefaultClasses()
    {
        var component = RenderComponent<BitCard>();

        var card = component.Find(".bit-crd");

        // A plain card is the secondary surface at the medium size, and nothing else.
        Assert.IsTrue(card.ClassList.Contains("bit-crd-bsg"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-md"));
        Assert.IsFalse(card.ClassList.Contains("bit-crd-sct"));
        Assert.IsFalse(card.ClassList.Contains("bit-crd-int"));
    }

    [TestMethod]
    public void BitCardWithoutPartsShouldNotRenderTheSectionedDom()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.AddChildContent("<span>plain</span>");
        });

        // The plain card keeps the padded box it has always been: the content is a direct child of the root.
        Assert.AreEqual(0, component.FindAll(".bit-crd-mai").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-bdy").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-hdr").Count);
    }

    #region Background & Border

    [TestMethod]
    [DataRow(BitColorKind.Primary, "bit-crd-bpg")]
    [DataRow(BitColorKind.Secondary, "bit-crd-bsg")]
    [DataRow(BitColorKind.Tertiary, "bit-crd-btg")]
    [DataRow(BitColorKind.Transparent, "bit-crd-brg")]
    [DataRow(null, "bit-crd-bsg")]
    public void BitCardBackgroundClassTest(BitColorKind? background, string expected)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            if (background.HasValue)
            {
                parameters.Add(p => p.Background, background.Value);
            }
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains(expected));
    }

    [TestMethod]
    [DataRow(BitColorKind.Primary, "bit-crd-bpr")]
    [DataRow(BitColorKind.Secondary, "bit-crd-bsr")]
    [DataRow(BitColorKind.Tertiary, "bit-crd-btr")]
    [DataRow(BitColorKind.Transparent, "bit-crd-brr")]
    public void BitCardBorderClassTest(BitColorKind border, string expected)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Border, border);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-brd"));
        Assert.IsTrue(card.ClassList.Contains(expected));
    }

    [TestMethod]
    public void BitCardWithoutBorderShouldNotRenderTheBorderClass()
    {
        var component = RenderComponent<BitCard>();

        var card = component.Find(".bit-crd");

        Assert.IsFalse(card.ClassList.Contains("bit-crd-brd"));
    }

    #endregion

    #region Color & Variant

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-crd-pri")]
    [DataRow(BitColor.Secondary, "bit-crd-sec")]
    [DataRow(BitColor.Tertiary, "bit-crd-ter")]
    [DataRow(BitColor.Info, "bit-crd-inf")]
    [DataRow(BitColor.Success, "bit-crd-suc")]
    [DataRow(BitColor.Warning, "bit-crd-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-crd-swr")]
    [DataRow(BitColor.Error, "bit-crd-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-crd-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-crd-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-crd-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-crd-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-crd-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-crd-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-crd-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-crd-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-crd-tbr")]
    public void BitCardColorClassTest(BitColor color, string expected)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains(expected));
    }

    [TestMethod]
    public void BitCardWithoutColorShouldNotRenderAColorClass()
    {
        var component = RenderComponent<BitCard>();

        var card = component.Find(".bit-crd");

        Assert.IsFalse(card.ClassList.Contains("bit-crd-pri"));
    }

    [TestMethod]
    [DataRow(BitVariant.Fill, "bit-crd-vfl")]
    [DataRow(BitVariant.Outline, "bit-crd-vot")]
    [DataRow(BitVariant.Text, "bit-crd-vtx")]
    [DataRow(null, "bit-crd-vfl")]
    public void BitCardVariantClassTest(BitVariant? variant, string expected)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Primary);

            if (variant.HasValue)
            {
                parameters.Add(p => p.Variant, variant.Value);
            }
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains(expected));
    }

    [TestMethod]
    public void BitCardVariantWithoutColorShouldNotRenderAVariantClass()
    {
        // A variant paints in a role color, so it means nothing until there is a role to paint in.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Variant, BitVariant.Outline);
        });

        var card = component.Find(".bit-crd");

        Assert.IsFalse(card.ClassList.Contains("bit-crd-vot"));
        Assert.IsFalse(card.ClassList.Contains("bit-crd-vfl"));
    }

    #endregion

    #region Size

    [TestMethod]
    [DataRow(BitSize.Small, "bit-crd-sm")]
    [DataRow(BitSize.Medium, "bit-crd-md")]
    [DataRow(BitSize.Large, "bit-crd-lg")]
    [DataRow(null, "bit-crd-md")]
    public void BitCardSizeClassTest(BitSize? size, string expected)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            if (size.HasValue)
            {
                parameters.Add(p => p.Size, size.Value);
            }
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains(expected));
    }

    #endregion

    #region Sizing

    [TestMethod]
    public void BitCardFullSizeShouldApplyCorrectCssClasses()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.FullSize, true);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-fhe"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-fwi"));
    }

    [TestMethod]
    public void BitCardFullHeightShouldApplyCorrectCssClass()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.FullHeight, true);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-fhe"));
        Assert.IsFalse(card.ClassList.Contains("bit-crd-fwi"));
    }

    [TestMethod]
    public void BitCardFullWidthShouldApplyCorrectCssClass()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.FullWidth, true);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-fwi"));
        Assert.IsFalse(card.ClassList.Contains("bit-crd-fhe"));
    }

    [TestMethod]
    [DataRow("300px")]
    [DataRow("50%")]
    [DataRow("20rem")]
    public void BitCardWidthShouldApplyCorrectStyle(string width)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Width, width);
        });

        var card = component.Find(".bit-crd");
        Assert.IsTrue(card.GetAttribute("style")!.Contains($"width:{width}"));
    }

    [TestMethod]
    [DataRow("200px")]
    [DataRow("50%")]
    [DataRow("10rem")]
    public void BitCardHeightShouldApplyCorrectStyle(string height)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Height, height);
        });

        var card = component.Find(".bit-crd");
        Assert.IsTrue(card.GetAttribute("style")!.Contains($"height:{height}"));
    }

    [TestMethod]
    public void BitCardWidthAndHeightShouldApplyBothStyles()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Width, "300px");
            parameters.Add(p => p.Height, "200px");
        });

        var card = component.Find(".bit-crd");
        var style = card.GetAttribute("style")!;
        Assert.IsTrue(style.Contains("width:300px"));
        Assert.IsTrue(style.Contains("height:200px"));
    }

    [TestMethod]
    public void BitCardWidthAndHeightShouldNotRenderWhenNull()
    {
        var component = RenderComponent<BitCard>();

        var card = component.Find(".bit-crd");
        var style = card.GetAttribute("style");
        Assert.IsTrue(style is null || (style.Contains("width") is false && style.Contains("height") is false));
    }

    [TestMethod]
    public void BitCardMinAndMaxSizesShouldApplyTheirStyles()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.MinWidth, "10rem");
            parameters.Add(p => p.MaxWidth, "20rem");
            parameters.Add(p => p.MinHeight, "5rem");
            parameters.Add(p => p.MaxHeight, "15rem");
        });

        var style = component.Find(".bit-crd").GetAttribute("style")!;

        Assert.IsTrue(style.Contains("min-width:10rem"));
        Assert.IsTrue(style.Contains("max-width:20rem"));
        Assert.IsTrue(style.Contains("min-height:5rem"));
        Assert.IsTrue(style.Contains("max-height:15rem"));
    }

    #endregion

    #region Elevation & shadow

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(12)]
    [DataRow(24)]
    public void BitCardElevationShouldApplyCorrectClass(int elevation)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Elevation, elevation);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains($"bit-crd-e{elevation}"));
    }

    [TestMethod]
    [DataRow(25)]
    [DataRow(-1)]
    [DataRow(100)]
    public void BitCardElevationOutOfRangeShouldBeIgnored(int elevation)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Elevation, elevation);
        });

        var card = component.Find(".bit-crd");

        for (var i = 0; i <= 24; i++)
        {
            Assert.IsFalse(card.ClassList.Contains($"bit-crd-e{i}"));
        }
    }

    [TestMethod]
    public void BitCardWithoutElevationShouldNotRenderAnElevationClass()
    {
        var component = RenderComponent<BitCard>();

        var card = component.Find(".bit-crd");

        for (var i = 0; i <= 24; i++)
        {
            Assert.IsFalse(card.ClassList.Contains($"bit-crd-e{i}"));
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardNoShadowTest(bool noShadow)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.NoShadow, noShadow);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual(noShadow, card.ClassList.Contains("bit-crd-nsd"));
    }

    #endregion

    #region Shape & padding

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardNoPaddingTest(bool noPadding)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.NoPadding, noPadding);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual(noPadding, card.ClassList.Contains("bit-crd-npd"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardOutlinedTest(bool outlined)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Outlined, outlined);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual(outlined, card.ClassList.Contains("bit-crd-otl"));
    }

    [TestMethod]
    public void BitCardOutlinedShouldNotTakeTheBorderClassOff()
    {
        // Outlined is a shorthand: an explicit Border still decides the color of the rule.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Outlined, true);
            parameters.Add(p => p.Border, BitColorKind.Tertiary);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-otl"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-brd"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-btr"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardSquareTest(bool square)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Square, square);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual(square, card.ClassList.Contains("bit-crd-sqr"));
    }

    #endregion

    #region Header

    [TestMethod]
    public void BitCardTitleShouldRenderTheHeaderAndSectionTheCard()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Card title");
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual("Card title", component.Find(".bit-crd-ttl").TextContent);
        Assert.AreEqual(1, component.FindAll(".bit-crd-mai").Count);
    }

    [TestMethod]
    public void BitCardSubtitleShouldRender()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
        });

        Assert.AreEqual("Subtitle", component.Find(".bit-crd-sub").TextContent);
    }

    [TestMethod]
    public void BitCardSubtitleAloneShouldStillRenderTheHeader()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Subtitle, "Subtitle");
        });

        Assert.AreEqual(1, component.FindAll(".bit-crd-hdr").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-ttl").Count);
        Assert.AreEqual("Subtitle", component.Find(".bit-crd-sub").TextContent);
    }

    [TestMethod]
    public void BitCardIconNameShouldRenderTheHeaderIcon()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.IconName, "Album");
        });

        var icon = component.Find(".bit-crd-hic");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Album"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitCardIconTemplateShouldRenderInTheLeadingSlotAndKeepTheHeaderText()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
            parameters.Add(p => p.IconTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-avatar");
                builder.AddContent(2, "AL");
                builder.CloseElement();
            }));
        });

        var slot = component.Find(".bit-crd-hic");

        Assert.AreEqual("DIV", slot.TagName);
        Assert.AreEqual(1, component.FindAll(".bit-crd-hic .custom-avatar").Count);
        Assert.IsNull(slot.GetAttribute("aria-hidden"));
        Assert.AreEqual("Title", component.Find(".bit-crd-ttl").TextContent);
        Assert.AreEqual("Subtitle", component.Find(".bit-crd-sub").TextContent);
    }

    [TestMethod]
    public void BitCardIconTemplateShouldWinOverTheIconName()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.IconName, "Album");
            parameters.Add(p => p.IconTemplate, (RenderFragment)(builder => builder.AddContent(0, "Avatar")));
        });

        var slot = component.Find(".bit-crd-hic");

        Assert.AreEqual(1, component.FindAll(".bit-crd-hic").Count);
        Assert.IsFalse(slot.ClassList.Contains("bit-icon--Album"));
        Assert.AreEqual("Avatar", slot.TextContent);
    }

    [TestMethod]
    public void BitCardIconTemplateAloneShouldSectionTheCard()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.IconTemplate, (RenderFragment)(builder => builder.AddContent(0, "Avatar")));
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual(1, component.FindAll(".bit-crd-mai > .bit-crd-hdr > .bit-crd-hic").Count);
    }

    [TestMethod]
    public void BitCardHeaderTemplateShouldWinOverTheIconTemplate()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.IconTemplate, (RenderFragment)(builder => builder.AddContent(0, "Avatar")));
            parameters.Add(p => p.HeaderTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-header");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".custom-header").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-hic").Count);
    }

    [TestMethod]
    public void BitCardHeaderTemplateShouldReplaceTheTitleAndSubtitle()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
            parameters.Add(p => p.HeaderTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-header");
                builder.AddContent(2, "Custom");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".custom-header").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-ttl").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-sub").Count);
    }

    [TestMethod]
    public void BitCardHeaderTemplateShouldKeepTheActions()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.HeaderTemplate, (RenderFragment)(builder => builder.AddContent(0, "Custom")));
            parameters.Add(p => p.Actions, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "act-btn");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-crd-act .act-btn").Count);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(6)]
    public void BitCardHeadingLevelShouldMakeTheTitleAHeading(int level)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.HeadingLevel, level);
        });

        var title = component.Find(".bit-crd-ttl");

        Assert.AreEqual("heading", title.GetAttribute("role"));
        Assert.AreEqual(level.ToString(), title.GetAttribute("aria-level"));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(-1)]
    public void BitCardHeadingLevelOutOfRangeShouldBeIgnored(int level)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.HeadingLevel, level);
        });

        var title = component.Find(".bit-crd-ttl");

        Assert.IsNull(title.GetAttribute("role"));
        Assert.IsNull(title.GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitCardHeadingLevelShouldBeDroppedOnACardThatIsAButton()
    {
        // The children of a button are presentational: what is in one is read as the name of the control rather
        // than as structure, so a heading role in there is never reached and is a thing for a validator to flag.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.HeadingLevel, 3);
            parameters.Add(p => p.OnClick, () => { });
        });

        var title = component.Find(".bit-crd-ttl");

        Assert.AreEqual("button", component.Find(".bit-crd").GetAttribute("role"));
        Assert.IsNull(title.GetAttribute("role"));
        Assert.IsNull(title.GetAttribute("aria-level"));
    }

    [TestMethod]
    [DataRow("option")]
    [DataRow("tab")]
    [DataRow("switch")]
    public void BitCardHeadingLevelShouldBeDroppedOnAnyRoleThatPresentsItsChildren(string role)
    {
        // A button is not the only role whose contents are read as its name rather than as structure.
        var component = RenderSplattedCard(builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Title), "Title");
            builder.AddAttribute(2, nameof(BitCard.HeadingLevel), 3);
            builder.AddAttribute(3, "role", role);
        });

        var title = component.Find(".bit-crd-ttl");

        Assert.IsNull(title.GetAttribute("role"));
        Assert.IsNull(title.GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitCardHeadingLevelShouldSurviveARoleThatIsNotAButton()
    {
        // The card only gives the heading up where it actually ended up reporting itself as a button; a card that
        // splatted a role of its own in is whatever that role says it is.
        var component = RenderSplattedCard(builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Title), "Title");
            builder.AddAttribute(2, nameof(BitCard.HeadingLevel), 3);
            builder.AddAttribute(3, nameof(BitCard.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
            builder.AddAttribute(4, "role", "article");
        });

        var title = component.Find(".bit-crd-ttl");

        Assert.AreEqual("article", component.Find(".bit-crd").GetAttribute("role"));
        Assert.AreEqual("heading", title.GetAttribute("role"));
        Assert.AreEqual("3", title.GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitCardHeadingLevelShouldSurviveALinkedCard()
    {
        // A linked card is not a button - the anchor stretched over it is the control - so the title of one is
        // still the heading of the section the card is.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.HeadingLevel, 2);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        var title = component.Find(".bit-crd-ttl");

        Assert.AreEqual("heading", title.GetAttribute("role"));
        Assert.AreEqual("2", title.GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitCardWithoutHeadingLevelShouldLeaveTheTitlePlain()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
        });

        var title = component.Find(".bit-crd-ttl");

        Assert.IsNull(title.GetAttribute("role"));
        Assert.IsNull(title.GetAttribute("aria-level"));
    }

    #endregion

    #region Cover, body & footer

    [TestMethod]
    public void BitCardImageUrlShouldRenderTheCoverImage()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
        });

        var image = component.Find(".bit-crd-img");

        Assert.AreEqual("/images/a.png", image.GetAttribute("src"));
        Assert.AreEqual(string.Empty, image.GetAttribute("alt"));
        Assert.AreEqual(1, component.FindAll(".bit-crd-cvr").Count);
    }

    [TestMethod]
    public void BitCardImageAltShouldNameTheCoverImage()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.ImageAlt, "A picture");
        });

        Assert.AreEqual("A picture", component.Find(".bit-crd-img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitCardImageHeightShouldRenderItsCustomProperty()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.ImageHeight, "9rem");
        });

        Assert.IsTrue(component.Find(".bit-crd").GetAttribute("style")!.Contains("--bit-crd-img-height:9rem"));
    }

    [TestMethod]
    [DataRow(BitImageLoading.Lazy, "lazy")]
    [DataRow(BitImageLoading.Eager, "eager")]
    public void BitCardImageLoadingShouldReachTheCoverImage(BitImageLoading loading, string expected)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.ImageLoading, loading);
        });

        Assert.AreEqual(expected, component.Find(".bit-crd-img").GetAttribute("loading"));
    }

    [TestMethod]
    public void BitCardWithoutImageLoadingShouldLeaveTheAttributeOff()
    {
        // No attribute at all rather than an explicit eager: eager is what the browser does anyway, and a card
        // that says so out loud cannot be told apart from one that was asked for it.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
        });

        Assert.IsNull(component.Find(".bit-crd-img").GetAttribute("loading"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardCoverOverlayTest(bool coverOverlay)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.CoverOverlay, coverOverlay);
        });

        Assert.AreEqual(coverOverlay, component.Find(".bit-crd").ClassList.Contains("bit-crd-ovl"));
    }

    [TestMethod]
    public void BitCardCoverOverlayShouldKeepTheCoverAndTheMainColumn()
    {
        // The overlay is only a layer change: the same cover and the same column of content are rendered, and
        // the raised slots are still inside that column so a linked hero card keeps its controls.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.CoverOverlay, true);
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Footer, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "ftr-btn");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-crd-sct > .bit-crd-cvr > .bit-crd-img").Count);
        Assert.AreEqual(1, component.FindAll(".bit-crd-sct > .bit-crd-mai > .bit-crd-ftr > .ftr-btn").Count);
    }

    [TestMethod]
    public void BitCardCoverWidthShouldRenderItsCustomProperty()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.CoverWidth, "8rem");
        });

        Assert.IsTrue(component.Find(".bit-crd").GetAttribute("style")!.Contains("--bit-crd-cvr-width:8rem"));
    }

    [TestMethod]
    public void BitCardCoverRatioShouldRenderItsClassAndCustomProperty()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.CoverRatio, "16 / 9");
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-cra"));
        Assert.IsTrue(card.GetAttribute("style")!.Contains("--bit-crd-cvr-ratio:16 / 9"));
    }

    [TestMethod]
    public void BitCardWithoutCoverRatioShouldNotRenderTheRatioClass()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
        });

        var card = component.Find(".bit-crd");

        Assert.IsFalse(card.ClassList.Contains("bit-crd-cra"));
        Assert.IsFalse(card.GetAttribute("style")?.Contains("--bit-crd-cvr-ratio") ?? false);
    }

    [TestMethod]
    public void BitCardCoverRatioAloneShouldNotSectionTheCard()
    {
        // The ratio only sizes a cover; a card that has none is still the plain padded box it started as.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.CoverRatio, "1 / 1");
            parameters.AddChildContent("<span class=\"inner\">body</span>");
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-cra"));
        Assert.IsFalse(card.ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual(0, component.FindAll(".bit-crd-cvr").Count);
    }

    [TestMethod]
    public void BitCardImagePositionShouldRenderItsCustomProperty()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/img.png");
            parameters.Add(p => p.ImagePosition, "top");
        });

        Assert.IsTrue(component.Find(".bit-crd").GetAttribute("style")!.Contains("--bit-crd-img-position:top"));
    }

    [TestMethod]
    public void BitCardWithoutImagePositionShouldNotRenderItsCustomProperty()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/img.png");
        });

        Assert.IsFalse((component.Find(".bit-crd").GetAttribute("style") ?? string.Empty).Contains("--bit-crd-img-position"));
    }

    [TestMethod]
    public void BitCardImagePositionAloneShouldNotSectionTheCard()
    {
        // It is only the crop of a cover the card already has; on its own there is nothing for it to move.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImagePosition, "top");
            parameters.AddChildContent("<span class=\"inner\">body</span>");
        });

        var card = component.Find(".bit-crd");

        Assert.IsFalse(card.ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual(0, component.FindAll(".bit-crd-cvr").Count);
    }

    [TestMethod]
    public void BitCardCoverShouldTakePrecedenceOverImageUrl()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.Cover, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-cover");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-crd-cvr .custom-cover").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-img").Count);
    }

    [TestMethod]
    public void BitCardChildContentShouldRenderIntoTheBodyOfASectionedCard()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.AddChildContent("<span class=\"inner\">body</span>");
        });

        Assert.AreEqual(1, component.FindAll(".bit-crd-bdy .inner").Count);
    }

    [TestMethod]
    public void BitCardWithoutChildContentShouldNotRenderABody()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
        });

        Assert.AreEqual(0, component.FindAll(".bit-crd-bdy").Count);
    }

    [TestMethod]
    public void BitCardWithOnlyACoverShouldNotRenderAnEmptyMainColumn()
    {
        // An empty main column would hang its own block padding under the picture.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
        });

        Assert.AreEqual(1, component.FindAll(".bit-crd-cvr").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-mai").Count);
    }

    [TestMethod]
    public void BitCardFooterShouldRenderAndSectionTheCard()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Footer, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "ftr-btn");
                builder.CloseElement();
            }));
        });

        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual(1, component.FindAll(".bit-crd-ftr .ftr-btn").Count);
    }

    [TestMethod]
    public void BitCardPartsShouldRenderInDocumentOrder()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Footer, (RenderFragment)(builder => builder.AddContent(0, "footer")));
            parameters.AddChildContent("<span>body</span>");
        });

        var html = component.Find(".bit-crd").InnerHtml;

        // The stylesheet and the layout both depend on this shape: cover, then main with the header, the body
        // and the footer inside it.
        var cover = html.IndexOf("bit-crd-cvr");
        var main = html.IndexOf("bit-crd-mai");
        var header = html.IndexOf("bit-crd-hdr");
        var body = html.IndexOf("bit-crd-bdy");
        var footer = html.IndexOf("bit-crd-ftr");

        Assert.IsTrue(cover >= 0 && main > cover && header > main && body > header && footer > body);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardDividerTest(bool divider)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Divider, divider);
        });

        Assert.AreEqual(divider, component.Find(".bit-crd").ClassList.Contains("bit-crd-dvd"));
    }

    [TestMethod]
    public void BitCardDividerAloneShouldNotSectionTheCard()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Divider, true);
            parameters.AddChildContent("<div>Body</div>");
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-dvd"));
        Assert.IsFalse(card.ClassList.Contains("bit-crd-sct"));
    }

    #endregion

    #region Floating actions

    [TestMethod]
    public void BitCardFloatingActionsShouldRenderAsADirectChildOfTheRoot()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.FloatingActions, (RenderFragment)(builder => builder.AddContent(0, "star")));
            parameters.AddChildContent("<span class=\"inner\">body</span>");
        });

        var floating = component.Find(".bit-crd-fac");

        // It floats over the whole surface, so it hangs off the root rather than off the column of parts.
        Assert.AreEqual(1, component.FindAll(".bit-crd > .bit-crd-fac").Count);
        Assert.AreEqual("star", floating.TextContent);
    }

    [TestMethod]
    public void BitCardFloatingActionsAloneShouldNotSectionTheCard()
    {
        // A slot that is positioned over the card needs no column of parts under it.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.FloatingActions, (RenderFragment)(builder => builder.AddContent(0, "star")));
            parameters.AddChildContent("<span class=\"inner\">body</span>");
        });

        var card = component.Find(".bit-crd");

        Assert.IsFalse(card.ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual(0, component.FindAll(".bit-crd-mai").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-bdy").Count);
        Assert.AreEqual(1, component.FindAll(".bit-crd > .inner").Count);
    }

    [TestMethod]
    public void BitCardFloatingActionsShouldStayOutsideTheMainColumnOfASectionedCard()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.FloatingActions, (RenderFragment)(builder => builder.AddContent(0, "star")));
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual(1, component.FindAll(".bit-crd > .bit-crd-fac").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-mai .bit-crd-fac").Count);
    }

    [TestMethod]
    public void BitCardFloatingActionsShouldRenderAfterTheStretchedLink()
    {
        // The anchor is painted under the slot, which is what keeps the control in it clickable on a linked card.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.FloatingActions, (RenderFragment)(builder => builder.AddContent(0, "star")));
        });

        var children = component.Find(".bit-crd").Children;

        Assert.IsTrue(children[0].ClassList.Contains("bit-crd-lnk"));
        Assert.IsTrue(children[1].ClassList.Contains("bit-crd-fac"));
    }

    [TestMethod]
    public void BitCardWithoutFloatingActionsShouldNotRenderTheSlot()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
        });

        Assert.AreEqual(0, component.FindAll(".bit-crd-fac").Count);
    }

    #endregion

    #region Reactivity

    [TestMethod]
    public void BitCardShouldSectionItselfWhenAPartArrivesLater()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.AddChildContent("<span class=\"inner\">body</span>");
        });

        Assert.IsFalse(component.Find(".bit-crd").ClassList.Contains("bit-crd-sct"));

        component.Render(parameters =>
        {
            parameters.AddChildContent("<span class=\"inner\">body</span>");
            parameters.Add(p => p.Footer, (RenderFragment)(builder => builder.AddContent(0, "footer")));
        });

        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual(1, component.FindAll(".bit-crd-bdy .inner").Count);
    }

    [TestMethod]
    public void BitCardShouldSectionItselfWhenAnIconTemplateArrivesLater()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Body</div>");
        });

        Assert.IsFalse(component.Find(".bit-crd").ClassList.Contains("bit-crd-sct"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IconTemplate, (RenderFragment)(builder => builder.AddContent(0, "Avatar")));
            parameters.AddChildContent("<div class=\"content\">Body</div>");
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual(1, component.FindAll(".bit-crd-hic").Count);
        Assert.AreEqual(1, component.FindAll(".bit-crd-bdy .content").Count);
    }

    [TestMethod]
    public void BitCardShouldBecomeAControlWhenAClickHandlerArrivesLater()
    {
        var component = RenderComponent<BitCard>();

        Assert.IsFalse(component.Find(".bit-crd").ClassList.Contains("bit-crd-int"));

        component.Render(parameters => parameters.Add(p => p.OnClick, () => { }));

        var card = component.Find(".bit-crd");
        Assert.IsTrue(card.ClassList.Contains("bit-crd-int"));
        Assert.AreEqual("button", card.GetAttribute("role"));
    }

    [TestMethod]
    public void BitCardShouldStopBeingAControlWhenTheHrefGoesAway()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("bit-crd-int"));

        component.Render(parameters => parameters.Add(p => p.Href, (string?)null));

        Assert.IsFalse(component.Find(".bit-crd").ClassList.Contains("bit-crd-int"));
        Assert.AreEqual(0, component.FindAll(".bit-crd-lnk").Count);
    }

    #endregion

    #region Loading

    [TestMethod]
    public void BitCardLoadingShouldStandInForTheBody()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Loading, true);
            parameters.AddChildContent("<span class=\"inner\">body</span>");
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-ldg"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual("true", card.GetAttribute("aria-busy"));
        Assert.AreEqual(1, component.FindAll(".bit-crd-bdy .bit-crd-skl").Count);
        Assert.AreEqual(0, component.FindAll(".inner").Count);
    }

    [TestMethod]
    public void BitCardLoadingShouldKeepTheHeader()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Loading, true);
            parameters.Add(p => p.Title, "Weekly report");
        });

        Assert.AreEqual("Weekly report", component.Find(".bit-crd-ttl").TextContent);
    }

    [TestMethod]
    public void BitCardLoadingTemplateShouldReplaceTheDefaultPlaceholder()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Loading, true);
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-loading");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-crd-bdy .custom-loading").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-skl").Count);
    }

    [TestMethod]
    public void BitCardLoadedCardShouldGiveTheContentBack()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Loading, true);
            parameters.AddChildContent("<span class=\"inner\">body</span>");
        });

        component.Render(parameters => parameters.Add(p => p.Loading, false));

        var card = component.Find(".bit-crd");

        Assert.IsFalse(card.ClassList.Contains("bit-crd-ldg"));
        Assert.IsNull(card.GetAttribute("aria-busy"));
        Assert.AreEqual(1, component.FindAll(".inner").Count);
        Assert.AreEqual(0, component.FindAll(".bit-crd-skl").Count);
    }

    #endregion

    #region Horizontal, Reversed & Hoverable

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardHorizontalTest(bool horizontal)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Horizontal, horizontal);
        });

        Assert.AreEqual(horizontal, component.Find(".bit-crd").ClassList.Contains("bit-crd-hrz"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardReversedTest(bool reversed)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        Assert.AreEqual(reversed, component.Find(".bit-crd").ClassList.Contains("bit-crd-rev"));
    }

    [TestMethod]
    public void BitCardReversedShouldComposeWithHorizontal()
    {
        // The two are independent: one picks the axis, the other picks which end of it the cover goes on.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.Reversed, true);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-hrz"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-rev"));
    }

    [TestMethod]
    public void BitCardReversedShouldNotChangeTheDocumentOrderOfTheParts()
    {
        // The order is reversed by the layout rather than by the markup, so the reading order of the card - the
        // one a screen reader and a stylesheet-less page follow - is the same either way.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.ImageUrl, "/img.png");
        });

        var children = component.Find(".bit-crd").Children;

        Assert.IsTrue(children[0].ClassList.Contains("bit-crd-cvr"));
        Assert.IsTrue(children[1].ClassList.Contains("bit-crd-mai"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardScrollableBodyTest(bool scrollableBody)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ScrollableBody, scrollableBody);
        });

        Assert.AreEqual(scrollableBody, component.Find(".bit-crd").ClassList.Contains("bit-crd-scb"));
    }

    [TestMethod]
    public void BitCardScrollableBodyAloneShouldNotSectionTheCard()
    {
        // It says what the content does inside the card, not that the card grew any parts.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.ScrollableBody, true);
            parameters.AddChildContent("<span class=\"inner\">body</span>");
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-scb"));
        Assert.IsFalse(card.ClassList.Contains("bit-crd-sct"));
        Assert.AreEqual(0, component.FindAll(".bit-crd-bdy").Count);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardHoverableTest(bool hoverable)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Hoverable, hoverable);
        });

        Assert.AreEqual(hoverable, component.Find(".bit-crd").ClassList.Contains("bit-crd-hov"));
    }

    #endregion

    #region Click

    [TestMethod]
    public void BitCardOnClickShouldMakeTheRootAButton()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual("button", card.GetAttribute("role"));
        Assert.AreEqual("0", card.GetAttribute("tabindex"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-int"));
    }

    [TestMethod]
    public void BitCardWithoutOnClickShouldNotBeAControl()
    {
        var component = RenderComponent<BitCard>();

        var card = component.Find(".bit-crd");

        Assert.IsNull(card.GetAttribute("role"));
        Assert.IsNull(card.GetAttribute("tabindex"));
        Assert.IsFalse(card.ClassList.Contains("bit-crd-int"));
    }

    [TestMethod]
    public void BitCardOnClickShouldFireOnClick()
    {
        var clicked = 0;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        component.Find(".bit-crd").Click();

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    [DataRow("Enter")]
    [DataRow(" ")]
    [DataRow("Spacebar")]
    public void BitCardOnClickShouldFireOnActivationKeys(string key)
    {
        var clicked = 0;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        component.Find(".bit-crd").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    [DataRow("a")]
    [DataRow("Tab")]
    [DataRow("Escape")]
    public void BitCardOnClickShouldNotFireOnOtherKeys(string key)
    {
        var clicked = 0;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        component.Find(".bit-crd").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public void BitCardDisabledShouldNotFireOnClick()
    {
        var clicked = 0;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        var card = component.Find(".bit-crd");
        card.Click();
        card.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, clicked);
        Assert.IsTrue(card.ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", card.GetAttribute("aria-disabled"));
        Assert.IsNull(card.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCardTabIndexShouldOverrideTheDefaultOfAClickableCard()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("3", component.Find(".bit-crd").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCardActionsShouldNotFireTheClickOfACardThatIsAButton()
    {
        // Pressing the overflow menu of a card is pressing the menu, not the card under it.
        var cardClicks = 0;
        var actionClicks = 0;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.OnClick, () => cardClicks++);
            parameters.Add(p => p.Actions, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "action");
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => actionClicks++));
                builder.CloseElement();
            }));
        });

        component.Find(".action").Click();

        Assert.AreEqual(1, actionClicks);
        Assert.AreEqual(0, cardClicks);

        // And the card itself still answers the pointer.
        component.Find(".bit-crd").Click();

        Assert.AreEqual(1, cardClicks);
    }

    [TestMethod]
    public void BitCardFooterShouldNotFireTheClickOfACardThatIsAButton()
    {
        var cardClicks = 0;
        var footerClicks = 0;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => cardClicks++);
            parameters.Add(p => p.Footer, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "footer-button");
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => footerClicks++));
                builder.CloseElement();
            }));
        });

        component.Find(".footer-button").Click();

        Assert.AreEqual(1, footerClicks);
        Assert.AreEqual(0, cardClicks);
    }

    [TestMethod]
    public void BitCardFloatingActionsShouldNotFireTheClickOfACardThatIsAButton()
    {
        var cardClicks = 0;
        var floatingClicks = 0;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => cardClicks++);
            parameters.Add(p => p.FloatingActions, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "floating-button");
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => floatingClicks++));
                builder.CloseElement();
            }));
        });

        component.Find(".floating-button").Click();

        Assert.AreEqual(1, floatingClicks);
        Assert.AreEqual(0, cardClicks);
    }

    [TestMethod]
    public void BitCardFooterKeyPressShouldNotToggleACardThatIsAButton()
    {
        // The keyboard reaches the buttons of a card the same way the pointer does, and the space that presses
        // one of them must not press the card as well.
        var selected = false;
        var footerKeys = 0;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Bind(p => p.Selected, selected, v => selected = v);
            parameters.Add(p => p.Footer, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "footer-button");
                builder.AddAttribute(2, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => footerKeys++));
                builder.CloseElement();
            }));
        });

        component.Find(".footer-button").KeyDown(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual(1, footerKeys);
        Assert.IsFalse(selected);
    }

    [TestMethod]
    public void BitCardFooterShouldNotFireTheClickOfALinkedCardEither()
    {
        // A linked card is not a button, but its click handler is still its own: the buttons the block-link
        // pattern keeps reachable in the footer must not fire it on their way past.
        var cardClicks = 0;
        var footerClicks = 0;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.OnClick, () => cardClicks++);
            parameters.Add(p => p.Footer, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "footer-button");
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => footerClicks++));
                builder.CloseElement();
            }));
        });

        component.Find(".footer-button").Click();

        Assert.AreEqual(1, footerClicks);
        Assert.AreEqual(0, cardClicks);

        component.Find(".bit-crd").Click();

        Assert.AreEqual(1, cardClicks);
    }

    [TestMethod]
    public void BitCardFooterClickShouldStillLeaveAPlainCard()
    {
        // A card that is not a control of its own swallows nothing: whatever the app wrapped the card in still
        // sees the clicks of the buttons inside it, the way it would without a card in between.
        var outerClicks = 0;

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "outer");
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => outerClicks++));

                builder.OpenComponent<BitCard>(3);
                builder.AddAttribute(4, nameof(BitCard.Footer), (RenderFragment)(inner =>
                {
                    inner.OpenElement(0, "button");
                    inner.AddAttribute(1, "class", "footer-button");
                    inner.CloseElement();
                }));
                builder.CloseComponent();

                builder.CloseElement();
            });
        });

        component.Find(".footer-button").Click();

        Assert.AreEqual(1, outerClicks);
    }

    #endregion

    #region Link

    [TestMethod]
    public void BitCardHrefShouldRenderTheStretchedLink()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        var link = component.Find(".bit-crd-lnk");

        Assert.AreEqual("https://bitplatform.dev", link.GetAttribute("href"));
        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("bit-crd-int"));
    }

    [TestMethod]
    public void BitCardWithoutHrefShouldNotRenderTheStretchedLink()
    {
        var component = RenderComponent<BitCard>();

        Assert.AreEqual(0, component.FindAll(".bit-crd-lnk").Count);
    }

    [TestMethod]
    public void BitCardLinkShouldNotMakeTheRootAButton()
    {
        // The anchor over the surface is the control; a role on the root as well would be a second one.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.OnClick, () => { });
        });

        Assert.AreNotEqual("button", component.Find(".bit-crd").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCardLinkShouldBeNamedByTheTitle()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Title, "bit BlazorUI");
        });

        var link = component.Find(".bit-crd-lnk");
        var title = component.Find(".bit-crd-ttl");

        Assert.AreEqual(title.Id, link.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCardLinkAriaLabelShouldWinOverTheTitle()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Title, "bit BlazorUI");
            parameters.Add(p => p.AriaLabel, "Open the docs");
        });

        var link = component.Find(".bit-crd-lnk");

        Assert.AreEqual("Open the docs", link.GetAttribute("aria-label"));
        Assert.IsNull(link.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCardLinkShouldFallBackToTheSubtitleForItsName()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Subtitle, "blazorui.bitplatform.dev");
        });

        var link = component.Find(".bit-crd-lnk");
        var subtitle = component.Find(".bit-crd-sub");

        Assert.AreEqual(subtitle.Id, link.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCardLinkShouldNotBeNamedByATitleACustomHeaderTookOff()
    {
        // A custom header replaces the title and the subtitle, so neither id is on the page. Pointing the link
        // at one of them anyway would name it after an element that was never rendered, which leaves it with no
        // accessible name at all - worse than the href a reader would otherwise hear.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Title, "bit BlazorUI");
            parameters.Add(p => p.Subtitle, "blazorui.bitplatform.dev");
            parameters.Add(p => p.HeaderTemplate, (RenderFragment)(builder => builder.AddContent(0, "Custom")));
        });

        Assert.AreEqual(0, component.FindAll(".bit-crd-ttl").Count);
        Assert.IsNull(component.Find(".bit-crd-lnk").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCardLinkAriaLabelShouldStillNameACustomHeaderCard()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Title, "bit BlazorUI");
            parameters.Add(p => p.HeaderTemplate, (RenderFragment)(builder => builder.AddContent(0, "Custom")));
            parameters.Add(p => p.AriaLabel, "Open the docs");
        });

        var link = component.Find(".bit-crd-lnk");

        Assert.AreEqual("Open the docs", link.GetAttribute("aria-label"));
        Assert.IsNull(link.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCardLinkShouldTakeTheNameOffTheRoot()
    {
        // The name belongs to the control the reader lands on; a copy on the wrapper reads the words twice.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.AriaLabel, "Open the docs");
        });

        var card = component.Find(".bit-crd");

        Assert.IsNull(card.GetAttribute("aria-label"));
        Assert.IsNull(card.GetAttribute("role"));
    }

    [TestMethod]
    public void BitCardTargetBlankShouldGetNoOpener()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Target, "_blank");
        });

        Assert.AreEqual("noopener", component.Find(".bit-crd-lnk").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitCardExplicitRelShouldWinOverTheDefault()
    {
        // noreferrer already keeps the opened page from reaching back, so noopener is not added on top of it.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Rel, BitLinkRels.NoFollow | BitLinkRels.NoReferrer);
        });

        var rel = component.Find(".bit-crd-lnk").GetAttribute("rel")!;

        Assert.IsTrue(rel.Contains("nofollow"));
        Assert.IsTrue(rel.Contains("noreferrer"));
        Assert.IsFalse(rel.Contains("noopener"));
    }

    [TestMethod]
    public void BitCardExplicitRelShouldStillGetNoOpenerForTargetBlank()
    {
        // A rel that says nothing about the opener does not waive the protection against reverse tabnabbing.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Rel, BitLinkRels.NoFollow);
        });

        var rel = component.Find(".bit-crd-lnk").GetAttribute("rel")!;

        Assert.IsTrue(rel.Contains("nofollow"));
        Assert.IsTrue(rel.Contains("noopener"));
    }

    [TestMethod]
    public void BitCardFragmentHrefShouldNotGetARel()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "#section");
            parameters.Add(p => p.Target, "_blank");
        });

        Assert.IsNull(component.Find(".bit-crd-lnk").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitCardDownloadShouldReachTheStretchedLink()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "/files/a.pdf");
            parameters.Add(p => p.Download, "a.pdf");
        });

        Assert.AreEqual("a.pdf", component.Find(".bit-crd-lnk").GetAttribute("download"));
    }

    [TestMethod]
    public void BitCardDisabledLinkShouldLoseItsHrefAndFocus()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.IsEnabled, false);
        });

        var link = component.Find(".bit-crd-lnk");

        Assert.IsNull(link.GetAttribute("href"));
        Assert.AreEqual("-1", link.GetAttribute("tabindex"));
        Assert.AreEqual("true", link.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitCardSplattedLabelledByShouldNameTheStretchedLink()
    {
        // The name of a linked card belongs to the anchor a reader lands on, whichever way the card was named -
        // and a second copy of it left on the wrapper would have the same words announced twice.
        var component = RenderSplattedCard(builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Href), "https://bitplatform.dev");
            builder.AddAttribute(2, nameof(BitCard.Title), "Title");
            builder.AddAttribute(3, "aria-labelledby", "some-heading");
        });

        Assert.IsNull(component.Find(".bit-crd").GetAttribute("aria-labelledby"));
        Assert.AreEqual("some-heading", component.Find(".bit-crd-lnk").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCardSplattedLabelledByShouldStayOnAnUnlinkedCard()
    {
        var component = RenderSplattedCard(builder => builder.AddAttribute(1, "aria-labelledby", "some-heading"));

        Assert.AreEqual("some-heading", component.Find(".bit-crd").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCardLinkTabIndexShouldReachTheAnchorAndNotTheRoot()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.IsNull(component.Find(".bit-crd").GetAttribute("tabindex"));
        Assert.AreEqual("3", component.Find(".bit-crd-lnk").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCardLinkShouldNotTakeATabStopOfItsOwn()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.OnClick, () => { });
        });

        Assert.IsNull(component.Find(".bit-crd").GetAttribute("tabindex"));
        Assert.IsNull(component.Find(".bit-crd-lnk").GetAttribute("tabindex"));
    }

    #endregion

    #region Selection

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardSelectedShouldApplyItsClass(bool selected)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Selected, selected);
        });

        Assert.AreEqual(selected, component.Find(".bit-crd").ClassList.Contains("bit-crd-sel"));
    }

    [TestMethod]
    public void BitCardBoundSelectedShouldMakeTheCardAToggle()
    {
        var selected = false;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Bind(p => p.Selected, selected, v => selected = v);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual("button", card.GetAttribute("role"));
        Assert.AreEqual("false", card.GetAttribute("aria-pressed"));

        card.Click();

        Assert.IsTrue(selected);
        Assert.AreEqual("true", component.Find(".bit-crd").GetAttribute("aria-pressed"));
        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("bit-crd-sel"));
    }

    [TestMethod]
    public void BitCardBoundSelectedShouldToggleOnTheKeyboard()
    {
        var selected = false;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Bind(p => p.Selected, selected, v => selected = v);
        });

        component.Find(".bit-crd").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsTrue(selected);
    }

    [TestMethod]
    public void BitCardDisabledShouldNotToggleTheSelection()
    {
        var selected = false;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.Selected, selected, v => selected = v);
        });

        component.Find(".bit-crd").Click();

        Assert.IsFalse(selected);
    }

    [TestMethod]
    public void BitCardLinkedShouldNotToggleTheSelectionButShouldStillClick()
    {
        var selected = false;
        var clicked = false;

        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.OnClick, () => clicked = true);
            parameters.Bind(p => p.Selected, selected, v => selected = v);
        });

        component.Find(".bit-crd").Click();

        Assert.IsFalse(selected);
        Assert.IsTrue(clicked);
    }

    [TestMethod]
    public void BitCardClickableSelectedShouldReportAriaPressedWithoutABinding()
    {
        // The app flipped the value itself rather than binding it. The card is still a button that is pressed, and a
        // button whose state only the sighted reader can see has no state at all.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
            parameters.Add(p => p.Selected, true);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual("button", card.GetAttribute("role"));
        Assert.AreEqual("true", card.GetAttribute("aria-pressed"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-sel"));
    }

    [TestMethod]
    public void BitCardClickableWithoutAnySelectionShouldNotReportAriaPressed()
    {
        // A card that only does something is a plain button, and a plain button is not a toggle.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual("button", card.GetAttribute("role"));
        Assert.IsNull(card.GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitCardLinkedSelectedShouldNotReportAriaPressed()
    {
        // aria-pressed belongs to a button, and a linked card is not one.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Selected, true);
        });

        var card = component.Find(".bit-crd");

        Assert.IsNull(card.GetAttribute("aria-pressed"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-sel"));
    }

    [TestMethod]
    [DataRow("option")]
    [DataRow("row")]
    [DataRow("gridcell")]
    [DataRow("tab")]
    [DataRow("treeitem")]
    [DataRow("columnheader")]
    [DataRow("rowheader")]
    public void BitCardSelectedShouldReportAriaSelectedOnARoleThatCarriesIt(string role)
    {
        // aria-pressed belongs to a button. A card made an item of a listbox, a grid, a tree or a tab strip
        // reports the very same selection with the state its own role answers to.
        var component = RenderSplattedCard(builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Selected), true);
            builder.AddAttribute(2, nameof(BitCard.SelectedChanged), EventCallback.Factory.Create<bool>(this, _ => { }));
            builder.AddAttribute(3, "role", role);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual(role, card.GetAttribute("role"));
        Assert.AreEqual("true", card.GetAttribute("aria-selected"));
        Assert.IsNull(card.GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitCardUnselectedShouldStillReportAriaSelectedOnARoleThatCarriesIt()
    {
        // A toggle that only says so while it is on is a toggle whose off state cannot be told from no state.
        var component = RenderSplattedCard(builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.SelectedChanged), EventCallback.Factory.Create<bool>(this, _ => { }));
            builder.AddAttribute(2, "role", "option");
        });

        Assert.AreEqual("false", component.Find(".bit-crd").GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitCardWithoutASelectionShouldNotReportAriaSelected()
    {
        var component = RenderSplattedCard(builder => builder.AddAttribute(1, "role", "option"));

        Assert.IsNull(component.Find(".bit-crd").GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitCardSelectedShouldReportNeitherStateOnARoleThatCarriesNeither()
    {
        // An article is not a control and has no selection to report; the ring is all such a card has to show
        // for it, which is what the Selected docs warn about.
        var component = RenderSplattedCard(builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Selected), true);
            builder.AddAttribute(2, nameof(BitCard.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
            builder.AddAttribute(3, "role", "article");
        });

        var card = component.Find(".bit-crd");

        Assert.IsNull(card.GetAttribute("aria-pressed"));
        Assert.IsNull(card.GetAttribute("aria-selected"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-sel"));
    }

    [TestMethod]
    public void BitCardSelectableCardShouldNotReportAriaSelectedOnItsOwnRole()
    {
        // The roles the card picks for itself - the button of a clickable card, the group of a named one - do
        // not carry a selection, so nothing but aria-pressed is reported there.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
            parameters.Add(p => p.Selected, true);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual("true", card.GetAttribute("aria-pressed"));
        Assert.IsNull(card.GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitCardUnboundSelectedShouldNotReportAriaPressed()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Selected, true);
        });

        Assert.IsNull(component.Find(".bit-crd").GetAttribute("aria-pressed"));
    }

    #endregion

    #region Accessibility & attributes

    [TestMethod]
    public void BitCardAriaLabelShouldGiveTheCardAGroupRole()
    {
        // aria-label on an element with no role of its own is not guaranteed to be announced.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "A card");
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual("A card", card.GetAttribute("aria-label"));
        Assert.AreEqual("group", card.GetAttribute("role"));
    }

    [TestMethod]
    public void BitCardSplattedLabelledByShouldGiveTheCardAGroupRole()
    {
        // A name pointing at a heading elsewhere on the page names the card just as an AriaLabel does, and it is
        // just as unlikely to be announced on an element that reports no role at all.
        var component = RenderSplattedCard(builder => builder.AddAttribute(1, "aria-labelledby", "some-heading"));

        var card = component.Find(".bit-crd");

        Assert.AreEqual("some-heading", card.GetAttribute("aria-labelledby"));
        Assert.AreEqual("group", card.GetAttribute("role"));
    }

    [TestMethod]
    public void BitCardWithNoNameAtAllShouldNotReportAGroupRole()
    {
        // A group is the role of a box that was given a name; an unnamed one is just a box.
        var component = RenderComponent<BitCard>(parameters => parameters.Add(p => p.Title, "Title"));

        Assert.IsNull(component.Find(".bit-crd").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCardLinkedCardShouldNotBecomeAGroupFromItsLabelledBy()
    {
        // The name of a linked card belongs to the anchor stretched over it, which is the control a reader lands
        // on, so the wrapper stays a plain box rather than becoming a second named thing.
        var component = RenderSplattedCard(builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.Href), "https://bitplatform.dev");
            builder.AddAttribute(2, "aria-labelledby", "some-heading");
        });

        Assert.IsNull(component.Find(".bit-crd").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCardSplattedRoleShouldWinOverTheOneTheCardPicks()
    {
        // The splat only reaches the card through the render tree: HtmlAttributes is a plain parameter on
        // BitComponentBase rather than a CaptureUnmatchedValues one, so bUnit's AddUnmatched cannot feed it.
        var component = RenderSplattedCard(builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.AriaLabel), "A card");
            builder.AddAttribute(2, "role", "article");
        });

        Assert.AreEqual("article", component.Find(".bit-crd").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCardTabIndexShouldReachAPlainCard()
    {
        // A card the page asked to be focusable is focusable, control or not - and the stylesheet gives every
        // focusable card a ring, so it is one the keyboard can see it has reached.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "0");
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual("0", card.GetAttribute("tabindex"));
        Assert.IsNull(card.GetAttribute("role"));
    }

    [TestMethod]
    public void BitCardDisabledPlainCardShouldLeaveTheTabOrder()
    {
        // A stop the keyboard can still land on but nothing answers on is worse than no stop at all, and that
        // holds for a card the page made focusable itself just as much as for one the card made a button.
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "0");
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsNull(component.Find(".bit-crd").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCardDisabledSplattedTabIndexShouldLeaveTheTabOrderToo()
    {
        var component = RenderSplattedCard(builder =>
        {
            builder.AddAttribute(1, nameof(BitCard.IsEnabled), false);
            builder.AddAttribute(2, "tabindex", "0");
        });

        Assert.IsNull(component.Find(".bit-crd").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCardIdShouldLandOnTheRoot()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Id, "my-card");
        });

        Assert.AreEqual("my-card", component.Find(".bit-crd").Id);
    }

    [TestMethod]
    public void BitCardTitleIdShouldBeDerivedFromTheCardId()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Id, "my-card");
            parameters.Add(p => p.Title, "Title");
        });

        Assert.AreEqual("my-card-ttl", component.Find(".bit-crd-ttl").Id);
    }

    [TestMethod]
    [DataRow(BitDir.Ltr, "ltr")]
    [DataRow(BitDir.Rtl, "rtl")]
    [DataRow(BitDir.Auto, "auto")]
    public void BitCardDirShouldLandOnTheRoot(BitDir dir, string expected)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual(expected, card.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, card.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitCardVisibilityTest(BitVisibility visibility, string expected)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-crd").GetAttribute("style") ?? string.Empty;

        if (expected.HasValue())
        {
            Assert.IsTrue(style.Contains(expected));
        }
        else
        {
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
    }

    [TestMethod]
    public void BitCardHtmlAttributesShouldLandOnTheRoot()
    {
        var component = RenderSplattedCard(builder => builder.AddAttribute(1, "data-test", "card"));

        Assert.AreEqual("card", component.Find(".bit-crd").GetAttribute("data-test"));
    }

    private IRenderedComponent<BitParams> RenderSplattedCard(Action<RenderTreeBuilder> attributes)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                attributes(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCardDisabledPlainCardShouldNotReportAriaDisabled()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-dis"));
        Assert.IsNull(card.GetAttribute("role"));
        Assert.IsNull(card.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitCardDisabledNamedCardShouldReportAriaDisabled()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Release notes");
            parameters.Add(p => p.IsEnabled, false);
        });

        var card = component.Find(".bit-crd");

        Assert.AreEqual("group", card.GetAttribute("role"));
        Assert.AreEqual("true", card.GetAttribute("aria-disabled"));
    }

    #endregion

    #region Classes & Styles

    [TestMethod]
    public void BitCardClassAndStyleShouldLandOnTheRoot()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "color: red");
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("custom-class"));
        Assert.IsTrue(card.GetAttribute("style")!.Contains("color: red"));
    }

    [TestMethod]
    public void BitCardClassesShouldReachEveryPart()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
            parameters.Add(p => p.IconName, "Album");
            parameters.Add(p => p.Actions, (RenderFragment)(builder => builder.AddContent(0, "actions")));
            parameters.Add(p => p.FloatingActions, (RenderFragment)(builder => builder.AddContent(0, "star")));
            parameters.Add(p => p.Footer, (RenderFragment)(builder => builder.AddContent(0, "footer")));
            parameters.AddChildContent("<span>body</span>");
            parameters.Add(p => p.Classes, new BitCardClassStyles
            {
                Root = "c-root",
                Link = "c-link",
                FloatingActions = "c-floating",
                Cover = "c-cover",
                Image = "c-image",
                Main = "c-main",
                Header = "c-header",
                Icon = "c-icon",
                HeaderText = "c-headertext",
                Title = "c-title",
                Subtitle = "c-subtitle",
                Actions = "c-actions",
                Body = "c-body",
                Footer = "c-footer"
            });
        });

        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("c-root"));
        Assert.IsTrue(component.Find(".bit-crd-lnk").ClassList.Contains("c-link"));
        Assert.IsTrue(component.Find(".bit-crd-fac").ClassList.Contains("c-floating"));
        Assert.IsTrue(component.Find(".bit-crd-cvr").ClassList.Contains("c-cover"));
        Assert.IsTrue(component.Find(".bit-crd-img").ClassList.Contains("c-image"));
        Assert.IsTrue(component.Find(".bit-crd-mai").ClassList.Contains("c-main"));
        Assert.IsTrue(component.Find(".bit-crd-hdr").ClassList.Contains("c-header"));
        Assert.IsTrue(component.Find(".bit-crd-hic").ClassList.Contains("c-icon"));
        Assert.IsTrue(component.Find(".bit-crd-htx").ClassList.Contains("c-headertext"));
        Assert.IsTrue(component.Find(".bit-crd-ttl").ClassList.Contains("c-title"));
        Assert.IsTrue(component.Find(".bit-crd-sub").ClassList.Contains("c-subtitle"));
        Assert.IsTrue(component.Find(".bit-crd-act").ClassList.Contains("c-actions"));
        Assert.IsTrue(component.Find(".bit-crd-bdy").ClassList.Contains("c-body"));
        Assert.IsTrue(component.Find(".bit-crd-ftr").ClassList.Contains("c-footer"));
    }

    [TestMethod]
    public void BitCardStylesShouldReachEveryPart()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.ImageUrl, "/images/a.png");
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Subtitle, "Subtitle");
            parameters.Add(p => p.IconName, "Album");
            parameters.Add(p => p.Actions, (RenderFragment)(builder => builder.AddContent(0, "actions")));
            parameters.Add(p => p.FloatingActions, (RenderFragment)(builder => builder.AddContent(0, "star")));
            parameters.Add(p => p.Footer, (RenderFragment)(builder => builder.AddContent(0, "footer")));
            parameters.AddChildContent("<span>body</span>");
            parameters.Add(p => p.Styles, new BitCardClassStyles
            {
                Root = "color:red",
                Link = "color:navy",
                FloatingActions = "color:crimson",
                Cover = "color:orange",
                Image = "color:yellow",
                Main = "color:green",
                Header = "color:blue",
                Icon = "color:indigo",
                HeaderText = "color:violet",
                Title = "color:black",
                Subtitle = "color:white",
                Actions = "color:teal",
                Body = "color:gray",
                Footer = "color:brown"
            });
        });

        Assert.IsTrue(component.Find(".bit-crd").GetAttribute("style")!.Contains("color:red"));
        Assert.AreEqual("color:navy", component.Find(".bit-crd-lnk").GetAttribute("style"));
        Assert.AreEqual("color:crimson", component.Find(".bit-crd-fac").GetAttribute("style"));
        Assert.AreEqual("color:orange", component.Find(".bit-crd-cvr").GetAttribute("style"));
        Assert.AreEqual("color:yellow", component.Find(".bit-crd-img").GetAttribute("style"));
        Assert.AreEqual("color:green", component.Find(".bit-crd-mai").GetAttribute("style"));
        Assert.AreEqual("color:blue", component.Find(".bit-crd-hdr").GetAttribute("style"));
        Assert.AreEqual("color:indigo", component.Find(".bit-crd-hic").GetAttribute("style"));
        Assert.AreEqual("color:violet", component.Find(".bit-crd-htx").GetAttribute("style"));
        Assert.AreEqual("color:black", component.Find(".bit-crd-ttl").GetAttribute("style"));
        Assert.AreEqual("color:white", component.Find(".bit-crd-sub").GetAttribute("style"));
        Assert.AreEqual("color:teal", component.Find(".bit-crd-act").GetAttribute("style"));
        Assert.AreEqual("color:gray", component.Find(".bit-crd-bdy").GetAttribute("style"));
        Assert.AreEqual("color:brown", component.Find(".bit-crd-ftr").GetAttribute("style"));
    }

    [TestMethod]
    public void BitCardSelectedClassAndStyleShouldOnlyApplyWhileSelected()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Selected, true);
            parameters.Add(p => p.Classes, new BitCardClassStyles { Selected = "c-selected" });
            parameters.Add(p => p.Styles, new BitCardClassStyles { Selected = "color:hotpink" });
        });

        var card = component.Find(".bit-crd");
        Assert.IsTrue(card.ClassList.Contains("c-selected"));
        Assert.IsTrue(card.GetAttribute("style")!.Contains("color:hotpink"));

        component.Render(parameters => parameters.Add(p => p.Selected, false));

        card = component.Find(".bit-crd");
        Assert.IsFalse(card.ClassList.Contains("c-selected"));
        Assert.IsFalse((card.GetAttribute("style") ?? string.Empty).Contains("color:hotpink"));
    }

    #endregion

    #region BitParams

    [TestMethod]
    public void BitCardParamsShouldHaveCorrectParamName()
    {
        var paramName = BitCardParams.ParamName;
        var expectedName = $"{nameof(BitParams)}.{nameof(BitCard)}";

        Assert.AreEqual(expectedName, paramName);
    }

    [TestMethod]
    public void BitCardParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitCardParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitCardParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitCardShouldApplyCascadingParametersFromBitParams()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitCardParams
            {
                Color = BitColor.Success,
                Variant = BitVariant.Outline,
                Size = BitSize.Large,
                Elevation = 6,
                Square = true,
                NoPadding = true,
                Hoverable = true,
                Horizontal = true,
                Divider = true,
                Background = BitColorKind.Tertiary,
                Border = BitColorKind.Primary,
                Width = "20rem",
                MaxWidth = "30rem"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                builder.CloseComponent();
            });
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-suc"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-vot"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-lg"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-e6"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-sqr"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-npd"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-hov"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-hrz"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-dvd"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-btg"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-bpr"));

        var style = card.GetAttribute("style")!;
        Assert.IsTrue(style.Contains("width:20rem"));
        Assert.IsTrue(style.Contains("max-width:30rem"));
    }

    [TestMethod]
    public void BitCardDirectParametersShouldOverrideCascadingParameters()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitCardParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                Elevation = 6
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                builder.AddAttribute(1, nameof(BitCard.Color), BitColor.Error);
                builder.AddAttribute(2, nameof(BitCard.Size), BitSize.Small);
                builder.CloseComponent();
            });
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-err"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-sm"));

        // The elevation was not set directly, so the cascaded one still applies.
        Assert.IsTrue(card.ClassList.Contains("bit-crd-e6"));
    }

    [TestMethod]
    public void BitCardCascadedCoverParametersShouldReachTheCover()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitCardParams
            {
                CoverOverlay = true,
                ImageLoading = BitImageLoading.Lazy
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                builder.AddAttribute(1, nameof(BitCard.ImageUrl), "/images/a.png");
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("bit-crd-ovl"));
        Assert.AreEqual("lazy", component.Find(".bit-crd-img").GetAttribute("loading"));
    }

    [TestMethod]
    public void BitCardCascadedCoverRatioShouldReachTheCardAsAClassAndAProperty()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitCardParams { CoverRatio = "4 / 3" }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                builder.AddAttribute(1, nameof(BitCard.ImageUrl), "/images/a.png");
                builder.CloseComponent();
            });
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-cra"));
        Assert.IsTrue(card.GetAttribute("style")!.Contains("--bit-crd-cvr-ratio:4 / 3"));
    }

    [TestMethod]
    public void BitCardCascadedReversedAndImagePositionShouldReachTheCard()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitCardParams { Reversed = true, ImagePosition = "top" }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                builder.AddAttribute(1, nameof(BitCard.ImageUrl), "/img.png");
                builder.CloseComponent();
            });
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-rev"));
        Assert.IsTrue(card.GetAttribute("style")!.Contains("--bit-crd-img-position:top"));
    }

    [TestMethod]
    public void BitCardCascadedDownloadShouldReachTheStretchedLink()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitCardParams { Download = "report.pdf" }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                builder.AddAttribute(1, nameof(BitCard.Href), "/report.pdf");
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("report.pdf", component.Find(".bit-crd-lnk").GetAttribute("download"));
    }

    [TestMethod]
    public void BitCardCascadedScrollableBodyShouldReachTheCard()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitCardParams { ScrollableBody = true }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("bit-crd-scb"));
    }

    [TestMethod]
    public void BitCardCascadedTargetShouldStillProduceTheDefaultRel()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitCardParams { Target = "_blank" }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCard>(0);
                builder.AddAttribute(1, nameof(BitCard.Href), "https://bitplatform.dev");
                builder.CloseComponent();
            });
        });

        var link = component.Find(".bit-crd-lnk");

        Assert.AreEqual("_blank", link.GetAttribute("target"));
        Assert.AreEqual("noopener", link.GetAttribute("rel"));
    }

    #endregion
}
