using System;
using System.Threading;
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


    // ----------------------------------------------------------------- radius

    [TestMethod]
    public void BitShimmerShouldRespectRadius()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Radius, "0.75rem"));

        Assert.IsTrue(component.Find(".bit-smr").GetAttribute("style").Contains("--bit-smr-rad:0.75rem"));
    }

    [TestMethod]
    public void BitShimmerRadiusShouldBePublishedOverTheShapeThatCarriesOneOfItsOwn()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Shape, BitShimmerShape.Pill);
            parameters.Add(p => p.Radius, "0");
        });

        var root = component.Find(".bit-smr");

        // The shape still names the corner it would draw; the radius lands on the style attribute, which is
        // where it wins over the class that names it.
        Assert.IsTrue(root.ClassList.Contains("bit-smr-pil"));
        Assert.IsTrue(root.GetAttribute("style").Contains("--bit-smr-rad:0"));
    }

    [TestMethod]
    public void BitShimmerShouldChangeTheRadiusAfterARerender()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Radius, "0.25rem"));

        Assert.IsTrue(component.Find(".bit-smr").GetAttribute("style").Contains("--bit-smr-rad:0.25rem"));

        component.Render(parameters => parameters.Add(p => p.Radius, "1rem"));

        var style = component.Find(".bit-smr").GetAttribute("style");

        Assert.IsTrue(style.Contains("--bit-smr-rad:1rem"));
        Assert.IsFalse(style.Contains("--bit-smr-rad:0.25rem"));
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
        Assert.IsFalse(component.Find(".bit-smr").GetAttribute("style").Contains("--bit-smr-hgt:1rem"));
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



    // ----------------------------------------------------------------- line widths

    [TestMethod]
    public void BitShimmerShouldGiveEveryLineAMeasureOfItsOwn()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 4);
            parameters.Add(p => p.LineWidths, ["100%", "88%", "94%", "52%"]);
        });

        var wrappers = component.FindAll(".bit-smr-wrp");

        Assert.AreEqual(4, wrappers.Count);
        Assert.AreEqual("width:100%", wrappers[0].GetAttribute("style"));
        Assert.AreEqual("width:88%", wrappers[1].GetAttribute("style"));
        Assert.AreEqual("width:94%", wrappers[2].GetAttribute("style"));
        Assert.AreEqual("width:52%", wrappers[3].GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldTreatAShortLineWidthListAsAPrefix()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 4);
            parameters.Add(p => p.LineWidths, ["70%", "40%"]);
        });

        var wrappers = component.FindAll(".bit-smr-wrp");

        // A line the list does not reach keeps the width it would have had anyway - the full measure, or the
        // shortened last one the stylesheet draws - so a short list varies the lines it names and no others.
        Assert.AreEqual("width:70%", wrappers[0].GetAttribute("style"));
        Assert.AreEqual("width:40%", wrappers[1].GetAttribute("style"));
        Assert.IsNull(wrappers[2].GetAttribute("style"));
        Assert.IsNull(wrappers[3].GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldSkipTheEmptyEntriesOfALineWidthList()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.LineWidths, ["70%", "", "45%"]);
        });

        var wrappers = component.FindAll(".bit-smr-wrp");

        // An entry with nothing in it names no measure, so the line it stands for is left as it was rather
        // than being given a `width:` with no length after it.
        Assert.AreEqual("width:70%", wrappers[0].GetAttribute("style"));
        Assert.IsNull(wrappers[1].GetAttribute("style"));
        Assert.AreEqual("width:45%", wrappers[2].GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldNotGiveASingleLineAMeasureFromTheLineWidthList()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.LineWidths, ["70%"]));

        // A single bar is sized by Width; only a stack has lines to give a width of their own.
        Assert.IsNull(component.Find(".bit-smr-wrp").GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerCircleShouldIgnoreTheLineWidthList()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Circle, true);
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.LineWidths, ["70%", "40%", "20%"]);
        });

        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);
        Assert.IsNull(component.Find(".bit-smr-wrp").GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerOverlayShouldIgnoreTheLineWidthList()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Overlay, true);
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.LineWidths, ["70%", "40%", "20%"]);
            parameters.AddChildContent("Covered content");
        });

        // A cover is one box over the whole of the content, so there are no lines for the list to measure.
        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);
        Assert.IsNull(component.Find(".bit-smr-wrp").GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldSpliceTheLineWidthAfterACustomWrapperStyle()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 2);
            parameters.Add(p => p.LineWidths, ["70%"]);
            parameters.Add(p => p.Styles, new BitShimmerClassStyles { ShimmerWrapper = "background:tomato" });
        });

        var wrappers = component.FindAll(".bit-smr-wrp");

        // Without the semicolon between them the custom declaration would swallow the width.
        Assert.AreEqual("background:tomato;width:70%", wrappers[0].GetAttribute("style"));
        Assert.AreEqual("background:tomato", wrappers[1].GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldChangeTheLineWidthsAfterARerender()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 2);
            parameters.Add(p => p.LineWidths, ["70%", "40%"]);
        });

        Assert.AreEqual("width:70%", component.FindAll(".bit-smr-wrp")[0].GetAttribute("style"));

        component.Render(parameters => parameters.Add(p => p.LineWidths, ["30%", "20%"]));

        Assert.AreEqual("width:30%", component.FindAll(".bit-smr-wrp")[0].GetAttribute("style"));
        Assert.AreEqual("width:20%", component.FindAll(".bit-smr-wrp")[1].GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldNotRenderALineWidthOnALoadedShimmer()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.LineWidths, ["70%", "40%", "20%"]);
            parameters.Add(p => p.Loaded, true);
            parameters.AddChildContent("Loaded content");
        });

        Assert.AreEqual(0, component.FindAll(".bit-smr-wrp").Count);
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


    [TestMethod]
    public void BitShimmerShouldNotTimeAnAnimationWithANegativeNumberOfMilliseconds()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Duration, -3000);
            parameters.Add(p => p.Delay, -500);
        });

        var style = component.Find(".bit-smr-anm").GetAttribute("style");

        // A negative duration is not a duration a browser accepts, and a negative delay would start the loop
        // part-way through a sweep nobody asked to skip; both are clamped rather than published as they are.
        Assert.AreEqual("animation-delay:0ms;animation-duration:0ms", style);
    }

    [TestMethod]
    public void BitShimmerShouldNotWaitForANegativeShowDelay()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.ShowDelay, -750));

        Assert.IsTrue(component.Find(".bit-smr").GetAttribute("style").Contains("--bit-smr-dly:0ms"));
    }


    // ----------------------------------------------------------------- stagger

    [TestMethod]
    public void BitShimmerShouldStaggerTheLinesOfAStack()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 4);
            parameters.Add(p => p.Stagger, 200);
        });

        var shimmers = component.FindAll(".bit-smr-anm");

        // Line n starts at n * Stagger, so the stack reads as a paragraph arriving line by line rather than
        // as one block breathing.
        Assert.AreEqual("animation-delay:0ms", shimmers[0].GetAttribute("style"));
        Assert.AreEqual("animation-delay:200ms", shimmers[1].GetAttribute("style"));
        Assert.AreEqual("animation-delay:400ms", shimmers[2].GetAttribute("style"));
        Assert.AreEqual("animation-delay:600ms", shimmers[3].GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldAddTheStaggerToTheDelayRatherThanReplacingIt()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.Delay, 500);
            parameters.Add(p => p.Stagger, 100);
        });

        var shimmers = component.FindAll(".bit-smr-anm");

        Assert.AreEqual("animation-delay:500ms", shimmers[0].GetAttribute("style"));
        Assert.AreEqual("animation-delay:600ms", shimmers[1].GetAttribute("style"));
        Assert.AreEqual("animation-delay:700ms", shimmers[2].GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldKeepTheDurationOnEveryStaggeredLine()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 2);
            parameters.Add(p => p.Duration, 2000);
            parameters.Add(p => p.Stagger, 250);
        });

        var shimmers = component.FindAll(".bit-smr-anm");

        Assert.AreEqual("animation-delay:0ms;animation-duration:2000ms", shimmers[0].GetAttribute("style"));
        Assert.AreEqual("animation-delay:250ms;animation-duration:2000ms", shimmers[1].GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldNotStaggerASinglePlaceholderAgainstItself()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Stagger, 200));

        // There is nothing to offset one line against, which is what keeps a lone placeholder on the pace the
        // stylesheet gives it rather than on an explicit start of its own.
        Assert.AreEqual(string.Empty, component.Find(".bit-smr-anm").GetAttribute("style"));
    }

    [TestMethod]
    public void BitShimmerShouldNotStaggerAnAnimationItDoesNotPlay()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.Animation, BitShimmerAnimation.None);
            parameters.Add(p => p.Stagger, 200);
        });

        foreach (var shimmer in component.FindAll(".bit-smr-anm"))
        {
            Assert.AreEqual(string.Empty, shimmer.GetAttribute("style"));
        }
    }

    [TestMethod]
    public void BitShimmerShouldNotStaggerLinesByANegativeNumberOfMilliseconds()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 3);
            parameters.Add(p => p.Stagger, -200);
        });

        foreach (var shimmer in component.FindAll(".bit-smr-anm"))
        {
            Assert.AreEqual("animation-delay:0ms", shimmer.GetAttribute("style"));
        }
    }

    [TestMethod]
    public void BitShimmerShouldChangeTheStaggerAfterARerender()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Lines, 2);
            parameters.Add(p => p.Stagger, 100);
        });

        Assert.AreEqual("animation-delay:100ms", component.FindAll(".bit-smr-anm")[1].GetAttribute("style"));

        component.Render(parameters => parameters.Add(p => p.Stagger, 400));

        Assert.AreEqual("animation-delay:400ms", component.FindAll(".bit-smr-anm")[1].GetAttribute("style"));
    }


    // ----------------------------------------------------------------- min show time

    [TestMethod]
    public void BitShimmerShouldNotHoldBackAPlaceholderThatWasNeverShown()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.ShowDelay, 10_000);
            parameters.Add(p => p.MinShowTime, 10_000);
            parameters.AddChildContent("Loaded content");
        });

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        // The response beat the delay that was holding the placeholder back, so there was never a placeholder
        // for a shortest life to apply to.
        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-ldd"));
        Assert.AreEqual(0, component.FindAll(".bit-smr-wrp").Count);
    }

    [TestMethod]
    public void BitShimmerShouldNotHoldBackAnythingWithoutAMinShowTime()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.AddChildContent("Loaded content"));

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-ldd"));
    }

    [TestMethod]
    public void BitShimmerShouldNotHoldBackAShimmerThatStartsOutLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.MinShowTime, 10_000);
            parameters.Add(p => p.Loaded, true);
            parameters.AddChildContent("Loaded content");
        });

        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-ldd"));
    }

    [TestMethod]
    public void BitShimmerShouldKeepThePlaceholderForItsMinShowTime()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.MinShowTime, 1000);
            parameters.AddChildContent("Loaded content");
        });

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        // The response landed while the placeholder was still living out its shortest life, so the swap waits.
        Assert.IsFalse(component.Find(".bit-smr").ClassList.Contains("bit-smr-ldd"));
        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);

        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-ldd")),
                                   TimeSpan.FromSeconds(5));

        Assert.AreEqual(0, component.FindAll(".bit-smr-wrp").Count);
        Assert.IsTrue(component.Find(".bit-smr-cnt").TextContent.Contains("Loaded content"));
    }

    [TestMethod]
    public void BitShimmerShouldSayThatItIsStillBusyWhileTheSwapIsHeldBack()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.MinShowTime, 1000);
            parameters.Add(p => p.Label, "Loading");
            parameters.Add(p => p.LoadedLabel, "Loaded");
            parameters.AddChildContent("Loaded content");
        });

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        // What the shimmer reports is what it is showing, so the live region and aria-busy are held back with
        // the swap rather than announcing a wait that is still on the page as over.
        Assert.AreEqual("true", component.Find(".bit-smr").GetAttribute("aria-busy"));
        Assert.AreEqual("Loading", component.Find(".bit-smr-vhd").TextContent);

        component.WaitForAssertion(() => Assert.AreEqual("Loaded", component.Find(".bit-smr-vhd").TextContent),
                                   TimeSpan.FromSeconds(5));

        Assert.IsNull(component.Find(".bit-smr").GetAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitShimmerShouldDropAHeldBackSwapThatIsTakenBackBeforeItLands()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.MinShowTime, 1000);
            parameters.AddChildContent("Loaded content");
        });

        component.Render(parameters => parameters.Add(p => p.Loaded, true));
        component.Render(parameters => parameters.Add(p => p.Loaded, false));

        // The response was taken back while the shortest life was still running, so the placeholder is what
        // the shimmer should still be showing when the hold would have expired.
        Thread.Sleep(1400);

        Assert.IsFalse(component.Find(".bit-smr").ClassList.Contains("bit-smr-ldd"));
        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);
    }

    [TestMethod]
    public void BitShimmerShouldStopHoldingThePlaceholderOnceItHasLivedItsMinShowTimeOut()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.MinShowTime, 100);
            parameters.AddChildContent("Loaded content");
        });

        Thread.Sleep(300);

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        // The placeholder outlived its shortest life before the response even landed, so nothing is held back.
        Assert.IsTrue(component.Find(".bit-smr").ClassList.Contains("bit-smr-ldd"));
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

    [TestMethod]
    [DataRow(true, "span")]
    [DataRow(false, "div")]
    public void BitShimmerShouldRenderARootThatBelongsWhereItIsPut(bool inline, string expectedTag)
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.Inline, inline));

        // A placeholder standing in a line of text is phrasing content: a div there would be closed out of the
        // paragraph around it by the HTML parser, taking the sentence apart.
        Assert.AreEqual(expectedTag, component.Find(".bit-smr").TagName.ToLower());
    }

    [TestMethod]
    public void BitShimmerShouldKeepEverythingButTheTagAcrossTheTwoRoots()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
            parameters.Add(p => p.Id, "inline-shimmer");
            parameters.Add(p => p.AriaLabel, "Loading the price");
            parameters.Add(p => p.Width, "4rem");
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-smr");

        Assert.AreEqual("inline-shimmer", root.GetAttribute("id"));
        Assert.AreEqual("Loading the price", root.GetAttribute("aria-label"));
        Assert.AreEqual("true", root.GetAttribute("aria-busy"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.GetAttribute("style").Contains("width:4rem"));
        Assert.IsNotNull(component.Find(".bit-smr-wrp"));
        Assert.IsNotNull(component.Find(".bit-smr-anm"));
    }

    [TestMethod]
    public void BitShimmerShouldChangeTheRootTagAfterARerender()
    {
        var component = RenderComponent<BitShimmer>();

        Assert.AreEqual("div", component.Find(".bit-smr").TagName.ToLower());

        component.Render(parameters => parameters.Add(p => p.Inline, true));

        Assert.AreEqual("span", component.Find(".bit-smr").TagName.ToLower());
    }

    [TestMethod]
    [DataRow(false, "div")]
    [DataRow(true, "span")]
    public void BitShimmerShouldRenderTheWholePlaceholderAsPhrasingContentWhileItIsInline(bool inline, string expectedTag)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Inline, inline);
            parameters.Add(p => p.Lines, 2);
        });

        Assert.AreEqual(expectedTag, component.Find(".bit-smr-wrp").TagName.ToLower());
        Assert.AreEqual(expectedTag, component.Find(".bit-smr-anm").TagName.ToLower());
    }

    [TestMethod]
    [DataRow(false, "div")]
    [DataRow(true, "span")]
    public void BitShimmerShouldRenderTheContentBoxAsPhrasingContentWhileItIsInline(bool inline, string expectedTag)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Inline, inline);
            parameters.Add(p => p.Loaded, true);
            parameters.AddChildContent("Loaded content");
        });

        Assert.AreEqual(expectedTag, component.Find(".bit-smr-cnt").TagName.ToLower());
    }

    [TestMethod]
    [DataRow(false, "div")]
    [DataRow(true, "span")]
    public void BitShimmerShouldRenderTheCoveredContentAsPhrasingContentWhileItIsInline(bool inline, string expectedTag)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Inline, inline);
            parameters.Add(p => p.Overlay, true);
            parameters.AddChildContent("Covered content");
        });

        Assert.AreEqual(expectedTag, component.Find(".bit-smr-cvd").TagName.ToLower());
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
    public void BitShimmerShouldStopPublishingThePlaceholderSizingOnceItIsLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Height, "5rem");
            parameters.Add(p => p.Gap, "1rem");
            parameters.Add(p => p.LastLineWidth, "35%");
            parameters.Add(p => p.Radius, "0.5rem");
            parameters.Add(p => p.ShowDelay, 750);
            parameters.AddChildContent("Loaded content");
        });

        var style = component.Find(".bit-smr").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-smr-hgt:5rem"));
        Assert.IsTrue(style.Contains("--bit-smr-gap:1rem"));
        Assert.IsTrue(style.Contains("--bit-smr-llw:35%"));
        Assert.IsTrue(style.Contains("--bit-smr-rad:0.5rem"));
        Assert.IsTrue(style.Contains("--bit-smr-dly:750ms"));

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        style = component.Find(".bit-smr").GetAttribute("style") ?? string.Empty;

        // The properties are inherited, so leaving them on a loaded shimmer would hand the sizing of a
        // placeholder that is gone to every shimmer the content it made room for happens to contain.
        Assert.IsFalse(style.Contains("--bit-smr-hgt"));
        Assert.IsFalse(style.Contains("--bit-smr-gap"));
        Assert.IsFalse(style.Contains("--bit-smr-llw"));
        Assert.IsFalse(style.Contains("--bit-smr-rad"));
        Assert.IsFalse(style.Contains("--bit-smr-dly"));
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


    // ----------------------------------------------------------------- overlay

    [TestMethod]
    public void BitShimmerShouldCoverTheContentInsteadOfStandingInForIt()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Overlay, true);
            parameters.AddChildContent("<span>Covered content</span>");
        });

        var root = component.Find(".bit-smr");

        // The content is on the page, keeping the box the size of what is being waited on, and the placeholder
        // is laid over it rather than in place of it.
        Assert.IsTrue(root.ClassList.Contains("bit-smr-ovl"));
        Assert.IsNotNull(component.Find(".bit-smr-cvd"));
        Assert.IsTrue(component.Markup.Contains("Covered content"));
        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);
    }

    [TestMethod]
    public void BitShimmerShouldNotCoverAnythingWhileItIsNotAnOverlay()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.AddChildContent("<span>Covered content</span>"));

        Assert.AreEqual(0, component.FindAll(".bit-smr-cvd").Count);
        Assert.IsFalse(component.Find(".bit-smr").ClassList.Contains("bit-smr-ovl"));
        Assert.IsFalse(component.Markup.Contains("Covered content"));
    }

    [TestMethod]
    public void BitShimmerOverlayShouldRespectTheContentAlias()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Overlay, true);
            parameters.Add(p => p.Content, (RenderFragment)(builder => builder.AddContent(0, "Covered content")));
        });

        Assert.IsTrue(component.Find(".bit-smr-cvd").TextContent.Contains("Covered content"));
    }

    [TestMethod]
    public void BitShimmerOverlayShouldBeOneBoxOverTheWholeOfTheContent()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Overlay, true);
            parameters.Add(p => p.Lines, 4);
            parameters.AddChildContent("Covered content");
        });

        // A cover is one box over content whose size is already known, so the stack of lines is not one of the
        // two layouts the box would then have.
        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);
        Assert.IsFalse(component.Find(".bit-smr").ClassList.Contains("bit-smr-mln"));
    }

    [TestMethod]
    public void BitShimmerOverlayShouldLeaveTheTemplateToTheInPlacePlaceholder()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Overlay, true);
            parameters.Add(p => p.Template, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-template");
                builder.CloseElement();
            }));
            parameters.AddChildContent("Covered content");
        });

        var root = component.Find(".bit-smr");

        Assert.AreEqual(0, component.FindAll(".custom-template").Count);
        Assert.IsFalse(root.ClassList.Contains("bit-smr-tpl"));
        Assert.IsTrue(root.ClassList.Contains("bit-smr-ovl"));
        Assert.AreEqual(1, component.FindAll(".bit-smr-wrp").Count);
    }

    [TestMethod]
    public void BitShimmerShouldUncoverTheContentOnceItIsLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Overlay, true);
            parameters.AddChildContent("Covered content");
        });

        Assert.AreEqual(1, component.FindAll(".bit-smr-cvd").Count);

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        var root = component.Find(".bit-smr");

        Assert.AreEqual(0, component.FindAll(".bit-smr-cvd").Count);
        Assert.AreEqual(0, component.FindAll(".bit-smr-wrp").Count);
        Assert.IsFalse(root.ClassList.Contains("bit-smr-ovl"));
        Assert.IsTrue(root.ClassList.Contains("bit-smr-ldd"));
        Assert.IsTrue(component.Find(".bit-smr-cnt").TextContent.Contains("Covered content"));
    }

    [TestMethod]
    public void BitShimmerOverlayShouldStillReportThatItIsBusy()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Overlay, true);
            parameters.AddChildContent("Covered content");
        });

        Assert.AreEqual("true", component.Find(".bit-smr").GetAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitShimmerOverlayShouldRespectTheContentClassAndStyle()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Overlay, true);
            parameters.Add(p => p.Classes, new BitShimmerClassStyles { Content = "custom-content" });
            parameters.Add(p => p.Styles, new BitShimmerClassStyles { Content = "color:tomato" });
            parameters.AddChildContent("Covered content");
        });

        var covered = component.Find(".bit-smr-cvd");

        Assert.IsTrue(covered.ClassList.Contains("custom-content"));
        Assert.AreEqual("color:tomato", covered.GetAttribute("style"));
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
    [DataRow(BitPoliteness.Assertive, "assertive", "alert")]
    public void BitShimmerShouldRespectPoliteness(BitPoliteness politeness, string expectedAriaLive, string expectedRole)
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading");
            parameters.Add(p => p.Politeness, politeness);
        });

        var region = component.Find(".bit-smr-vhd");

        Assert.AreEqual(expectedAriaLive, region.GetAttribute("aria-live"));

        // Both roles carry a politeness of their own, so the one that is rendered is the one that agrees with
        // aria-live - and a region asked to stay silent drops the role along with the live region it implies.
        Assert.AreEqual(expectedRole, region.GetAttribute("role"));
    }



    // ----------------------------------------------------------------- the role of the root

    [TestMethod]
    public void BitShimmerShouldNotTakeARoleItWasNotGivenAnythingToSay()
    {
        var component = RenderComponent<BitShimmer>();

        // A page of a dozen unlabelled placeholders is a dozen progress bars a screen reader has to walk
        // past, so the role arrives with the label rather than by default.
        Assert.IsNull(component.Find(".bit-smr").GetAttribute("role"));
    }

    [TestMethod]
    public void BitShimmerShouldPublishALabelledPlaceholderAsAProgressBar()
    {
        var component = RenderComponent<BitShimmer>(parameters => parameters.Add(p => p.AriaLabel, "Profile"));

        var root = component.Find(".bit-smr");

        // aria-label names nothing at all on a plain element, so the label comes with the role that carries it.
        Assert.AreEqual("progressbar", root.GetAttribute("role"));
        Assert.AreEqual("Profile", root.GetAttribute("aria-label"));
        Assert.AreEqual("true", root.GetAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitShimmerShouldGiveTheProgressBarRoleBackOnceItIsLoaded()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Profile");
            parameters.AddChildContent("Loaded content");
        });

        Assert.AreEqual("progressbar", component.Find(".bit-smr").GetAttribute("role"));

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        // What is left is the box around the content, which is nobody's progress bar.
        Assert.IsNull(component.Find(".bit-smr").GetAttribute("role"));
        Assert.AreEqual("Profile", component.Find(".bit-smr").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitShimmerShouldKeepTheProgressBarRoleWhileTheSwapIsHeldBack()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.MinShowTime, 1000);
            parameters.Add(p => p.AriaLabel, "Profile");
            parameters.AddChildContent("Loaded content");
        });

        component.Render(parameters => parameters.Add(p => p.Loaded, true));

        // What the shimmer reports is what it is showing, so the role goes when the placeholder does.
        Assert.AreEqual("progressbar", component.Find(".bit-smr").GetAttribute("role"));

        component.WaitForAssertion(() => Assert.IsNull(component.Find(".bit-smr").GetAttribute("role")),
                                   TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitShimmerShouldPublishAnInlinePlaceholderAsAProgressBarToo()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
            parameters.Add(p => p.AriaLabel, "The price");
        });

        var root = component.Find(".bit-smr");

        Assert.AreEqual("span", root.TagName.ToLower());
        Assert.AreEqual("progressbar", root.GetAttribute("role"));
    }

    [TestMethod]
    public void BitShimmerShouldPublishATemplatedSkeletonAsOneProgressBar()
    {
        var component = RenderComponent<BitShimmer>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "The card");
            parameters.Add(p => p.Template, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-template");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual("progressbar", component.Find(".bit-smr").GetAttribute("role"));
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
