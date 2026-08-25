using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Shimmer;

[TestClass]
public class BitShimmerTests : BunitTestContext
{
    [TestMethod]
    public void BitShimmerShouldRenderRootElement()
    {
        var component = RenderComponent<BitShimmer>();

        var root = component.Find(".bit-smr");

        Assert.IsNotNull(root);
        Assert.AreEqual("div", root.TagName.ToLower());
    }

    [TestMethod]
    public void BitShimmerShouldRenderTheDefaultPlaceholder()
    {
        var component = RenderComponent<BitShimmer>();

        var wrapper = component.Find(".bit-smr-wrp");
        var shimmer = component.Find(".bit-smr-anm");

        Assert.IsNotNull(wrapper);
        Assert.IsNotNull(shimmer);
        Assert.IsTrue(shimmer.ClassList.Contains("bit-smr-wav"));
    }

    [TestMethod]
    public void BitShimmerShouldRenderTheDefaultIdOnTheRoot()
    {
        var component = RenderComponent<BitShimmer>();

        var root = component.Find(".bit-smr");

        Assert.AreEqual(component.Instance.UniqueId, root.GetAttribute("id"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectId()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Id, "custom-id"));

        Assert.AreEqual("custom-id", component.Find(".bit-smr").GetAttribute("id"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.AriaLabel, "Loading content"));

        Assert.AreEqual("Loading content", component.Find(".bit-smr").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitShimmerHtmlAttributesTest>();

        Assert.AreEqual("bit", component.Find(".bit-smr").GetAttribute("data-val-test"));
        Assert.AreEqual("test-shimmer", component.Find(".bit-smr").GetAttribute("id"));
    }

    [TestMethod]
    public void BitShimmerShouldRenderTheExpectedDefaultMarkup()
    {
        var component = RenderComponent<BitShimmerHtmlAttributesTest>();

        component.MarkupMatches(@"
<div data-val-test=""bit"" id=""test-shimmer"" class=""bit-smr bit-smr-lin bit-smr-md bit-smr-tbg"" aria-busy=""true"">
    <div class=""bit-smr-wrp bit-smr-bsbg "" aria-hidden=""true"">
        <div style="""" class=""bit-smr-anm bit-smr-wav ""></div>
    </div>
</div>");
    }

    [TestMethod]
    public void BitShimmerShouldRespectIsEnabled()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.IsEnabled, false));

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitShimmerShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Visibility, visibility));

        var style = component.Find(".bit-smr").GetAttribute("style") ?? string.Empty;

        if (expectedStyle.Length > 0)
        {
            Assert.IsTrue(style.Contains(expectedStyle));
        }
        else
        {
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
    }

    [TestMethod]
    public void BitShimmerShouldRespectForceAnimation()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.ForceAnimation, true));

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-fam"));
    }



    // ----------------------------------------------------------------- shape

    [TestMethod]
    public void BitShimmerShouldRespectCircleParameter()
    {
        var compLinear = RenderComponent<BitShimmer>();
        var compCircle = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Circle, true));

        Assert.IsTrue(compLinear.Find(".bit-smr").ClassList.Contains("bit-smr-lin"));
        Assert.IsTrue(compCircle.Find(".bit-smr").ClassList.Contains("bit-smr-crl"));
        Assert.IsFalse(compCircle.Find(".bit-smr").ClassList.Contains("bit-smr-lin"));
    }

    [TestMethod]
    [DataRow(BitShimmerShape.Rounded, "bit-smr-lin")]
    [DataRow(BitShimmerShape.Square, "bit-smr-sqr")]
    [DataRow(BitShimmerShape.Pill, "bit-smr-pil")]
    [DataRow(BitShimmerShape.Circle, "bit-smr-crl")]
    public void BitShimmerShouldRespectShape(BitShimmerShape shape, string expectedClass)
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Shape, shape));

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitShimmerShape.Square)]
    [DataRow(BitShimmerShape.Pill)]
    public void BitShimmerShouldKeepTheLinearBaseClassForRectangularShapes(BitShimmerShape shape)
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Shape, shape));

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-lin"));
    }

    [TestMethod]
    public void BitShimmerShapeShouldWinOverCircle()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Circle, true);
            parameters.Add(p => p.Shape, BitShimmerShape.Pill);
        });

        var root = component.Find(".bit-smr");

        Assert.IsTrue(root.ClassList.Contains("bit-smr-pil"));
        Assert.IsFalse(root.ClassList.Contains("bit-smr-crl"));
    }

    [TestMethod]
    public void BitShimmerShouldChangeTheShapeClassAfterARerender()
    {
        var component = RenderComponent<BitShimmer>();

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-lin"));

        component.Render(parameters => parameters.Add(p => p.Shape, BitShimmerShape.Circle));

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-crl"));
        Assert.IsFalse(component.Find(".bit-smr").ClassList.Contains("bit-smr-lin"));
    }



    // ----------------------------------------------------------------- size

    [TestMethod]
    [DataRow(BitSize.Small, "bit-smr-sm")]
    [DataRow(BitSize.Medium, "bit-smr-md")]
    [DataRow(BitSize.Large, "bit-smr-lg")]
    [DataRow(null, "bit-smr-md")]
    public void BitShimmerShouldRespectSize(BitSize? size, string expectedClass)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            if (size.HasValue) parameters.Add(p => p.Size, size.Value);
        });

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitShimmerShouldChangeTheSizeAndLayoutClassesAfterARerender()
    {
        var component = RenderComponent<BitShimmer>();

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-md"));
        Assert.IsFalse(component.Find(".bit-smr").ClassList.Contains("bit-smr-inl"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
            parameters.Add(p => p.Inline, true);
        });

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-lg"));
        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-inl"));
        Assert.IsFalse(component.Find(".bit-smr").ClassList.Contains("bit-smr-md"));
    }

    [TestMethod]
    public void BitShimmerShouldChangeTheSizingCustomPropertiesAfterARerender()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Height, "1rem"));

        Assert.IsTrue(component.Find(".bit-smr").GetAttribute("style").Contains("--bit-smr-hgt:1rem"));

        component.Render(parameters => parameters.Add(p => p.Height, "3rem"));

        Assert.IsTrue(component.Find(".bit-smr").GetAttribute("style").Contains("--bit-smr-hgt:3rem"));
    }

    [TestMethod]
    [DataRow("5rem", null)]
    [DataRow(null, "10rem")]
    [DataRow("3rem", "8rem")]
    public void BitShimmerShouldRespectWidthAndHeight(string height, string width)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            if (height is not null) parameters.Add(p => p.Height, height);
            if (width is not null) parameters.Add(p => p.Width, width);
        });

        var style = component.Find(".bit-smr").GetAttribute("style");

        if (height is not null)
        {
            // The height sizes the placeholder rather than the root, so it is published as a custom property
            // that the line (or each of the lines) reads.
            Assert.IsTrue(style.Contains($"--bit-smr-hgt:{height}"));
        }

        if (width is not null)
        {
            Assert.IsTrue(style.Contains($"width:{width}"));
        }
    }

    [TestMethod]
    public void BitShimmerShouldNotRenderSizingCustomPropertiesWhenTheyAreNotSet()
    {
        var component = RenderComponent<BitShimmer>();

        var style = component.Find(".bit-smr").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-smr-hgt"));
        Assert.IsFalse(style.Contains("--bit-smr-gap"));
        Assert.IsFalse(style.Contains("--bit-smr-llw"));
        Assert.IsFalse(style.Contains("--bit-smr-dly"));
    }



    // ----------------------------------------------------------------- lines

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(3, 3)]
    [DataRow(7, 7)]
    [DataRow(0, 1)]
    [DataRow(-5, 1)]
    public void BitShimmerShouldRenderOneWrapperPerLine(int lines, int expectedCount)
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Lines, lines));

        Assert.AreEqual(expectedCount, component.FindAll(".bit-smr-wrp").Count);
        Assert.AreEqual(expectedCount, component.FindAll(".bit-smr-anm").Count);
    }

    [TestMethod]
    [DataRow(1, false)]
    [DataRow(2, true)]
    public void BitShimmerShouldRespectTheMultiLineClass(int lines, bool expected)
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Lines, lines));

        Assert.AreEqual(expected, component.Find(".bit-smr").ClassList.Contains("bit-smr-mln"));
    }

    [TestMethod]
    public void BitShimmerCircleShouldIgnoreLines()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Circle, true);
            parameters.Add(p => p.Lines, 4);
        });

        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);
        Assert.IsFalse(component.Find(".bit-smr").ClassList.Contains("bit-smr-mln"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectGapAndLastLineWidth()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.Gap, "1rem");
            parameters.Add(p => p.LastLineWidth, "35%");
        });

        var style = component.Find(".bit-smr").GetAttribute("style");

        Assert.IsTrue(style.Contains("--bit-smr-gap:1rem"));
        Assert.IsTrue(style.Contains("--bit-smr-llw:35%"));
    }

    [TestMethod]
    public void BitShimmerShouldChangeTheLineCountAfterARerender()
    {
        var component = RenderComponent<BitShimmer>();

        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);

        component.Render(parameters => parameters.Add(p => p.Lines, 4));

        Assert.AreEqual(4, component.FindAll(".bit-smr-wrp").Count);
        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-mln"));
    }



    // ----------------------------------------------------------------- animation

    [TestMethod]
    [DataRow(BitShimmerAnimation.Wave, "bit-smr-wav")]
    [DataRow(BitShimmerAnimation.Pulse, "bit-smr-pul")]
    [DataRow(BitShimmerAnimation.Fade, "bit-smr-fad")]
    [DataRow(BitShimmerAnimation.None, "bit-smr-non")]
    [DataRow(null, "bit-smr-wav")]
    public void BitShimmerShouldRespectAnimation(BitShimmerAnimation? animation, string expectedClass)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            if (animation.HasValue) parameters.Add(p => p.Animation, animation.Value);
        });

        Assert.IsTrue(component.Find(".bit-smr-anm").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitShimmerShouldRespectPulseParameter()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Pulse, true));

        Assert.IsTrue(component.Find(".bit-smr-anm").ClassList.Contains("bit-smr-pul"));
    }

    [TestMethod]
    public void BitShimmerAnimationShouldWinOverPulse()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Pulse, true);
            parameters.Add(p => p.Animation, BitShimmerAnimation.Fade);
        });

        var shimmer = component.Find(".bit-smr-anm");

        Assert.IsTrue(shimmer.ClassList.Contains("bit-smr-fad"));
        Assert.IsFalse(shimmer.ClassList.Contains("bit-smr-pul"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectDurationAndDelay()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Duration, 3000);
            parameters.Add(p => p.Delay, 500);
        });

        var style = component.Find(".bit-smr-anm").GetAttribute("style");

        Assert.IsTrue(style.Contains("animation-duration:3000ms"));
        Assert.IsTrue(style.Contains("animation-delay:500ms"));
    }

    [TestMethod]
    public void BitShimmerShouldNotTimeAnAnimationItDoesNotPlay()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Animation, BitShimmerAnimation.None);
            parameters.Add(p => p.Duration, 3000);
            parameters.Add(p => p.Delay, 500);
        });

        var style = component.Find(".bit-smr-anm").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("animation-duration"));
        Assert.IsFalse(style.Contains("animation-delay"));
    }

    [TestMethod]
    public void BitShimmerShouldSpliceTheAnimationStyleAfterACustomShimmerStyle()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Styles, new BitShimmerClassStyles { Shimmer = "background:tomato" });
            parameters.Add(p => p.Duration, 1200);
        });

        // Without the semicolon between them the custom declaration would swallow the animation one.
        Assert.AreEqual("background:tomato;animation-duration:1200ms", component.Find(".bit-smr-anm").GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectDelayOnItsOwn()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Delay, 250));

        Assert.AreEqual("animation-delay:250ms", component.Find(".bit-smr-anm").GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectShowDelay()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.ShowDelay, 750));

        Assert.IsTrue(component.Find(".bit-smr").GetAttribute("style").Contains("--bit-smr-dly:750ms"));
    }



    // ----------------------------------------------------------------- inline

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitShimmerShouldRespectInline(bool inline)
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Inline, inline));

        Assert.AreEqual(inline, component.Find(".bit-smr").ClassList.Contains("bit-smr-inl"));
    }



    // ----------------------------------------------------------------- loaded

    [TestMethod]
    public void BitShimmerShouldShowChildContentWhenLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Loaded, true);
            parameters.AddChildContent("Loaded content");
        });

        Assert.IsTrue(component.Markup.Contains("Loaded content"));
        Assert.AreEqual(0, component.FindAll(".bit-smr-wrp").Count);
        Assert.IsNotNull(component.Find(".bit-smr-cnt"));
    }

    [TestMethod]
    public void BitShimmerShouldNotShowChildContentWhileItIsNotLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.AddChildContent("Loaded content"));

        Assert.IsFalse(component.Markup.Contains("Loaded content"));
        Assert.AreEqual(0, component.FindAll(".bit-smr-cnt").Count);
    }

    [TestMethod]
    public void BitShimmerShouldRespectTheContentAlias()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Loaded, true);
            parameters.Add(p => p.Content, (RenderFragment)(builder => builder.AddContent(0, "Aliased content")));
        });

        Assert.IsTrue(component.Markup.Contains("Aliased content"));
    }

    [TestMethod]
    public void BitShimmerChildContentShouldWinOverContent()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Loaded, true);
            parameters.Add(p => p.Content, (RenderFragment)(builder => builder.AddContent(0, "Aliased content")));
            parameters.AddChildContent("Child content");
        });

        Assert.IsTrue(component.Markup.Contains("Child content"));
        Assert.IsFalse(component.Markup.Contains("Aliased content"));
    }

    [TestMethod]
    public void BitShimmerShouldDropThePlaceholderSizingOnceItIsLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Height, "5rem");
            parameters.Add(p => p.Lines, 3);
            parameters.AddChildContent("Loaded content");
        });

        var root = component.Find(".bit-smr");

        Assert.IsTrue(root.ClassList.Contains("bit-smr-lin"));
        Assert.IsTrue(root.ClassList.Contains("bit-smr-mln"));

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        root = component.Find(".bit-smr");

        // The classes that read the height are gone, so the content is laid out by the page rather than by
        // the box that was standing in for it.
        Assert.IsTrue(root.ClassList.Contains("bit-smr-ldd"));
        Assert.IsFalse(root.ClassList.Contains("bit-smr-lin"));
        Assert.IsFalse(root.ClassList.Contains("bit-smr-mln"));
    }

    [TestMethod]
    public void BitShimmerShouldKeepTheWidthAfterTheSwap()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Width, "15rem");
            parameters.Add(p => p.Loaded, true);
            parameters.AddChildContent("Loaded content");
        });

        // A placeholder and the content that replaces it occupy the same column, so the width stays.
        Assert.IsTrue(component.Find(".bit-smr").GetAttribute("style").Contains("width:15rem"));
    }

    [TestMethod]
    public void BitShimmerShouldNotRenderAnyLineOnceItIsLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 5);
            parameters.Add(p => p.Loaded, true);
            parameters.AddChildContent("Loaded content");
        });

        Assert.AreEqual(0, component.FindAll(".bit-smr-wrp").Count);
        Assert.AreEqual(0, component.FindAll(".bit-smr-anm").Count);
    }

    [TestMethod]
    public void BitShimmerShouldSwapBetweenThePlaceholderAndTheContent()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.AddChildContent("Loaded content"));

        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        Assert.AreEqual(0, component.FindAll(".bit-smr-wrp").Count);
        Assert.AreEqual(1, component.FindAll(".bit-smr-cnt").Count);

        component.Render(parameters => parameters.Add(p => p.Loaded, false));

        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);
        Assert.AreEqual(0, component.FindAll(".bit-smr-cnt").Count);
    }

    [TestMethod]
    [DataRow(false, "true")]
    [DataRow(true, null)]
    public void BitShimmerShouldRespectAriaBusy(bool loaded, string expectedAriaBusy)
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Loaded, loaded));

        Assert.AreEqual(expectedAriaBusy, component.Find(".bit-smr").GetAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitShimmerShouldHideThePlaceholderFromAssistiveTechnologies()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Lines, 3));

        foreach (var wrapper in component.FindAll(".bit-smr-wrp"))
        {
            Assert.AreEqual("true", wrapper.GetAttribute("aria-hidden"));
        }
    }



    // ----------------------------------------------------------------- template

    [TestMethod]
    public void BitShimmerShouldRenderTheTemplateInsteadOfThePlaceholder()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Template, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-template");
                builder.CloseElement();
            }));
        });

        Assert.IsNotNull(component.Find(".custom-template"));
        Assert.AreEqual(0, component.FindAll(".bit-smr-wrp").Count);
        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-tpl"));
    }

    [TestMethod]
    public void BitShimmerTemplateShouldNotBeShapedOrStackedLikeTheDefaultPlaceholder()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Shape, BitShimmerShape.Circle);
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.Template, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-template");
                builder.CloseElement();
            }));
        });

        var root = component.Find(".bit-smr");

        // The template brings boxes of its own, so a height meant for a single bar must not crop it.
        Assert.IsTrue(root.ClassList.Contains("bit-smr-tpl"));
        Assert.IsFalse(root.ClassList.Contains("bit-smr-crl"));
        Assert.IsFalse(root.ClassList.Contains("bit-smr-lin"));
        Assert.IsFalse(root.ClassList.Contains("bit-smr-mln"));
        Assert.AreEqual(1, component.FindAll(".custom-template").Count);
    }

    [TestMethod]
    public void BitShimmerShouldNotRenderTheTemplateOnceItIsLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Loaded, true);
            parameters.Add(p => p.Template, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-template");
                builder.CloseElement();
            }));
            parameters.AddChildContent("Loaded content");
        });

        Assert.AreEqual(0, component.FindAll(".custom-template").Count);
        Assert.IsFalse(component.Find(".bit-smr").ClassList.Contains("bit-smr-tpl"));
        Assert.IsTrue(component.Markup.Contains("Loaded content"));
    }



    // ----------------------------------------------------------------- colors

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-smr-pri")]
    [DataRow(BitColor.Secondary, "bit-smr-sec")]
    [DataRow(BitColor.Tertiary, "bit-smr-ter")]
    [DataRow(BitColor.Info, "bit-smr-inf")]
    [DataRow(BitColor.Success, "bit-smr-suc")]
    [DataRow(BitColor.Warning, "bit-smr-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-smr-swr")]
    [DataRow(BitColor.Error, "bit-smr-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-smr-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-smr-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-smr-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-smr-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-smr-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-smr-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-smr-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-smr-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-smr-tbr")]
    [DataRow(null, "bit-smr-tbg")]
    public void BitShimmerShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            if (color.HasValue) parameters.Add(p => p.Color, color.Value);
        });

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-smr-bpri")]
    [DataRow(BitColor.Secondary, "bit-smr-bsec")]
    [DataRow(BitColor.Tertiary, "bit-smr-bter")]
    [DataRow(BitColor.Info, "bit-smr-binf")]
    [DataRow(BitColor.Success, "bit-smr-bsuc")]
    [DataRow(BitColor.Warning, "bit-smr-bwrn")]
    [DataRow(BitColor.SevereWarning, "bit-smr-bswr")]
    [DataRow(BitColor.Error, "bit-smr-berr")]
    [DataRow(BitColor.PrimaryBackground, "bit-smr-bpbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-smr-bsbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-smr-btbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-smr-bpfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-smr-bsfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-smr-btfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-smr-bpbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-smr-bsbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-smr-btbr")]
    [DataRow(null, "bit-smr-bsbg")]
    public void BitShimmerShouldRespectBackground(BitColor? background, string expectedClass)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            if (background.HasValue) parameters.Add(p => p.Background, background.Value);
        });

        Assert.IsTrue(component.Find(".bit-smr-wrp").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitShimmerShouldRespectBackgroundOnEveryLine()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.Background, BitColor.Error);
        });

        var wrappers = component.FindAll(".bit-smr-wrp");

        Assert.AreEqual(3, wrappers.Count);
        foreach (var wrapper in wrappers)
        {
            Assert.IsTrue(wrapper.ClassList.Contains("bit-smr-berr"));
        }
    }



    // ----------------------------------------------------------------- live region

    [TestMethod]
    public void BitShimmerShouldNotRenderALiveRegionWithNothingToSay()
    {
        var component = RenderComponent<BitShimmer>();

        Assert.AreEqual(0, component.FindAll(".bit-smr-vhd").Count);
    }

    [TestMethod]
    public void BitShimmerShouldRenderTheLabelInALiveRegion()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Label, "Loading your profile"));

        var region = component.Find(".bit-smr-vhd");

        Assert.AreEqual("status", region.GetAttribute("role"));
        Assert.AreEqual("polite", region.GetAttribute("aria-live"));
        Assert.AreEqual("true", region.GetAttribute("aria-atomic"));
        Assert.AreEqual("Loading your profile", region.TextContent);
    }

    [TestMethod]
    public void BitShimmerShouldSwapTheLabelForTheLoadedLabel()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading your profile");
            parameters.Add(p => p.LoadedLabel, "Profile loaded");
            parameters.AddChildContent("Xafan Salina");
        });

        Assert.AreEqual("Loading your profile", component.Find(".bit-smr-vhd").TextContent);

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        // The region stays on the page across the swap, which is what makes the change announceable.
        Assert.AreEqual("Profile loaded", component.Find(".bit-smr-vhd").TextContent);
    }

    [TestMethod]
    public void BitShimmerShouldRenderTheLiveRegionForALoadedLabelAlone()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.LoadedLabel, "Profile loaded"));

        Assert.AreEqual(1, component.FindAll(".bit-smr-vhd").Count);
        Assert.AreEqual(string.Empty, component.Find(".bit-smr-vhd").TextContent);
    }

    [TestMethod]
    [DataRow(BitPoliteness.Off, "off", null)]
    [DataRow(BitPoliteness.Polite, "polite", "status")]
    [DataRow(BitPoliteness.Assertive, "assertive", "status")]
    public void BitShimmerShouldRespectPoliteness(BitPoliteness politeness, string expectedAriaLive, string expectedRole)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading");
            parameters.Add(p => p.Politeness, politeness);
        });

        var region = component.Find(".bit-smr-vhd");

        Assert.AreEqual(expectedAriaLive, region.GetAttribute("aria-live"));

        // The status role carries a politeness of its own, so a region asked to stay silent drops it.
        Assert.AreEqual(expectedRole, region.GetAttribute("role"));
    }



    // ----------------------------------------------------------------- classes & styles

    [TestMethod]
    public void BitShimmerShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitShimmerClassStyles { Root = "custom-root", Shimmer = "custom-shimmer", ShimmerWrapper = "custom-wrapper" });
            parameters.Add(p => p.Styles, new BitShimmerClassStyles { Root = "background:tomato;", ShimmerWrapper = "background-color: darkgoldenrod;" });
            parameters.AddChildContent("Content");
        });

        var markup = component.Markup;

        Assert.IsTrue(markup.Contains("custom-root"));
        Assert.IsTrue(markup.Contains("custom-shimmer"));
        Assert.IsTrue(markup.Contains("custom-wrapper"));
        Assert.IsTrue(markup.Contains("background:tomato"));
        Assert.IsTrue(markup.Contains("darkgoldenrod"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Style, "border:2px solid gray");
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Width, "10rem");
        });

        var root = component.Find(".bit-smr");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.GetAttribute("style").Contains("border:2px solid gray"));
        Assert.IsTrue(root.GetAttribute("style").Contains("width:10rem"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectTheContentClassAndStyle()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Loaded, true);
            parameters.Add(p => p.Classes, new BitShimmerClassStyles { Content = "custom-content" });
            parameters.Add(p => p.Styles, new BitShimmerClassStyles { Content = "color:tomato" });
            parameters.AddChildContent("Content");
        });

        var content = component.Find(".bit-smr-cnt");

        Assert.IsTrue(content.ClassList.Contains("custom-content"));
        Assert.AreEqual("color:tomato", content.GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldRespectTheLabelClassAndStyle()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading");
            parameters.Add(p => p.Classes, new BitShimmerClassStyles { Label = "custom-label" });
            parameters.Add(p => p.Styles, new BitShimmerClassStyles { Label = "color:tomato" });
        });

        var region = component.Find(".bit-smr-vhd");

        Assert.IsTrue(region.ClassList.Contains("custom-label"));
        Assert.AreEqual("color:tomato", region.GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldApplyTheLineClassesToEveryLine()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.Classes, new BitShimmerClassStyles { Shimmer = "custom-shimmer", ShimmerWrapper = "custom-wrapper" });
        });

        Assert.AreEqual(3, component.FindAll(".custom-wrapper").Count);
        Assert.AreEqual(3, component.FindAll(".custom-shimmer").Count);
    }



    // ----------------------------------------------------------------- direction

    [TestMethod]
    public void BitShimmerShouldRespectDir()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Dir, BitDir.Rtl));

        var root = component.Find(".bit-smr");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }
}
