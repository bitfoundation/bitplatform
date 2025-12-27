using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Footer;

[TestClass]
public class BitFooterTests : BunitTestContext
{
    [TestMethod]
    public void BitFooterShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitFooter>();

        component.MarkupMatches(@"
<footer class=""bit-ftr"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [DataTestMethod]
    [DataRow("<div>Footer Content</div>")]
    [DataRow("I'm a Footer")]
    public void BitFooterShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches($@"
<footer class=""bit-ftr"" id:ignore>
    <div class=""bit-ftr-gut"">
        {childContent}
    </div>
</footer>");
    }

    [DataTestMethod]
    [DataRow(40)]
    [DataRow(100)]
    public void BitFooterShouldRespectHeight(int height)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Height, height);
        });

        component.MarkupMatches($@"
<footer style=""height:{height}px"" class=""bit-ftr"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [TestMethod]
    public void BitFooterShouldRespectFixed()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Fixed, true);
        });

        var footer = component.Find(".bit-ftr");

        Assert.IsTrue(footer.ClassList.Contains("bit-ftr-fix"));
    }

    [DataTestMethod]
    [DataRow("margin: 10px;")]
    [DataRow(null)]
    public void BitFooterShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (string.IsNullOrEmpty(style) is false)
        {
            component.MarkupMatches($@"
<footer style=""{style}"" class=""bit-ftr"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
        }
        else
        {
            component.MarkupMatches(@"
<footer class=""bit-ftr"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
        }
    }

    [DataTestMethod]
    [DataRow("footer-class")]
    [DataRow(null)]
    public void BitFooterShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = !string.IsNullOrEmpty(@class) ? $" {@class}" : string.Empty;

        component.MarkupMatches($@"
<footer class=""bit-ftr{cssClass}"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [DataTestMethod]
    [DataRow("footer-id")]
    [DataRow(null)]
    public void BitFooterShouldRespectId(string id)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id ?? component.Instance.UniqueId.ToString();

        component.MarkupMatches($@"
<footer id=""{expectedId}"" class=""bit-ftr"">
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [DataTestMethod]
    [DataRow(BitDir.Rtl)]
    [DataRow(BitDir.Ltr)]
    [DataRow(BitDir.Auto)]
    [DataRow(null)]
    public void BitFooterShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : string.Empty;
            component.MarkupMatches($@"
<footer dir=""{dir.Value.ToString().ToLower()}"" class=""bit-ftr{cssClass}"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
        }
        else
        {
            component.MarkupMatches(@"
<footer class=""bit-ftr"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
        }
    }

    [TestMethod]
    public void BitFooterShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitFooterHtmlAttributesTest>();

        component.MarkupMatches(@"
<footer data-val-test=""bit"" class=""bit-ftr"" id:ignore>
    <div class=""bit-ftr-gut"">
        I'm a footer
    </div>
</footer>");
    }
}
