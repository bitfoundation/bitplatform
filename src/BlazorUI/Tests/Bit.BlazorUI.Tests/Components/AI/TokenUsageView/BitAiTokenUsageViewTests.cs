using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.TokenUsageView;

[TestClass]
public class BitAiTokenUsageViewTests : BitAiTestBase
{
    private static readonly BitAiTokenUsage Usage = new() { InputTokens = 1200, OutputTokens = 300 };

    private CultureInfo? _culture;

    // The figures are formatted in the culture of the page, so the expectations below are written in one.
    [TestInitialize]
    public void UseEnglishNumbers()
    {
        _culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [TestCleanup]
    public void RestoreCulture()
    {
        if (_culture is not null) CultureInfo.CurrentCulture = _culture;
    }

    // 300 output tokens over the two seconds between the first token and the last one: 150 tokens a second.
    private static BitAiGenerationMetadata Metadata()
    {
        var startedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return new BitAiGenerationMetadata
        {
            Usage = Usage,
            StartedAt = startedAt,
            CompletedAt = startedAt.AddMilliseconds(2500),
            TimeToFirstToken = TimeSpan.FromMilliseconds(500),
        };
    }



    [TestMethod]
    public void BitAiTokenUsageViewShouldShowTheTokens()
    {
        var component = RenderComponent<BitAiTokenUsageView>(p => p.Add(c => c.Usage, Usage));

        var root = component.Find(".bit-aituv");

        Assert.AreEqual("SPAN", root.TagName);
        Assert.AreEqual("1.5k tokens", component.Find(".bit-aituv-tok").TextContent);
        Assert.AreEqual("Input: 1,200\nOutput: 300", root.GetAttribute("title"));
        Assert.IsEmpty(component.FindAll(".bit-aituv-cst"));
        Assert.IsEmpty(component.FindAll(".bit-aituv-spd"));
        Assert.IsEmpty(component.FindAll(".bit-aituv-ltc"));
    }

    // A title reaches the pointer alone, so the breakdown is also hidden text of the readout, a line to each kind, right
    // after the total it breaks down.
    [TestMethod]
    public void TheBreakdownIsAlsoTextForThoseTheTooltipNeverReaches()
    {
        var component = RenderComponent<BitAiTokenUsageView>(p => p.Add(c => c.Usage, Usage with { Cost = 1 }));

        var hidden = component.Find(".bit-aituv > .bit-aituv-vhd");
        var lines = hidden.Children.Select(l => l.TextContent.Trim()).ToArray();

        CollectionAssert.AreEqual(new[] { "Input: 1,200", "Output: 300" }, lines);
        Assert.IsTrue(hidden.PreviousElementSibling!.ClassList.Contains("bit-aituv-tok"));
        Assert.IsTrue(hidden.NextElementSibling!.ClassList.Contains("bit-aituv-cst"));
    }

    [TestMethod]
    public void TheLatencyIsNamedForAssistiveTechnologies()
    {
        var component = RenderComponent<BitAiTokenUsageView>(p => p
            .Add(c => c.Usage, Usage)
            .Add(c => c.Metadata, Metadata())
            .Add(c => c.ShowLatency, true));

        var latency = component.Find(".bit-aituv-ltc");

        Assert.AreEqual("Time to first token", latency.QuerySelector(".bit-aituv-vhd")!.TextContent.Trim());
        Assert.AreEqual("Time to first token 500ms", latency.TextContent);
    }

    [TestMethod,
        DataRow(999L, "999"),
        DataRow(1_000L, "1k"),
        DataRow(1_550L, "1.6k"),
        DataRow(311_000L, "311k"),
        DataRow(311_040L, "311k"),
        DataRow(2_000_000L, "2M"),
        DataRow(2_400_000L, "2.4M")]
    public void ATokenCountDropsADecimalThatIsZero(long tokens, string expected)
    {
        Assert.AreEqual(expected, new BitAiStrings().FormatTokens(tokens));
    }

    [TestMethod,
        DataRow(null, "0.5", "$0.50"),
        DataRow("USD", "0.004", "$0.0040"),
        DataRow("EUR", "1.25", "€1.25"),
        DataRow("GBP", "2", "£2.00"),
        DataRow("JPY", "3", "JPY 3"),
        DataRow("JPY", "0.4", "JPY 0.40"),
        DataRow("KWD", "1.5", "KWD 1.500"),
        DataRow("USD", "0", "$0.00")]
    public void TheCostIsWrittenInItsCurrency(string? currency, string cost, string expected)
    {
        var usage = Usage with { Cost = decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture), Currency = currency };

        var component = RenderComponent<BitAiTokenUsageView>(p => p.Add(c => c.Usage, usage));

        Assert.AreEqual(expected, component.Find(".bit-aituv-cst").TextContent);
    }

