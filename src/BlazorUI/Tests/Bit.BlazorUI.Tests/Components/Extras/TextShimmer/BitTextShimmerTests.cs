using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TextShimmer;

[TestClass]
public class BitTextShimmerTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTextShimmerShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-tsh");

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
    public void BitTextShimmerShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>();

        Assert.IsFalse(component.Find(".bit-tsh").ClassList.Contains("bit-dis"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-tsh").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRenderText()
    {
        const string text = "Thinking...";

        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, text);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(text, root.TextContent);
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectTextChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "12345");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "1234567890");
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("1234567890", root.TextContent);
        // The band is scaled by the length of the text, so a new text is a new width of the band as well.
        StringAssert.Contains(root.GetAttribute("style"), "--bit-tsh-spread:20px");
    }

    [TestMethod]
    public void BitTextShimmerShouldRenderParagraphElementByDefault()
    {
        var component = RenderComponent<BitTextShimmer>();

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("P", root.TagName);
    }

    [TestMethod]
    public void BitTextShimmerShouldRenderExpectedMarkup()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "12345");
        });

        component.MarkupMatches(@"<p id:ignore class=""bit-tsh"" style=""--bit-tsh-spread:10px"">12345</p>");
    }

    [TestMethod,
        DataRow("h1"),
        DataRow("span"),
        DataRow("div")]
    public void BitTextShimmerShouldRespectElement(string element)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(element.ToUpperInvariant(), root.TagName);
    }

    [TestMethod]
    public void BitTextShimmerShouldTrimTheElementName()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Element, "  h2 ");
            parameters.Add(p => p.Text, "heading");
        });

        Assert.AreEqual("H2", component.Find(".bit-tsh").TagName);
    }

    // A name that would end the tag, or that an engine refuses to build an element of, falls back to the default
    // tag rather than reaching the markup.
    [TestMethod,
        DataRow(""),
        DataRow("   "),
        DataRow("not a tag name"),
        DataRow("h4!"),
        DataRow("<script>"),
        DataRow("1h")]
    public void BitTextShimmerShouldFallBackToTheDefaultElementForAnUnusableElementName(string element)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.Add(p => p.Text, "text");
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("P", root.TagName);
        Assert.AreEqual("text", root.TextContent);
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectElementChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "text");
        });

        Assert.AreEqual("P", component.Find(".bit-tsh").TagName);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Element, "span");
        });

        Assert.AreEqual("SPAN", component.Find(".bit-tsh").TagName);
        Assert.AreEqual("text", component.Find(".bit-tsh").TextContent);
    }

    // A void element is defined to hold no content, so what is put inside one would either be dropped by the static
    // renderer or end up as a sibling of the element in the rendered markup.
    [TestMethod,
        DataRow("br"),
        DataRow("hr"),
        DataRow("img")]
    public void BitTextShimmerShouldNotRenderContentInsideAVoidElement(string element)
    {
        var withText = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.Add(p => p.Text, "this text has nowhere to go");
        });

        var withContent = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.AddChildContent("this content has nowhere to go");
        });

        Assert.IsFalse(withText.Markup.Contains("nowhere to go"));
        Assert.IsFalse(withContent.Markup.Contains("nowhere to go"));
    }

    [TestMethod]
    public void BitTextShimmerShouldScaleSpreadByTextLength()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "12345");
            parameters.Add(p => p.Spread, 2);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-spread:10px"));
    }

    [TestMethod]
    public void BitTextShimmerShouldScaleSpreadByContentLengthWithoutText()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.ContentLength, 20);
            parameters.Add(p => p.Spread, 1.5);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-spread:30px"));
    }

    // The content takes precedence over the text, so it is the content - measured by ContentLength - that the band
    // is scaled for rather than the text that is not rendered.
    [TestMethod]
    public void BitTextShimmerShouldScaleSpreadByContentLengthWhenBothTextAndContentAreSet()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "a much longer text than the content length");
            parameters.Add(p => p.ContentLength, 4);
            parameters.AddChildContent("content");
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-spread:8px");
    }

    // The length is counted in user-perceived characters, so an emoji with a skin tone modifier or a letter with a
    // combining accent is one character rather than the two or four UTF-16 units it is stored in.
    [TestMethod,
        DataRow("\U0001F44D\U0001F3FDab", "6px"),
        DataRow("éé", "4px"),
        DataRow("", "0px")]
    public void BitTextShimmerShouldCountTheTextInUserPerceivedCharacters(string text, string expected)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, text);
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), $"--bit-tsh-spread:{expected}");
    }

    [TestMethod,
        DataRow(-1.0, 0, "0px"),
        DataRow(2.0, -5, "0px"),
        DataRow(0.5, 3, "1.5px"),
        DataRow(1.0 / 3, 1, "0.333px")]
    public void BitTextShimmerShouldClampAndFormatSpread(double spread, int contentLength, string expected)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Spread, spread);
            parameters.Add(p => p.ContentLength, contentLength);
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), $"--bit-tsh-spread:{expected}");
    }

    // A length the browser refuses would make the whole gradient invalid and leave the transparent text with nothing
    // painted into it, so a spread that is not a finite number falls back to the default multiplier.
    [TestMethod,
        DataRow(double.NaN),
        DataRow(double.PositiveInfinity),
        DataRow(double.NegativeInfinity)]
    public void BitTextShimmerShouldIgnoreASpreadThatIsNotAFiniteNumber(double spread)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Spread, spread);
            parameters.Add(p => p.Text, "12345");
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-spread:10px");
    }

    [TestMethod]
    public void BitTextShimmerShouldWriteTheStyleInTheInvariantCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var component = RenderComponent<BitTextShimmer>(parameters =>
            {
                parameters.Add(p => p.Spread, 0.5);
                parameters.Add(p => p.ContentLength, 3);
                parameters.Add(p => p.Duration, 1500);
            });

            var style = component.Find(".bit-tsh").GetAttribute("style");

            StringAssert.Contains(style, "--bit-tsh-spread:1.5px");
            StringAssert.Contains(style, "--bit-tsh-duration:1500ms");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectSpreadChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "12345");
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-spread:10px");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Spread, 4);
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-spread:20px");
    }

    [TestMethod]
    public void BitTextShimmerShouldNotRenderOptionalStyleVariablesByDefault()
    {
        var component = RenderComponent<BitTextShimmer>();

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-spread:20px"));
        Assert.IsFalse(style.Contains("--bit-tsh-duration"));
        Assert.IsFalse(style.Contains("--bit-tsh-delay"));
        Assert.IsFalse(style.Contains("--bit-tsh-iterations"));
        Assert.IsFalse(style.Contains("--bit-tsh-repeat-delay"));
        Assert.IsFalse(style.Contains("--bit-tsh-size"));
        Assert.IsFalse(style.Contains("--bit-tsh-angle"));
        Assert.IsFalse(style.Contains("--bit-tsh-base-clr"));
        Assert.IsFalse(style.Contains("--bit-tsh-gradient-clr"));
    }

    [TestMethod]
    public void BitTextShimmerShouldNotRenderOptionalClassesByDefault()
    {
        var component = RenderComponent<BitTextShimmer>();

        var classList = component.Find(".bit-tsh").ClassList;

        Assert.AreEqual(1, classList.Length);
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectDuration()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Duration, 1500);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-duration:1500ms"));
    }

    [TestMethod]
    public void BitTextShimmerShouldClampANegativeDuration()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Duration, -500);
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-duration:0ms");
    }

    [TestMethod,
        DataRow(0, "0ms"),
        DataRow(750, "750ms"),
        DataRow(-200, "0ms")]
    public void BitTextShimmerShouldRespectDelay(int delay, string expected)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Delay, delay);
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), $"--bit-tsh-delay:{expected}");
    }

    [TestMethod,
        DataRow(1),
        DataRow(3)]
    public void BitTextShimmerShouldRespectIterations(int iterations)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Iterations, iterations);
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), $"--bit-tsh-iterations:{iterations}");
    }

    // A count below one is not a number of sweeps a shimmer can play, so it is left to the stylesheet, which sweeps
    // forever.
    [TestMethod,
        DataRow(0),
        DataRow(-1)]
    public void BitTextShimmerShouldIgnoreIterationsBelowOne(int iterations)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Iterations, iterations);
        });

        Assert.IsFalse(component.Find(".bit-tsh").GetAttribute("style")!.Contains("--bit-tsh-iterations"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectIterationsChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Iterations, 1);
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-iterations:1");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Iterations, (int?)null);
        });

        Assert.IsFalse(component.Find(".bit-tsh").GetAttribute("style")!.Contains("--bit-tsh-iterations"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectColors()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.BaseColor, "#3f3f46");
            parameters.Add(p => p.GradientColor, "#22d3ee");
        });

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-base-clr:#3f3f46"));
        Assert.IsTrue(style.Contains("--bit-tsh-gradient-clr:#22d3ee"));
    }

    [TestMethod]
    public void BitTextShimmerShouldIgnoreEmptyColors()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.BaseColor, "");
            parameters.Add(p => p.GradientColor, "  ");
        });

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-tsh-base-clr"));
        Assert.IsFalse(style.Contains("--bit-tsh-gradient-clr"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectColorsChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.GradientColor, "red");
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-gradient-clr:red");

        component.Render(parameters =>
        {
            parameters.Add(p => p.GradientColor, "blue");
        });

        var style = component.Find(".bit-tsh").GetAttribute("style");

        StringAssert.Contains(style, "--bit-tsh-gradient-clr:blue");
        Assert.IsFalse(style!.Contains("red"));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-tsh-pri"),
        DataRow(BitColor.Secondary, "bit-tsh-sec"),
        DataRow(BitColor.Tertiary, "bit-tsh-ter"),
        DataRow(BitColor.Info, "bit-tsh-inf"),
        DataRow(BitColor.Success, "bit-tsh-suc"),
        DataRow(BitColor.Warning, "bit-tsh-wrn"),
        DataRow(BitColor.SevereWarning, "bit-tsh-swr"),
        DataRow(BitColor.Error, "bit-tsh-err"),
        DataRow(BitColor.PrimaryBackground, "bit-tsh-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-tsh-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-tsh-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-tsh-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-tsh-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-tsh-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-tsh-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-tsh-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-tsh-tbr")]
    public void BitTextShimmerShouldRespectColor(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-tsh").ClassList.Contains(expectedClass));
    }

    // The explicit color is written inline, which is what lets it win over the one the class of the Color reads from
    // the theme.
    [TestMethod]
    public void BitTextShimmerShouldKeepBothColorAndGradientColor()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Primary);
            parameters.Add(p => p.GradientColor, "gold");
        });

        var root = component.Find(".bit-tsh");

        Assert.IsTrue(root.ClassList.Contains("bit-tsh-pri"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-tsh-gradient-clr:gold");
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectColorChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Primary);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
        });

        var root = component.Find(".bit-tsh");

        Assert.IsFalse(root.ClassList.Contains("bit-tsh-pri"));
        Assert.IsTrue(root.ClassList.Contains("bit-tsh-err"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTextShimmerShouldRespectPaused(bool paused)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Paused, paused);
        });

        Assert.AreEqual(paused, component.Find(".bit-tsh").ClassList.Contains("bit-tsh-pau"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectPausedChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Paused, true);
        });

        Assert.IsTrue(component.Find(".bit-tsh").ClassList.Contains("bit-tsh-pau"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Paused, false);
        });

        Assert.IsFalse(component.Find(".bit-tsh").ClassList.Contains("bit-tsh-pau"));
    }

    // The stylesheet reads the reading direction off the element itself, so the class only carries Reversed: on a
    // right-to-left element it is the direction attribute and the rtl class that turn the band around, and the two
    // are what a reversed band on such an element is turned back by.
    [TestMethod,
        DataRow(false, null),
        DataRow(false, BitDir.Ltr),
        DataRow(false, BitDir.Rtl),
        DataRow(true, null),
        DataRow(true, BitDir.Ltr),
        DataRow(true, BitDir.Rtl)]
    public void BitTextShimmerShouldRespectReversed(bool reversed, BitDir? dir)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(reversed, root.ClassList.Contains("bit-tsh-rev"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual(dir?.ToString().ToLowerInvariant(), root.GetAttribute("dir"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectReversedChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>();

        Assert.IsFalse(component.Find(".bit-tsh").ClassList.Contains("bit-tsh-rev"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reversed, true);
        });

        Assert.IsTrue(component.Find(".bit-tsh").ClassList.Contains("bit-tsh-rev"));
    }

    [TestMethod]
    public void BitTextShimmerShouldTakeTheCascadingDir()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.AddCascadingValue(BitDir.Rtl);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>();

        Assert.IsFalse(component.Find(".bit-tsh").ClassList.Contains("bit-rtl"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTextShimmerShouldRespectPauseOnHover(bool pauseOnHover)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.PauseOnHover, pauseOnHover);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(pauseOnHover, root.ClassList.Contains("bit-tsh-poh"));
        // Hovering is what pauses it, so the shimmer itself is not paused.
        Assert.IsFalse(root.ClassList.Contains("bit-tsh-pau"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTextShimmerShouldRespectStatic(bool isStatic)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Static, isStatic);
            parameters.Add(p => p.Text, "done");
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(isStatic, root.ClassList.Contains("bit-tsh-sta"));
        Assert.AreEqual("done", root.TextContent);
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectStaticChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "Thinking...");
        });

        Assert.IsFalse(component.Find(".bit-tsh").ClassList.Contains("bit-tsh-sta"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Static, true);
            parameters.Add(p => p.Text, "Done");
        });

        var root = component.Find(".bit-tsh");

        Assert.IsTrue(root.ClassList.Contains("bit-tsh-sta"));
        Assert.AreEqual("Done", root.TextContent);
    }

    // The pause widens the background by the distance the band would travel during it (2 + pause / duration widths
    // of the text) and lengthens the sweep by the pause itself, so the band keeps the speed it had without it.
    [TestMethod,
        DataRow(1000, 2000, "250%"),
        DataRow(2000, 2000, "300%"),
        DataRow(500, 1500, "233.333%")]
    public void BitTextShimmerShouldRespectRepeatDelay(int repeatDelay, int duration, string expectedSize)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.RepeatDelay, repeatDelay);
            parameters.Add(p => p.Duration, duration);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style");

        StringAssert.Contains(style, $"--bit-tsh-repeat-delay:{repeatDelay}ms");
        StringAssert.Contains(style, $"--bit-tsh-size:{expectedSize}");
    }

    // Without a Duration the ratio is taken of the default two-second sweep.
    [TestMethod]
    public void BitTextShimmerShouldScaleRepeatDelayByTheDefaultDuration()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.RepeatDelay, 1000);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style");

        StringAssert.Contains(style, "--bit-tsh-repeat-delay:1000ms");
        StringAssert.Contains(style, "--bit-tsh-size:250%");
        Assert.IsFalse(style!.Contains("--bit-tsh-duration"));
    }

    // A pause of nothing, or a sweep of no length to pause between, leaves the stylesheet's defaults alone.
    [TestMethod,
        DataRow(0, null),
        DataRow(-100, null),
        DataRow(1000, 0),
        DataRow(1000, -50)]
    public void BitTextShimmerShouldIgnoreAnUnusableRepeatDelay(int repeatDelay, int? duration)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.RepeatDelay, repeatDelay);
            parameters.Add(p => p.Duration, duration);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style")!;

        Assert.IsFalse(style.Contains("--bit-tsh-repeat-delay"));
        Assert.IsFalse(style.Contains("--bit-tsh-size"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectRepeatDelayChangingAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.RepeatDelay, 1000);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Duration, 4000);
        });

        // The size is a ratio of the pause to the sweep, so a new duration is a new size too.
        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-size:225%");

        component.Render(parameters =>
        {
            parameters.Add(p => p.RepeatDelay, (int?)null);
        });

        Assert.IsFalse(component.Find(".bit-tsh").GetAttribute("style")!.Contains("--bit-tsh-size"));
    }

    [TestMethod,
        DataRow(20.0, "20deg"),
        DataRow(-15.5, "-15.5deg"),
        DataRow(0.0, "0deg")]
    public void BitTextShimmerShouldRespectAngle(double angle, string expected)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Angle, angle);
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), $"--bit-tsh-angle:{expected}");
    }

    [TestMethod,
        DataRow(double.NaN),
        DataRow(double.PositiveInfinity)]
    public void BitTextShimmerShouldIgnoreAnAngleThatIsNotAFiniteNumber(double angle)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Angle, angle);
        });

        Assert.IsFalse(component.Find(".bit-tsh").GetAttribute("style")!.Contains("--bit-tsh-angle"));
    }

    // An explicit length replaces the computed one, whatever the text, the content length and the multiplier say.
    [TestMethod,
        DataRow("3em", "3em"),
        DataRow(" 40px ", "40px")]
    public void BitTextShimmerShouldRespectSpreadLength(string spreadLength, string expected)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.SpreadLength, spreadLength);
            parameters.Add(p => p.Spread, 5);
            parameters.Add(p => p.Text, "12345");
        });

        var style = component.Find(".bit-tsh").GetAttribute("style")!;

        StringAssert.Contains(style, $"--bit-tsh-spread:{expected}");
        Assert.IsFalse(style.Contains("25px"));
    }

    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("   ")]
    public void BitTextShimmerShouldComputeTheSpreadWithoutASpreadLength(string? spreadLength)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.SpreadLength, spreadLength);
            parameters.Add(p => p.Text, "12345");
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-spread:10px");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTextShimmerShouldRespectAlternate(bool alternate)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Alternate, alternate);
            parameters.Add(p => p.Reversed, true);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(alternate, root.ClassList.Contains("bit-tsh-alt"));
        // The two compose: Reversed still decides which way the first of the alternating sweeps runs.
        Assert.IsTrue(root.ClassList.Contains("bit-tsh-rev"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTextShimmerShouldRespectForceAnimation(bool forceAnimation)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.ForceAnimation, forceAnimation);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(forceAnimation, root.ClassList.Contains("bit-fam"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRenderChildContentOverText()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "the text");
            parameters.AddChildContent("<strong>the content</strong>");
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("the content", root.TextContent);
        Assert.IsNotNull(component.Find(".bit-tsh strong"));
    }

    [TestMethod]
    public void BitTextShimmerShouldSwitchFromChildContentToTextAfterRender()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "the text");
            parameters.AddChildContent("<strong>the content</strong>");
        });

        Assert.AreEqual("the content", component.Find(".bit-tsh").TextContent);

        component.Render(parameters =>
        {
            parameters.Add(p => p.ChildContent, (RenderFragment?)null);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("the text", root.TextContent);
        Assert.AreEqual(0, root.QuerySelectorAll("strong").Length);
        // Without the content the band is scaled by the text again.
        StringAssert.Contains(root.GetAttribute("style"), "--bit-tsh-spread:16px");
    }

    [TestMethod]
    public void BitTextShimmerShouldMergeStyleWithItsOwnVariables()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "12345");
            parameters.Add(p => p.Duration, 1000);
            parameters.Add(p => p.Style, "font-size:2rem");
        });

        Assert.AreEqual("--bit-tsh-spread:10px;--bit-tsh-duration:1000ms;font-size:2rem",
                        component.Find(".bit-tsh").GetAttribute("style"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectClass()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = component.Find(".bit-tsh");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-tsh"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectId()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Id, "shimmer-id");
        });

        Assert.AreEqual("shimmer-id", component.Find(".bit-tsh").GetAttribute("id"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRenderTheUniqueIdByDefault()
    {
        var component = RenderComponent<BitTextShimmer>();

        Assert.AreEqual(component.Instance.UniqueId, component.Find(".bit-tsh").GetAttribute("id"));
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitTextShimmerShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectAriaLabelAndTabIndex()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Assistant is thinking");
            parameters.Add(p => p.TabIndex, "-1");
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("Assistant is thinking", root.GetAttribute("aria-label"));
        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitTextShimmerShouldNotRenderUnsetAttributes()
    {
        var component = RenderComponent<BitTextShimmer>();

        var root = component.Find(".bit-tsh");

        Assert.IsFalse(root.HasAttribute("dir"));
        Assert.IsFalse(root.HasAttribute("aria-label"));
        Assert.IsFalse(root.HasAttribute("tabindex"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitTextShimmerShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style")!;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod]
    public void BitTextShimmerShouldMergeTheSplattedClassAndStyleWithItsOwn()
    {
        var component = RenderComponent<BitTextShimmerSplattedAttributesTest>();

        var element = component.Find("p");

        Assert.IsTrue(element.ClassList.Contains("bit-tsh"));
        Assert.IsTrue(element.ClassList.Contains("splatted-class"));

        Assert.AreEqual("font-weight:bold;--bit-tsh-spread:10px", element.GetAttribute("style"));
    }

    // A value the component would otherwise write as null does not leave a splatted attribute of the same name
    // alone - it removes it - so each one is resolved against what the page splatted instead. The attributes the
    // component has no parameter for, like the role and the politeness of a live region, are splatted as they are.
    [TestMethod]
    public void BitTextShimmerShouldKeepTheSplattedAttributesItWouldOtherwiseWriteItself()
    {
        var component = RenderComponent<BitTextShimmerSplattedAttributesTest>();

        var element = component.Find("section");

        Assert.AreEqual("splatted-id", element.GetAttribute("id"));
        Assert.AreEqual("rtl", element.GetAttribute("dir"));
        Assert.AreEqual("splatted label", element.GetAttribute("aria-label"));
        Assert.AreEqual("-1", element.GetAttribute("tabindex"));
        Assert.AreEqual("status", element.GetAttribute("role"));
        Assert.AreEqual("polite", element.GetAttribute("aria-live"));
    }

    // What the component is given itself is written over the splatted spelling of the same attribute.
    [TestMethod]
    public void BitTextShimmerShouldWriteItsOwnParametersOverTheSplattedAttributes()
    {
        var component = RenderComponent<BitTextShimmerSplattedAttributesTest>();

        var element = component.Find("article");

        Assert.AreEqual("own-id", element.GetAttribute("id"));
        Assert.AreEqual("ltr", element.GetAttribute("dir"));
        Assert.AreEqual("own label", element.GetAttribute("aria-label"));
        Assert.AreEqual("0", element.GetAttribute("tabindex"));
    }
}
