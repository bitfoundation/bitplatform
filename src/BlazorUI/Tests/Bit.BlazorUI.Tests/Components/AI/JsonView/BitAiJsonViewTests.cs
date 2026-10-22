using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.JsonView;

[TestClass]
public class BitAiJsonViewTests : BitAiTestBase
{
    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private string[] ToggleStates(IRenderedComponent<BitAiJsonView> view)
        => view.FindAll("button.bit-aijsv-tgl").Select(b => b.GetAttribute("aria-expanded")!).ToArray();



    [TestMethod]
    public void TogglingOneNodeInAJsonTreeLeavesASameNamedNodeInAnotherBranchAlone()
    {
        var view = RenderComponent<BitAiJsonView>(p => p
            .Add(c => c.Value, Json("""{"a":{"x":{"y":1}},"b":{"x":{"y":2}}}"""))
            .Add(c => c.DefaultExpandDepth, 2));

        CollectionAssert.AreEqual(new[] { "true", "true", "false", "true", "false" }, ToggleStates(view));

        view.FindAll("button.bit-aijsv-tgl")[2].Click();
        CollectionAssert.AreEqual(new[] { "true", "true", "true", "true", "false" }, ToggleStates(view));

        view.FindAll("button.bit-aijsv-tgl")[1].Click();
        CollectionAssert.AreEqual(new[] { "true", "false", "true", "false" }, ToggleStates(view));
    }

    [TestMethod]
    public void TogglingOneArrayItemInAJsonTreeLeavesItsSiblingsAlone()
    {
        var view = RenderComponent<BitAiJsonView>(p => p
            .Add(c => c.Value, Json("""[{"a":1},{"a":2},{"a":3}]"""))
            .Add(c => c.DefaultExpandDepth, 1));

        // The root is open; each item starts collapsed.
        CollectionAssert.AreEqual(new[] { "true", "false", "false", "false" }, ToggleStates(view));

        view.FindAll("button.bit-aijsv-tgl")[1].Click();

        CollectionAssert.AreEqual(new[] { "true", "true", "false", "false" }, ToggleStates(view));
    }

    [TestMethod]
    public void BitAiJsonViewShouldCollapseALargeContainerWhateverItsDepth()
    {
        var view = RenderComponent<BitAiJsonView>(p => p
            .Add(c => c.Value, Json("""{"items":[1,2,3,4]}"""))
            .Add(c => c.LargeContainerSize, 3));

        CollectionAssert.AreEqual(new[] { "true", "false" }, ToggleStates(view));
        StringAssert.Contains(view.FindAll("button.bit-aijsv-tgl")[1].TextContent, "[ 4 ]");
    }

    [TestMethod]
    public void BitAiJsonViewShouldRenderEachKindOfScalarWithItsOwnPart()
    {
        var view = RenderComponent<BitAiJsonView>(p => p
            .Add(c => c.Value, Json("""{"s":"text","n":1.5,"b":true,"z":null}""")));

        Assert.AreEqual("\"text\"", view.Find(".bit-aijsv-str").TextContent);
        Assert.AreEqual("1.5", view.Find(".bit-aijsv-num").TextContent);
        Assert.AreEqual("true", view.Find(".bit-aijsv-bln").TextContent);
        Assert.AreEqual("null", view.Find(".bit-aijsv-nul").TextContent);
        CollectionAssert.AreEqual(new[] { "\"s\"", "\"n\"", "\"b\"", "\"z\"" }, view.FindAll(".bit-aijsv-key").Select(k => k.TextContent).ToArray());
    }

