using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MarkdownView;

[TestClass]
public class BitAiMarkdownViewParamsTests : BitAiTestBase
{
    private IRenderedComponent<BitParams> RenderWithParams(BitAiMarkdownViewParams markdownParams, BitAiMarkdownViewClassStyles? ownClasses = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { markdownParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiMarkdownView>(0);
                builder.AddAttribute(1, nameof(BitAiMarkdownView.Document), BitAiMarkdownParser.Parse("text"));
                if (ownClasses is not null)
                {
                    builder.AddAttribute(2, nameof(BitAiMarkdownView.Classes), ownClasses);
                }
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAiMarkdownViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiMarkdownView", new BitAiMarkdownViewParams().Name);
    }

    [TestMethod]
    public void BitAiMarkdownViewShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiMarkdownViewParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root", Paragraph = "cascaded-par" },
            Styles = new() { Root = "margin: 1px" },
        });

        var root = component.Find(".bit-aimdv");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        Assert.IsTrue(component.Find(".bit-aimdv-par").ClassList.Contains("cascaded-par"));
    }

    [TestMethod]
    public void BitAiMarkdownViewShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(
            new BitAiMarkdownViewParams { Classes = new() { Root = "cascaded-root" } },
            new BitAiMarkdownViewClassStyles { Root = "own-root" });

        var root = component.Find(".bit-aimdv");

        Assert.IsTrue(root.ClassList.Contains("own-root"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-root"));
    }
}
