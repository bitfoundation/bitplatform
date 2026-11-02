using System.Text.Json;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ToolResultView;

[TestClass]
public class BitAiToolResultViewTests : BitAiTestBase
{
    [TestMethod]
    public void ATextualResultIsShownAsItWasWritten()
    {
        var part = new BitAiToolResultPart("c1", BitAiToolOutcome.FromText("line one\nline two")) { ToolName = "search" };

        var component = RenderComponent<BitAiToolResultView>(p => p.Add(c => c.Part, part));

        Assert.AreEqual("search", component.Find(".bit-aitrv-ttl").TextContent);
        Assert.AreEqual("line one\nline two", component.Find(".bit-aitrv-txt").TextContent);
        Assert.AreEqual("true", component.Find(".bit-aitrv-ico").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void AResultWithoutAToolNameIsTitledResult()
    {
        var part = new BitAiToolResultPart("c1", BitAiToolOutcome.FromText("x"));

        var component = RenderComponent<BitAiToolResultView>(p => p.Add(c => c.Part, part));

        Assert.AreEqual("Result", component.Find(".bit-aitrv-ttl").TextContent);
    }

    [TestMethod]
    public void AStructuredResultIsShownAsAJsonTree()
    {
        var part = new BitAiToolResultPart("c1", BitAiToolOutcome.Success(JsonDocument.Parse("""{"temp":12}""").RootElement.Clone()));

        var component = RenderComponent<BitAiToolResultView>(p => p.Add(c => c.Part, part));

        Assert.HasCount(1, component.FindComponents<BitAiJsonView>());
        StringAssert.Contains(component.Markup, "temp");
    }

    [TestMethod]
    public void AFailedResultIsShownAsACompactError()
    {
        var part = new BitAiToolResultPart("c1", BitAiToolOutcome.Failure("permission denied"));

        var component = RenderComponent<BitAiToolResultView>(p => p.Add(c => c.Part, part));

        var error = component.FindComponent<BitAiErrorView>();
        Assert.IsTrue(error.Instance.Compact);
        StringAssert.Contains(component.Markup, "permission denied");
    }

    [TestMethod]
    public void TheRootShouldRenderTheExpectedClassesAndAttributes()
    {
        var component = RenderComponent<BitAiToolResultView>(p => p
            .Add(c => c.Part, new BitAiToolResultPart("c1", BitAiToolOutcome.FromText("x")))
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Id, "result")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.Classes, new() { Root = "root-class", Body = "body-class" })
            .Add(c => c.Styles, new() { Root = "padding:2px", Text = "color:red" }));

        var root = component.Find(".bit-aitrv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("result", root.Id);
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
        Assert.IsTrue(component.Find(".bit-aitrv-bdy").ClassList.Contains("body-class"));
        Assert.AreEqual("color:red", component.Find(".bit-aitrv-txt").GetAttribute("style"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void TheRootShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiToolResultView>(p => p
            .Add(c => c.Part, new BitAiToolResultPart("c1", BitAiToolOutcome.FromText("x")))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aitrv").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }
}