    [TestMethod]
    public void BitAiJsonViewShouldNameEachToggleByTheHeadOfItsContainer()
    {
        var view = RenderComponent<BitAiJsonView>(p => p
            .Add(c => c.Value, Json("""{"user":{"name":"Ada"}}""")));

        var toggles = view.FindAll("button.bit-aijsv-tgl");

        Assert.AreEqual("button", toggles[1].GetAttribute("type"));
        StringAssert.Contains(toggles[1].TextContent, "\"user\"");
        // The chevron is a picture of the state aria-expanded already gives.
        Assert.AreEqual("true", toggles[1].QuerySelector("i")!.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitAiJsonViewShouldTurnTheChevronOfAnOpenContainerDown()
    {
        var view = RenderComponent<BitAiJsonView>(p => p
            .Add(c => c.Value, Json("""{"a":{"b":{"c":1}}}"""))
            .Add(c => c.DefaultExpandDepth, 1));

        var icons = view.FindAll(".bit-aijsv-tic");

        Assert.IsTrue(icons[0].ClassList.Contains("bit-aijsv-top"));
        Assert.IsTrue(icons[1].ClassList.Contains("bit-aijsv-tcl"));
        Assert.IsTrue(icons[1].ClassList.Contains("bit-icon--ChevronRight"));
    }

    [TestMethod]
    public void BitAiJsonViewShouldRenderNothingInsideTheRootWithoutAValue()
    {
        var view = RenderComponent<BitAiJsonView>();

        Assert.AreEqual(0, view.Find(".bit-aijsv").ChildElementCount);
    }

    [TestMethod]
    public void BitAiJsonViewShouldDisableItsTogglesAndIgnoreTheirClicksWhileDisabled()
    {
        var view = RenderComponent<BitAiJsonView>(p => p
            .Add(c => c.Value, Json("""{"a":{"b":1}}"""))
            .Add(c => c.DefaultExpandDepth, 1)
            .Add(c => c.Disabled, true));

        var toggle = view.FindAll("button.bit-aijsv-tgl")[1];

        Assert.IsTrue(toggle.HasAttribute("disabled"));
        Assert.AreEqual("true", toggle.GetAttribute("aria-disabled"));
        Assert.IsTrue(view.Find(".bit-aijsv").ClassList.Contains("bit-dis"));

        toggle.Click();

        CollectionAssert.AreEqual(new[] { "true", "false" }, ToggleStates(view));
    }

    [TestMethod]
    public void BitAiJsonViewShouldRenderTheRootAttributes()
    {
        var view = RenderComponent<BitAiJsonView>(p => p
            .Add(c => c.Value, Json("1"))
            .Add(c => c.Id, "tree")
            .Add(c => c.Class, "custom")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Dir, BitDir.Rtl));

        var root = view.Find(".bit-aijsv");

        Assert.AreEqual("tree", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiJsonViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var view = RenderComponent<BitAiJsonView>(p => p.Add(c => c.Visibility, visibility));

        var style = view.Find(".bit-aijsv").GetAttribute("style");

        if (expected is null) Assert.IsTrue(string.IsNullOrEmpty(style));
        else StringAssert.Contains(style, expected);
    }

    [TestMethod]
    public void BitAiJsonViewShouldApplyClassesAndStylesToItsParts()
    {
        var view = RenderComponent<BitAiJsonView>(p => p
            .Add(c => c.Value, Json("""{"o":{"s":"x"}}"""))
            .Add(c => c.Classes, new() { Root = "c-root", Toggle = "c-toggle", Key = "c-key", String = "c-string", Group = "c-group", Row = "c-row", ToggleIcon = "c-icon" })
            .Add(c => c.Styles, new() { String = "color:red" }));

        Assert.IsTrue(view.Find(".bit-aijsv").ClassList.Contains("c-root"));
        Assert.IsTrue(view.Find(".bit-aijsv-tgl").ClassList.Contains("c-toggle"));
        Assert.IsTrue(view.Find(".bit-aijsv-key").ClassList.Contains("c-key"));
        Assert.IsTrue(view.Find(".bit-aijsv-grp").ClassList.Contains("c-group"));
        Assert.IsTrue(view.Find(".bit-aijsv-row").ClassList.Contains("c-row"));
        Assert.IsTrue(view.Find(".bit-aijsv-tic").ClassList.Contains("c-icon"));
        Assert.IsTrue(view.Find(".bit-aijsv-str").ClassList.Contains("c-string"));
        Assert.AreEqual("color:red", view.Find(".bit-aijsv-str").GetAttribute("style"));
    }

    [TestMethod]
    public void BitAiJsonViewShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiJsonViewParams { Class = "cascaded", DefaultExpandDepth = 0, Classes = new() { Toggle = "cascaded-toggle" } }
            });
            p.AddChildContent<BitAiJsonView>(c => c.Add(x => x.Value, Json("""{"a":1}""")));
        });

        Assert.IsTrue(component.Find(".bit-aijsv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aijsv-tgl").ClassList.Contains("cascaded-toggle"));
        Assert.AreEqual("false", component.Find(".bit-aijsv-tgl").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitAiJsonViewShouldKeepItsOwnValueOverACascadedOne()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams> { new BitAiJsonViewParams { DefaultExpandDepth = 0 } });
            p.AddChildContent<BitAiJsonView>(c => c.Add(x => x.Value, Json("""{"a":1}""")).Add(x => x.DefaultExpandDepth, 1));
        });

        Assert.AreEqual("true", component.Find(".bit-aijsv-tgl").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitAiJsonViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiJsonView", BitAiJsonViewParams.ParamName);
        Assert.AreEqual(BitAiJsonViewParams.ParamName, new BitAiJsonViewParams().Name);
    }
}
