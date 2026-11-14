using System.Collections.Generic;
using System.Linq;
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

        Assert.AreEqual("How can I help?", component.Find(".bit-ems-ttl").TextContent);
        Assert.AreEqual("Ask a question to get started.", component.Find(".bit-ems-dsc").TextContent.Trim());
        Assert.IsEmpty(component.FindAll(".bit-ems-con"));
    }

    [TestMethod]
    public void BitAiEmptyStateShouldRespectTitleAndDescription()
    {
        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Title, "Ask me anything")
            .Add(c => c.Description, "I know a lot about cats."));

        Assert.AreEqual("Ask me anything", component.Find(".bit-ems-ttl").TextContent);
        Assert.AreEqual("I know a lot about cats.", component.Find(".bit-ems-dsc").TextContent.Trim());
    }

    [TestMethod]
    public void BitAiEmptyStateShouldRenderItsTemplatesInPlaceOfItsTexts()
    {
        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Title, "unused title")
            .Add(c => c.Description, "unused")
            .Add(c => c.TitleTemplate, "<b class=\"heading\">Hi</b>")
            .Add(c => c.DescriptionTemplate, "<em class=\"custom\">Custom</em>"));

        Assert.IsNotNull(component.Find(".bit-ems-ttl > b.heading"));
        Assert.IsFalse(component.Find(".bit-ems-ttl").TextContent.Contains("unused title"));
        Assert.IsNotNull(component.Find(".bit-ems-dsc > em.custom"));
        Assert.IsFalse(component.Find(".bit-ems-dsc").TextContent.Contains("unused"));
    }

    [TestMethod]
    public void BitAiEmptyStateShouldBeDrawnByTheCoreEmptyState()
    {
        var component = RenderComponent<BitAiEmptyState>(p => p.Add(c => c.Suggestions, new List<string> { "Plan a trip" }));

        // One BitEmptyState inside the root of the AI one, holding every part; the suggestions are its actions.
        var core = component.Find(".bit-aiems > .bit-ems");
        CollectionAssert.AreEqual(new[] { "bit-ems-ico", "bit-ems-ttl", "bit-ems-dsc", "bit-ems-con" }, core.Children.Select(c => c.ClassList[0]).ToArray());
        Assert.AreEqual(1, component.FindAll(".bit-ems").Count);
    }

    [TestMethod]
    public void BitAiEmptyStateShouldMakeItsHeadingAHeadingOnlyWhenAsked()
    {
        var component = RenderComponent<BitAiEmptyState>();

        Assert.IsNull(component.Find(".bit-ems-ttl").GetAttribute("role"));

        component.Render(p => p.Add(c => c.HeadingLevel, 2));

        Assert.AreEqual("heading", component.Find(".bit-ems-ttl").GetAttribute("role"));
        Assert.AreEqual("2", component.Find(".bit-ems-ttl").GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitAiEmptyStateShouldShowASparkleByDefault()
    {
        var component = RenderComponent<BitAiEmptyState>();

        var icon = component.Find("i.bit-ems-ico");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--AutoEnhanceOn"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void AnEmptyIconShouldStillDrawTheSparkleFromThePackageSubset()
    {
        var component = RenderComponent<BitAiEmptyState>(p => p.Add(c => c.Icon, new BitIconInfo()));

        var icon = component.Find("i.bit-ems-ico");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--AutoEnhanceOn"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--AutoEnhanceOn"));
    }

    [TestMethod]
    public void BitAiEmptyStateShouldRespectIconNameAndIconTemplate()
    {
        var named = RenderComponent<BitAiEmptyState>(p => p.Add(c => c.IconName, "Robot"));

        Assert.IsTrue(named.Find(".bit-ems-ico").ClassList.Contains("bit-icon--Robot"));

        var templated = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.IconName, "Robot")
            .Add(c => c.IconTemplate, "<img class=\"logo\" alt=\"\" />"));

        Assert.IsNotNull(templated.Find("div.bit-ems-ico > img.logo"));
        Assert.IsEmpty(templated.FindAll("i.bit-ems-ico"));
    }

    [TestMethod]
    public void ASuggestionIsOfferedAsAOneTapStarter()
    {
        var chosen = new List<string>();

        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Suggestions, ["Plan a trip", "Explain a concept"])
            .Add(c => c.OnSuggestion, s => chosen.Add(s)));

        var buttons = component.FindAll(".bit-ems-con > button");

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

        var button = component.Find(".bit-ems-con > button");
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));

        button.Click();

        Assert.IsEmpty(chosen);
    }

    [TestMethod]
    public void BitAiEmptyStateShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitAiEmptyState>(p => p
            .Add(c => c.Suggestions, ["Plan a trip"])
            .Add(c => c.Classes, new() { Root = "custom-root", Icon = "custom-icon", Title = "custom-title", Description = "custom-body", Suggestions = "custom-suggestions", Suggestion = new() { Root = "custom-suggestion" } })
            .Add(c => c.Styles, new() { Root = "color: red;", Icon = "color: orange;", Title = "color: green;", Description = "color: blue;", Suggestions = "color: purple;" }));

        Assert.IsTrue(component.Find(".bit-aiems").ClassList.Contains("custom-root"));
        StringAssert.Contains(component.Find(".bit-aiems").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-ems-ico").ClassList.Contains("custom-icon"));
        StringAssert.Contains(component.Find(".bit-ems-ico").GetAttribute("style"), "color: orange;");
        Assert.IsTrue(component.Find(".bit-ems-ttl").ClassList.Contains("custom-title"));
        Assert.AreEqual("color: green;", component.Find(".bit-ems-ttl").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-ems-dsc").ClassList.Contains("custom-body"));
        Assert.AreEqual("color: blue;", component.Find(".bit-ems-dsc").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-ems-con").ClassList.Contains("custom-suggestions"));
        Assert.AreEqual("color: purple;", component.Find(".bit-ems-con").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-ems-con > button").ClassList.Contains("custom-suggestion"));
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
