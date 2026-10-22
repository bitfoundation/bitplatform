using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.StructuredDataView;

[TestClass]
public class BitAiStructuredDataViewTests : BitAiTestBase
{
    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }



    [TestMethod]
    public void AnArrayOfObjectsBecomesATableWithTheUnionOfItsKeys()
    {
        var part = new BitAiStructuredDataPart(Json("""[{"name":"Ada"},{"name":"Alan","field":"logic"}]"""));

        var view = RenderComponent<BitAiStructuredDataView>(p => p.Add(c => c.Part, part));

        Assert.HasCount(1, view.FindComponents<BitAiDataTable>());
        var markup = view.Markup;
        StringAssert.Contains(markup, "<table");
        StringAssert.Contains(markup, "Name");
        StringAssert.Contains(markup, "Field");
        StringAssert.Contains(markup, "logic");
    }

    [TestMethod]
    public void AnObjectBecomesALabelledKeyValueList()
    {
        var part = new BitAiStructuredDataPart(Json("""{"firstName":"Ada","born":1815,"died":null}"""));

        var view = RenderComponent<BitAiStructuredDataView>(p => p.Add(c => c.Part, part));

        var list = view.Find(".bit-aisdv > dl.bit-aisdv-lst");
        Assert.AreEqual(3, list.QuerySelectorAll(".bit-aisdv-ent").Length);
        Assert.AreEqual("First Name", view.Find("dt.bit-aisdv-trm").TextContent);
        StringAssert.Contains(view.Markup, "1815");
        Assert.AreEqual("-", view.Find(".bit-aisdv-emp").TextContent);
    }

    [TestMethod]
    public void ANestedValueOfAnObjectIsATreeOpenedOneLevelDeep()
    {
        var part = new BitAiStructuredDataPart(Json("""{"address":{"city":"London","geo":{"lat":51}}}"""));

        var view = RenderComponent<BitAiStructuredDataView>(p => p.Add(c => c.Part, part));

        var tree = view.FindComponent<BitAiJsonView>();
        Assert.AreEqual(1, tree.Instance.DefaultExpandDepth);
    }

    [TestMethod]
    public void AScalarOrAMixedArrayBecomesAJsonTree()
    {
        var part = new BitAiStructuredDataPart(Json("""[1,"two",{"three":3}]"""));

        var view = RenderComponent<BitAiStructuredDataView>(p => p.Add(c => c.Part, part));

        Assert.HasCount(1, view.FindComponents<BitAiJsonView>());
        Assert.IsEmpty(view.FindComponents<BitAiDataTable>());
    }

    [TestMethod]
    public void ARegisteredSchemaRendererTakesOver()
    {
        Renderers.RegisterStructured("invoice", _ => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "invoice-card");
            builder.CloseElement();
        });

        var part = new BitAiStructuredDataPart(Json("""{"total":10}"""), "invoice");
        var view = RenderComponent<BitAiStructuredDataView>(p => p.Add(c => c.Part, part));

        Assert.IsNotNull(view.Find(".invoice-card"));
        Assert.HasCount(1, view.FindComponents<BitAiRendererBoundary>());
        Assert.IsEmpty(view.FindAll("dt"));
    }

    [TestMethod]
    public void PartialStructuredDataFillsInAsItArrives()
    {
        var part = new BitAiStructuredDataPart();
        part.Append("""[{"name":"Ada"},{"name":"Al""");

        var markup = RenderComponent<BitAiStructuredDataView>(p => p.Add(c => c.Part, part)).Markup;

        StringAssert.Contains(markup, "Ada");
        StringAssert.Contains(markup, "Al");
    }

    [TestMethod]
    public void AStreamingPartWithNothingYetShowsTheThinkingIndicator()
    {
        var part = new BitAiStructuredDataPart();

        var view = RenderComponent<BitAiStructuredDataView>(p => p.Add(c => c.Part, part));

        Assert.HasCount(1, view.FindComponents<BitAiThinkingIndicator>());
    }

    [TestMethod]
    public void BitAiStructuredDataViewShouldHumanizePropertyNames()
    {
        var part = new BitAiStructuredDataPart(Json("""{"firstName":1,"last_name":2,"id":3,"zip-code":4,"":5}"""));

        var view = RenderComponent<BitAiStructuredDataView>(p => p.Add(c => c.Part, part));

        CollectionAssert.AreEqual(new[] { "First Name", "Last name", "Id", "Zip code", "" },
                                  view.FindAll("dt.bit-aisdv-trm").Select(dt => dt.TextContent).ToArray());
    }

    [TestMethod]
    public void BitAiStructuredDataViewShouldRenderTheRootAndItsParts()
    {
        var part = new BitAiStructuredDataPart(Json("""{"a":null}"""));

        var view = RenderComponent<BitAiStructuredDataView>(p => p
            .Add(c => c.Part, part)
            .Add(c => c.Id, "s1")
            .Add(c => c.Class, "custom")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.Classes, new() { Root = "c-root", List = "c-list", Entry = "c-entry", Label = "c-label", Value = "c-value", Empty = "c-empty" })
            .Add(c => c.Styles, new() { Label = "color:red" }));

        var root = view.Find(".bit-aisdv");

        Assert.AreEqual("s1", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.IsTrue(view.Find(".bit-aisdv-lst").ClassList.Contains("c-list"));
        Assert.IsTrue(view.Find(".bit-aisdv-ent").ClassList.Contains("c-entry"));
        Assert.IsTrue(view.Find(".bit-aisdv-trm").ClassList.Contains("c-label"));
        Assert.AreEqual("color:red", view.Find(".bit-aisdv-trm").GetAttribute("style"));
        Assert.IsTrue(view.Find(".bit-aisdv-val").ClassList.Contains("c-value"));
        Assert.IsTrue(view.Find(".bit-aisdv-emp").ClassList.Contains("c-empty"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiStructuredDataViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var view = RenderComponent<BitAiStructuredDataView>(p => p
            .Add(c => c.Part, new BitAiStructuredDataPart(Json("1")))
            .Add(c => c.Visibility, visibility));

        var style = view.Find(".bit-aisdv").GetAttribute("style");

        if (expected is null) Assert.IsTrue(string.IsNullOrEmpty(style));
        else StringAssert.Contains(style, expected);
    }

    [TestMethod]
    public void BitAiStructuredDataViewShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiStructuredDataViewParams { Class = "cascaded", Classes = new() { List = "cascaded-list" } }
            });
            p.AddChildContent<BitAiStructuredDataView>(c => c.Add(x => x.Part, new BitAiStructuredDataPart(Json("""{"a":1}"""))));
        });

        Assert.IsTrue(component.Find(".bit-aisdv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aisdv-lst").ClassList.Contains("cascaded-list"));
    }

    [TestMethod]
    public void BitAiStructuredDataViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiStructuredDataView", BitAiStructuredDataViewParams.ParamName);
        Assert.AreEqual(BitAiStructuredDataViewParams.ParamName, new BitAiStructuredDataViewParams().Name);
    }
}
