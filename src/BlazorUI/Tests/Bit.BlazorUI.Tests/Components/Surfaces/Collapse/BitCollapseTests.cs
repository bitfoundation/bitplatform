using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Collapse;

[TestClass]
public class BitCollapseTests : BunitTestContext
{
    [TestMethod]
    public void BitCollapseShouldRenderChildContent()
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Hello Collapse</div>");
        });

        var collapse = component.Find(".bit-col");
        Assert.IsNotNull(collapse);
        Assert.IsTrue(collapse.ToMarkup().Contains("Hello Collapse"));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitCollapseExpandedStateShouldApplyCorrectClasses(bool expanded)
    {
        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Expanded, expanded);
            parameters.AddChildContent("<div>content</div>");
        });

        var content = component.Find(".bit-col-con");

        if (expanded)
        {
            Assert.IsTrue(content.ClassList.Contains("bit-col-cex"));
            Assert.IsFalse(content.ClassList.Contains("bit-col-cco"));
        }
        else
        {
            Assert.IsTrue(content.ClassList.Contains("bit-col-cco"));
            Assert.IsFalse(content.ClassList.Contains("bit-col-cex"));
        }
    }

    [TestMethod]
    public void BitCollapseShouldRespectStylesAndClassesParameters()
    {
        var classes = new BitCollapseClassStyles { Root = "root-class", Content = "content-class", Expanded = "expanded-class" };
        var styles = new BitCollapseClassStyles { Root = "width:1px", Content = "width:2px", Expanded = "width:3px" };

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
            parameters.Add(p => p.Expanded, true);
            parameters.AddChildContent("<div>content</div>");
        });

        var root = component.Find(".bit-col");
        Assert.IsTrue(root.ClassList.Contains("root-class"));

        var content = component.Find(".bit-col-con");
        Assert.IsTrue(content.ClassList.Contains("content-class"));
        Assert.IsTrue(root.ClassList.Contains("expanded-class"));

        Assert.IsTrue(root.GetAttribute("style").Contains("width:1px"));
        Assert.IsTrue(content.GetAttribute("style").Contains("width:2px"));
        Assert.IsTrue(root.GetAttribute("style").Contains("width:3px"));
    }
}
