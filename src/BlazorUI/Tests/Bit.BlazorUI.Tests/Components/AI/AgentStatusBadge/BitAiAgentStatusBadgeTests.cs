using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.AgentStatusBadge;

[TestClass]
public class BitAiAgentStatusBadgeTests : BitAiTestBase
{
    private sealed class GermanStrings : BitAiStrings
    {
        public override string AgentCompleted => "Fertig";
    }



    [TestMethod]
    public void BitAiAgentStatusBadgeShouldBeAPoliteLiveRegionAroundATag()
    {
        var component = RenderComponent<BitAiAgentStatusBadge>(p => p.Add(c => c.Status, BitAiAgentRunStatus.Running));

        var root = component.Find(".bit-aiasb");

        Assert.AreEqual("SPAN", root.TagName);
        Assert.AreEqual("status", root.GetAttribute("role"));
        Assert.AreEqual("polite", root.GetAttribute("aria-live"));
        Assert.AreEqual("Running", root.QuerySelector(".bit-tag")!.TextContent.Trim());
    }

    [TestMethod,
        DataRow(BitAiAgentRunStatus.Idle, "Idle", null, "bit-tag-dft-ter"),
        DataRow(BitAiAgentRunStatus.Planning, "Planning", null, null),
        DataRow(BitAiAgentRunStatus.Running, "Running", null, null),
        DataRow(BitAiAgentRunStatus.WaitingForInput, "Waiting for your answer", "bit-tag-wrn", null),
        DataRow(BitAiAgentRunStatus.Completed, "Done", "bit-tag-suc", null),
        DataRow(BitAiAgentRunStatus.Failed, "Failed", "bit-tag-err", null),
        DataRow(BitAiAgentRunStatus.Cancelled, "Stopped", null, "bit-tag-dft-ter")]
    public void EveryStatusHasAWordAndAColor(BitAiAgentRunStatus status, string text, string? colorClass, string? defaultClass)
    {
        var component = RenderComponent<BitAiAgentStatusBadge>(p => p.Add(c => c.Status, status));

        var tag = component.Find(".bit-tag");
        var colors = new[] { "bit-tag-pri", "bit-tag-ter", "bit-tag-wrn", "bit-tag-suc", "bit-tag-err" };

        Assert.AreEqual(text, tag.TextContent.Trim());

        // A working run keeps the default look of a tag, and an idle or cancelled one is drawn neutral through the
        // host-default class: no Color is passed for either, so the app's --bit-Tag-* variables reach them.
        CollectionAssert.AreEqual(colorClass is null ? new string[0] : new[] { colorClass },
                                  colors.Where(tag.ClassList.Contains).ToArray());
        CollectionAssert.AreEqual(defaultClass is null ? new string[0] : new[] { defaultClass },
                                  tag.ClassList.Where(c => c.StartsWith("bit-tag-dft-", StringComparison.Ordinal)).ToArray());
    }

    [TestMethod]
    public void AgentStatusComesFromTheStringTable()
    {
        var component = RenderComponent<BitAiAgentStatusBadge>(p => p
            .Add(c => c.Status, BitAiAgentRunStatus.Completed)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Strings = new GermanStrings() }));

        Assert.AreEqual("Fertig", component.Find(".bit-aiasb").TextContent.Trim());
    }

    [TestMethod]
    public void TheLiveRegionStaysWhileTheStatusChanges()
    {
        var component = RenderComponent<BitAiAgentStatusBadge>(p => p.Add(c => c.Status, BitAiAgentRunStatus.Running));
        var id = component.Find(".bit-aiasb").Id;

        component.Render(p => p.Add(c => c.Status, BitAiAgentRunStatus.Completed));

        var region = component.Find(".bit-aiasb");

        Assert.AreEqual(id, region.Id);
        Assert.AreEqual("status", region.GetAttribute("role"));
        Assert.AreEqual("Done", region.TextContent.Trim());
    }

    [TestMethod]
    public void ASplattedRoleAndPolitenessAreHonored()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitAiAgentStatusBadge>(0);
            builder.AddAttribute(1, nameof(BitAiAgentStatusBadge.Status), BitAiAgentRunStatus.Running);
            builder.AddAttribute(2, "role", "note");
            builder.AddAttribute(3, "aria-live", "off");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-aiasb");

        Assert.AreEqual("note", root.GetAttribute("role"));
        Assert.AreEqual("off", root.GetAttribute("aria-live"));
    }

    [TestMethod]
    public void BitAiAgentStatusBadgeShouldRespectClassStyleAndDisabled()
    {
        var component = RenderComponent<BitAiAgentStatusBadge>(p => p
            .Add(c => c.Status, BitAiAgentRunStatus.Running)
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px")
            .Add(c => c.Disabled, true));

        var root = component.Find(".bit-aiasb");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(component.Find(".bit-tag").ClassList.Contains("bit-dis"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiAgentStatusBadgeShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiAgentStatusBadge>(p => p
            .Add(c => c.Status, BitAiAgentRunStatus.Running)
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aiasb").GetAttribute("style");

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
    public void BitAiAgentStatusBadgeShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiAgentStatusBadge>(p => p
            .Add(c => c.Status, BitAiAgentRunStatus.Running)
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aiasb");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitAiAgentStatusBadgeShouldHandTheClassesAndStylesToItsParts()
    {
        var component = RenderComponent<BitAiAgentStatusBadge>(p => p
            .Add(c => c.Status, BitAiAgentRunStatus.Running)
            .Add(c => c.Classes, new() { Root = "c-root", Tag = new() { Root = "c-tag" } })
            .Add(c => c.Styles, new() { Root = "color: red", Tag = new() { Root = "color: blue" } }));

        var root = component.Find(".bit-aiasb");

        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
        Assert.IsTrue(component.Find(".bit-tag").ClassList.Contains("c-tag"));
        StringAssert.Contains(component.Find(".bit-tag").GetAttribute("style"), "color: blue");
    }

    [TestMethod]
    public void BitAiAgentStatusBadgeShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiAgentStatusBadgeParams { Class = "cascaded", Classes = new() { Tag = new() { Root = "cascaded-tag" } } }
            });
            p.AddChildContent<BitAiAgentStatusBadge>(c => c.Add(x => x.Status, BitAiAgentRunStatus.Running));
        });

        Assert.IsTrue(component.Find(".bit-aiasb").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-tag").ClassList.Contains("cascaded-tag"));
    }

    [TestMethod]
    public void BitAiAgentStatusBadgeParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiAgentStatusBadge", BitAiAgentStatusBadgeParams.ParamName);
        Assert.AreEqual(BitAiAgentStatusBadgeParams.ParamName, new BitAiAgentStatusBadgeParams().Name);
    }
}
