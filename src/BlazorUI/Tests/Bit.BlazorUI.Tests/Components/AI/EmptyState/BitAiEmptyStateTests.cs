using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.EmptyState;

[TestClass]
public class BitAiEmptyStateTests : BitAiTestBase
{
    [TestMethod]
    public void BitAiEmptyStateShouldGreetWithTheStringTable()
    {
        var component = RenderComponent<BitAiEmptyState>();

        Assert.AreEqual("How can I help?", component.Find(".bit-aiems-ttl").TextContent);
        Assert.AreEqual("Ask a question to get started.", component.Find(".bit-aiems-bdy").TextContent.Trim());
        Assert.IsEmpty(component.FindAll(".bit-aiems-sgs"));
    }

    [TestMethod]
    public void BitAiEmptyStateShouldRespectTitleAndBody()
    {
        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Title, "Ask me anything")
            .Add(c => c.Body, "I know a lot about cats."));

        Assert.AreEqual("Ask me anything", component.Find(".bit-aiems-ttl").TextContent);
        Assert.AreEqual("I know a lot about cats.", component.Find(".bit-aiems-bdy").TextContent.Trim());
    }

    [TestMethod]
    public void BitAiEmptyStateShouldRenderTheChildContentInPlaceOfTheBody()
    {
        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Body, "unused")
            .AddChildContent("<em class=\"custom\">Custom</em>"));

        Assert.IsNotNull(component.Find(".bit-aiems-bdy > em.custom"));
        Assert.IsFalse(component.Find(".bit-aiems-bdy").TextContent.Contains("unused"));
    }

    [TestMethod]
    public void BitAiEmptyStateShouldShowASparkleByDefault()
    {
        var component = RenderComponent<BitAiEmptyState>();

        var icon = component.Find(".bit-ico.bit-aiems-ico");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--AutoEnhanceOn"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitAiEmptyStateShouldRespectIconNameAndIconTemplate()
    {
        var named = RenderComponent<BitAiEmptyState>(p => p.Add(c => c.IconName, "Robot"));

        Assert.IsTrue(named.Find(".bit-aiems-ico").ClassList.Contains("bit-icon--Robot"));

        var templated = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.IconName, "Robot")
            .Add(c => c.IconTemplate, "<img class=\"logo\" alt=\"\" />"));

        Assert.IsNotNull(templated.Find("div.bit-aiems-ico > img.logo"));
        Assert.IsEmpty(templated.FindAll(".bit-ico"));
    }

    [TestMethod]
    public void ASuggestionIsOfferedAsAOneTapStarter()
    {
        var chosen = new List<string>();

        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Suggestions, ["Plan a trip", "Explain a concept"])
            .Add(c => c.OnSuggestion, s => chosen.Add(s)));

        var buttons = component.FindAll(".bit-aiems-sgs > button");

        Assert.HasCount(2, buttons);
        Assert.AreEqual("Plan a trip", buttons[0].TextContent.Trim());

        buttons[1].Click();

        CollectionAssert.AreEqual(new[] { "Explain a concept" }, chosen);
    }

    [TestMethod]
    public void ADisabledEmptyStateOffersNoSuggestion()
    {
        var chosen = new List<string>();

        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Suggestions, ["Plan a trip"])
            .Add(c => c.Disabled, true)
            .Add(c => c.OnSuggestion, s => chosen.Add(s)));

        Assert.IsTrue(component.Find(".bit-aiems").ClassList.Contains("bit-dis"));

        var button = component.Find(".bit-aiems-sgs > button");
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));

        button.Click();

        Assert.IsEmpty(chosen);
    }

    [TestMethod]
    public void BitAiEmptyStateShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Suggestions, ["Plan a trip"])
            .Add(c => c.Classes, new() { Root = "custom-root", Icon = "custom-icon", Title = "custom-title", Body = "custom-body", Suggestions = "custom-suggestions", Suggestion = new() { Root = "custom-suggestion" } })
            .Add(c => c.Styles, new() { Root = "color: red;", Icon = "color: orange;", Title = "color: green;", Body = "color: blue;", Suggestions = "color: purple;" }));

        Assert.IsTrue(component.Find(".bit-aiems").ClassList.Contains("custom-root"));
        StringAssert.Contains(component.Find(".bit-aiems").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-aiems-ico").ClassList.Contains("custom-icon"));
        StringAssert.Contains(component.Find(".bit-aiems-ico").GetAttribute("style"), "color: orange;");
        Assert.IsTrue(component.Find(".bit-aiems-ttl").ClassList.Contains("custom-title"));
        Assert.AreEqual("color: green;", component.Find(".bit-aiems-ttl").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aiems-bdy").ClassList.Contains("custom-body"));
        Assert.AreEqual("color: blue;", component.Find(".bit-aiems-bdy").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aiems-sgs").ClassList.Contains("custom-suggestions"));
        Assert.AreEqual("color: purple;", component.Find(".bit-aiems-sgs").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aiems-sgs > button").ClassList.Contains("custom-suggestion"));
    }

    [TestMethod]
    public void BitAiEmptyStateShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: tomato;"));

        var root = component.Find(".bit-aiems");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "color: tomato;");
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiEmptyStateShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitAiEmptyState>(p => p.Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aiems").GetAttribute("style");

        if (expectedStyle.Length == 0)
        {
            Assert.IsTrue(style is null || style.Contains("visibility:hidden") is false);
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "bit-rtl")]
    [DataRow(BitDir.Ltr, null)]
    public void BitAiEmptyStateShouldRespectDir(BitDir dir, string? expectedClass)
    {
        var component = RenderComponent<BitAiEmptyState>(p => p.Add(c => c.Dir, dir));

        var root = component.Find(".bit-aiems");

        Assert.AreEqual(dir.ToString().ToLower(), root.GetAttribute("dir"));
        Assert.AreEqual(expectedClass is not null, root.ClassList.Contains("bit-rtl"));
    }
}
