using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Layouts.Header;

[TestClass]
public class BitHeaderTests : BunitTestContext
{
    [TestMethod]
    public void BitHeaderShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitHeader>();

        component.MarkupMatches(@"
<header class=""bit-hdr"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [DataTestMethod]
    [DataRow("<div>Title</div>")]
    [DataRow("I'm a Header")]
    public void BitHeaderShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches($@"
<header class=""bit-hdr"" id:ignore>
    <div class=""bit-hdr-gut"">
        {childContent}
    </div>
</header>");
    }

    [DataTestMethod]
    [DataRow(50)]
    [DataRow(120)]
    public void BitHeaderShouldRespectHeight(int height)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Height, height);
        });

        component.MarkupMatches($@"
<header style=""height:{height}px"" class=""bit-hdr"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [TestMethod]
    public void BitHeaderShouldRespectFixed()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Fixed, true);
        });

        var header = component.Find(".bit-hdr");

        Assert.IsTrue(header.ClassList.Contains("bit-hdr-fix"));
    }

    [DataTestMethod]
    [DataRow("padding: 1rem;")]
    [DataRow(null)]
    public void BitHeaderShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (!string.IsNullOrEmpty(style))
        {
            component.MarkupMatches($@"
<header style=""{style}"" class=""bit-hdr"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
        }
        else
        {
            component.MarkupMatches(@"
<header class=""bit-hdr"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
        }
    }

    [DataTestMethod]
    [DataRow("test-class")]
    [DataRow(null)]
    public void BitHeaderShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = !string.IsNullOrEmpty(@class) ? $" {@class}" : string.Empty;

        component.MarkupMatches($@"
<header class=""bit-hdr{cssClass}"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [DataTestMethod]
    [DataRow("test-id")]
    [DataRow(null)]
    public void BitHeaderShouldRespectId(string id)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id ?? component.Instance.UniqueId.ToString();

        component.MarkupMatches($@"
<header id=""{expectedId}"" class=""bit-hdr"">
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [DataTestMethod]
    [DataRow(BitDir.Rtl)]
    [DataRow(BitDir.Ltr)]
    [DataRow(BitDir.Auto)]
    [DataRow(null)]
    public void BitHeaderShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : string.Empty;
            component.MarkupMatches($@"
<header dir=""{dir.Value.ToString().ToLower()}"" class=""bit-hdr{cssClass}"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
        }
        else
        {
            component.MarkupMatches(@"
<header class=""bit-hdr"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
        }
    }

    [TestMethod]
    public void BitHeaderShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitHeaderHtmlAttributesTest>();

        component.MarkupMatches(@"
<header data-val-test=""bit"" class=""bit-hdr"" id:ignore>
    <div class=""bit-hdr-gut"">
        I'm a header
    </div>
</header>");
    }
}