    [TestMethod]
    public void TheSpeedAndTheLatencyComeFromTheMetadataWhenAskedFor()
    {
        var metadata = Metadata();

        var byDefault = RenderComponent<BitAiTokenUsageView>(p => p.Add(c => c.Usage, Usage).Add(c => c.Metadata, metadata));
        var everything = RenderComponent<BitAiTokenUsageView>(p => p
            .Add(c => c.Usage, Usage)
            .Add(c => c.Metadata, metadata)
            .Add(c => c.ShowLatency, true));
        var nothing = RenderComponent<BitAiTokenUsageView>(p => p
            .Add(c => c.Usage, Usage)
            .Add(c => c.Metadata, metadata)
            .Add(c => c.ShowSpeed, false));

        Assert.AreEqual("150 tok/s", byDefault.Find(".bit-aituv-spd").TextContent);
        Assert.IsEmpty(byDefault.FindAll(".bit-aituv-ltc"));
        Assert.AreEqual("Time to first token 500ms", everything.Find(".bit-aituv-ltc").TextContent);
        Assert.IsEmpty(nothing.FindAll(".bit-aituv-spd"));
    }

    [TestMethod]
    public void AnEmptyUsageShowsNoFigures()
    {
        var component = RenderComponent<BitAiTokenUsageView>(p => p.Add(c => c.Usage, new BitAiTokenUsage()));

        Assert.IsEmpty(component.Find(".bit-aituv").Children);
    }

    [TestMethod]
    public void BitAiTokenUsageViewShouldRespectClassStyleAndDisabled()
    {
        var component = RenderComponent<BitAiTokenUsageView>(p => p
            .Add(c => c.Usage, Usage)
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px")
            .Add(c => c.Disabled, true));

        var root = component.Find(".bit-aituv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiTokenUsageViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiTokenUsageView>(p => p
            .Add(c => c.Usage, Usage)
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aituv").GetAttribute("style");

        if (expected is null)
        {
            Assert.IsNull(style);
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitAiTokenUsageViewShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiTokenUsageView>(p => p
            .Add(c => c.Usage, Usage)
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aituv");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitAiTokenUsageViewShouldHandTheClassesAndStylesToItsParts()
    {
        var component = RenderComponent<BitAiTokenUsageView>(p => p
            .Add(c => c.Usage, Usage with { Cost = 1 })
            .Add(c => c.Classes, new() { Root = "c-root", Tokens = "c-tokens", Cost = "c-cost" })
            .Add(c => c.Styles, new() { Root = "color: red", Tokens = "color: blue" }));

        Assert.IsTrue(component.Find(".bit-aituv").ClassList.Contains("c-root"));
        StringAssert.Contains(component.Find(".bit-aituv").GetAttribute("style"), "color: red");
        Assert.IsTrue(component.Find(".bit-aituv-tok").ClassList.Contains("c-tokens"));
        Assert.AreEqual("color: blue", component.Find(".bit-aituv-tok").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aituv-cst").ClassList.Contains("c-cost"));
    }

    [TestMethod]
    public void BitAiTokenUsageViewShouldTakeTheCascadedParameters()
    {
        var metadata = Metadata();

        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiTokenUsageViewParams { Class = "cascaded", ShowSpeed = false, ShowLatency = true }
            });
            p.AddChildContent<BitAiTokenUsageView>(c => c.Add(x => x.Usage, Usage).Add(x => x.Metadata, metadata));
        });

        Assert.IsTrue(component.Find(".bit-aituv").ClassList.Contains("cascaded"));
        Assert.IsEmpty(component.FindAll(".bit-aituv-spd"));
        Assert.IsNotNull(component.Find(".bit-aituv-ltc"));
    }

    [TestMethod]
    public void BitAiTokenUsageViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiTokenUsageView", BitAiTokenUsageViewParams.ParamName);
        Assert.AreEqual(BitAiTokenUsageViewParams.ParamName, new BitAiTokenUsageViewParams().Name);
    }
}
