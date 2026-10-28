using System.Collections.Generic;
using System.Globalization;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ContextMeter;

[TestClass]
public class BitAiContextMeterTests : BitAiTestBase
{
    private CultureInfo? _culture;

    // The counts are formatted in the culture of the page, so the expectations below are written in one.
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

    private static BitAiConversation ConversationWithTurns(int turns, long window = 10_000)
    {
        var conversation = new BitAiConversation();
        for (var i = 0; i < turns; i++)
        {
            conversation.AddMessage(BitAiRole.User, $"question {i}");
            var reply = conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant));
            reply.MergeMetadata(new BitAiGenerationMetadata
            {
                Usage = new BitAiTokenUsage { InputTokens = 1_000 * (i + 1), OutputTokens = 100 },
                ContextWindow = window,
            });
        }

        return conversation;
    }



    [TestMethod]
    public void TheContextMeterReadsTheLatestTurnRatherThanSummingEveryTurn()
    {
        var conversation = ConversationWithTurns(3);

        var (used, window) = BitAiContextMeter.Measure(conversation);

        Assert.AreEqual(3_100L, used);
        Assert.AreEqual(10_000L, window);
        Assert.AreEqual(6_300L, conversation.TotalUsage.TotalTokens);
    }

    [TestMethod]
    public void TheContextMeterCountsPendingTokensOnTopOfTheConversation()
    {
        var component = RenderComponent<BitAiContextMeter>(p => p
            .Add(c => c.Conversation, ConversationWithTurns(1))
            .Add(c => c.PendingTokens, 400));

        var meter = component.Find("[role=meter]");

        Assert.AreEqual("15", meter.GetAttribute("aria-valuenow"));
        Assert.AreEqual("0", meter.GetAttribute("aria-valuemin"));
        Assert.AreEqual("100", meter.GetAttribute("aria-valuemax"));
        Assert.AreEqual("1,500 / 10,000 tokens", meter.GetAttribute("aria-valuetext"));
        Assert.AreEqual("Context", meter.GetAttribute("aria-label"));
        Assert.AreEqual("15%", component.Find(".bit-aictm-pct").TextContent);
        Assert.AreEqual("1,500 / 10,000 tokens", component.Find(".bit-aictm").GetAttribute("title"));
    }

    [TestMethod,
        DataRow(79L, null, null),
        DataRow(80L, "bit-aictm-wrn", "bit-prb-wrn"),
        DataRow(95L, "bit-aictm-crt", "bit-prb-err")]
    public void TheContextMeterWarnsAtEightyAndNinetyFivePercent(long used, string? severity, string? meterColor)
    {
        var component = RenderComponent<BitAiContextMeter>(p => p.Add(c => c.Used, used).Add(c => c.Window, 100));

        var root = component.Find(".bit-aictm");
        var meter = component.Find(".bit-aictm-mtr");
        var status = component.Find(".bit-aictm-sts");

        // The live region is on the page from the first render, so the warning is announced when it lands in it.
        Assert.AreEqual("status", status.GetAttribute("role"));

        if (severity is null)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-aictm-wrn"));
            Assert.IsFalse(root.ClassList.Contains("bit-aictm-crt"));
            Assert.IsFalse(meter.ClassList.Contains("bit-prb-wrn"));
            Assert.IsFalse(meter.ClassList.Contains("bit-prb-err"));
            Assert.AreEqual(string.Empty, status.TextContent);
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains(severity));
            Assert.IsTrue(meter.ClassList.Contains(meterColor!));
            Assert.AreNotEqual(string.Empty, status.TextContent);
        }
    }

    [TestMethod]
    public void TheWarningIsAnnouncedAsTheMeterCrossesIt()
    {
        var component = RenderComponent<BitAiContextMeter>(p => p.Add(c => c.Used, 10).Add(c => c.Window, 100));
        Assert.AreEqual(string.Empty, component.Find(".bit-aictm-sts").TextContent);

        component.Render(p => p.Add(c => c.Used, 85));

        Assert.AreEqual("The context window is filling up", component.Find(".bit-aictm-sts").TextContent);
        Assert.IsTrue(component.Find(".bit-aictm").ClassList.Contains("bit-aictm-wrn"));

        component.Render(p => p.Add(c => c.Used, 96));

        Assert.AreEqual("The context window is almost full", component.Find(".bit-aictm-sts").TextContent);
        Assert.IsTrue(component.Find(".bit-aictm").ClassList.Contains("bit-aictm-crt"));
        Assert.IsFalse(component.Find(".bit-aictm").ClassList.Contains("bit-aictm-wrn"));
    }

    [TestMethod]
    public void TheContextMeterThresholdsAreConfigurable()
    {
        var component = RenderComponent<BitAiContextMeter>(p => p
            .Add(c => c.Used, 60).Add(c => c.Window, 100)
            .Add(c => c.Warn, 0.5).Add(c => c.Critical, 0.7));

        Assert.IsTrue(component.Find(".bit-aictm").ClassList.Contains("bit-aictm-wrn"));
    }

    [TestMethod]
    public void TheContextMeterFollowsAChangeOfModel()
    {
        var small = new BitAiModel("small", "Small") { ContextWindow = 1_000 };
        var large = new BitAiModel("large", "Large") { ContextWindow = 4_000 };

        var component = RenderComponent<BitAiContextMeter>(p => p.Add(c => c.Used, 500).Add(c => c.Model, small));
        Assert.AreEqual("50", component.Find("[role=meter]").GetAttribute("aria-valuenow"));

        component.Render(p => p.Add(c => c.Model, large));

        Assert.AreEqual("12", component.Find("[role=meter]").GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void AnOverfullWindowFillsTheBarAndSaysByHowMuch()
    {
        var component = RenderComponent<BitAiContextMeter>(p => p.Add(c => c.Used, 150).Add(c => c.Window, 100));

        Assert.AreEqual("100", component.Find("[role=meter]").GetAttribute("aria-valuenow"));
        Assert.AreEqual("150%", component.Find(".bit-aictm-pct").TextContent);
    }

    // The readout is cut to a whole number as the value of the meter is, so it never shows a mark the reading has not
    // reached: rounded, 6,392 of 8,000 would read 80% with no warning beside it.
    [TestMethod,
        DataRow(6_392L, "79%", false),
        DataRow(6_400L, "80%", true)]
    public void TheReadoutNeverReachesAMarkTheReadingHasNot(long used, string percent, bool warned)
    {
        var component = RenderComponent<BitAiContextMeter>(p => p.Add(c => c.Used, used).Add(c => c.Window, 8_000));

        Assert.AreEqual(percent, component.Find(".bit-aictm-pct").TextContent);
        Assert.AreEqual(percent.TrimEnd('%'), component.Find("[role=meter]").GetAttribute("aria-valuenow"));
        Assert.AreEqual(warned, component.Find(".bit-aictm").ClassList.Contains("bit-aictm-wrn"));
    }

    // Cut in whole numbers: 0.29 * 100 is 28.999... in binary floating point.
    [TestMethod]
    public void AWholePercentIsNotCutShortByFloatingPoint()
    {
        var component = RenderComponent<BitAiContextMeter>(p => p.Add(c => c.Used, 29).Add(c => c.Window, 100));

        Assert.AreEqual("29%", component.Find(".bit-aictm-pct").TextContent);
        Assert.AreEqual("29", component.Find("[role=meter]").GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void NothingIsRenderedWithoutAWindow()
    {
        var component = RenderComponent<BitAiContextMeter>(p => p.Add(c => c.Used, 500));

        Assert.AreEqual(string.Empty, component.Markup.Trim());
    }

    [TestMethod]
    public void BitAiContextMeterShouldRespectClassStyleAndDisabled()
    {
        var component = RenderComponent<BitAiContextMeter>(p => p
            .Add(c => c.Used, 10).Add(c => c.Window, 100)
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px")
            .Add(c => c.Disabled, true));

        var root = component.Find(".bit-aictm");

        Assert.AreEqual("DIV", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(component.Find(".bit-aictm-mtr").ClassList.Contains("bit-dis"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiContextMeterShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiContextMeter>(p => p
            .Add(c => c.Used, 10).Add(c => c.Window, 100)
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aictm").GetAttribute("style");

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
    public void BitAiContextMeterShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiContextMeter>(p => p
            .Add(c => c.Used, 10).Add(c => c.Window, 100)
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aictm");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitAiContextMeterShouldHandTheClassesAndStylesToItsParts()
    {
        var component = RenderComponent<BitAiContextMeter>(p => p
            .Add(c => c.Used, 10).Add(c => c.Window, 100)
            .Add(c => c.Classes, new() { Root = "c-root", Label = "c-label", Meter = new() { Root = "c-meter" }, Percent = "c-percent" })
            .Add(c => c.Styles, new() { Root = "color: red", Status = "color: blue" }));

        Assert.IsTrue(component.Find(".bit-aictm").ClassList.Contains("c-root"));
        StringAssert.Contains(component.Find(".bit-aictm").GetAttribute("style"), "color: red");
        Assert.IsTrue(component.Find(".bit-aictm-ttl").ClassList.Contains("c-label"));
        Assert.IsTrue(component.Find(".bit-aictm-mtr").ClassList.Contains("c-meter"));
        Assert.IsTrue(component.Find(".bit-aictm-pct").ClassList.Contains("c-percent"));
        Assert.AreEqual("color: blue", component.Find(".bit-aictm-sts").GetAttribute("style"));
    }

    [TestMethod]
    public void BitAiContextMeterShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiContextMeterParams { Class = "cascaded", Warn = 0.5, Critical = 0.9 }
            });
            p.AddChildContent<BitAiContextMeter>(c => c.Add(x => x.Used, 60).Add(x => x.Window, 100));
        });

        var root = component.Find(".bit-aictm");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-aictm-wrn"));
    }

    [TestMethod]
    public void BitAiContextMeterParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiContextMeter", BitAiContextMeterParams.ParamName);
        Assert.AreEqual(BitAiContextMeterParams.ParamName, new BitAiContextMeterParams().Name);
    }
}
