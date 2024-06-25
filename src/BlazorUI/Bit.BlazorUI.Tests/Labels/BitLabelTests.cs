using Bunit;
using AngleSharp.Css.Dom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Labels;

[TestClass]
public class BitLabelTests : BunitTestContext
{
    [DataTestMethod]
    public void BitLabelShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitLabel>();

        var bitLabel = component.Find(".bit-lbl");

        Assert.AreEqual("label", bitLabel.TagName, true);
        Assert.IsTrue(bitLabel.HasAttribute("id"));
        Assert.IsTrue(bitLabel.HasAttribute("class"));

        Assert.IsFalse(bitLabel.HasAttribute("style"));
        Assert.IsFalse(bitLabel.HasAttribute("dir"));

        Assert.AreEqual(string.Empty, bitLabel.TextContent);

        component.MarkupMatches(@$"<label id:regex=""{GUID_PATTERN}"" class=""bit-lbl""></label>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLabelShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitLabel = component.Find(".bit-lbl");

        if (isEnabled)
        {
            Assert.IsFalse(bitLabel.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitLabel.ClassList.Contains("bit-dis"));
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLabelShouldRespectRequired(bool required)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Required, required);
        });

        var bitLabel = component.Find(".bit-lbl");

        if (required)
        {
            Assert.IsTrue(bitLabel.ClassList.Contains("bit-lbl-req"));
        }
        else
        {
            Assert.IsFalse(bitLabel.ClassList.Contains("bit-lbl-req"));
        }
    }

    [DataTestMethod,
        DataRow("14px"),
        DataRow("1rem"),
        DataRow("6em"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectStyle(string size)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            if (size.HasValue())
            {
                parameters.Add(p => p.Style, $"font-size:{size};");
            }
        });

        var bitLabel = component.Find(".bit-lbl");

        var fontSize = bitLabel.GetStyle().GetFontSize();

        if (size.HasValue())
        {
            Assert.IsTrue(bitLabel.HasAttribute("style"));

            Assert.AreEqual(size, fontSize);
        }
        else
        {
            Assert.AreEqual(string.Empty, fontSize);
            Assert.IsFalse(bitLabel.HasAttribute("style"));
        }
    }

    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var bitLabel = component.Find(".bit-lbl");

        component.MarkupMatches(@$"<label id:ignore class=""bit-lbl{(@class.HasValue() ? " test-class" : null)}""></label>");

        if (@class.HasValue())
        {
            Assert.IsTrue(bitLabel.ClassList.Contains(@class));
        }
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectId(string id)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var bitLabel = component.Find(".bit-lbl");

        if (id.HasNoValue())
        {
            Assert.AreEqual(component.Instance.UniqueId.ToString(), bitLabel.Id);
            StringAssert.Matches(bitLabel.Id, new(GUID_PATTERN));
        }
        else
        {
            Assert.AreEqual(id, bitLabel.Id);
        }
    }

    [DataTestMethod,
        DataRow("test-for"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectFor(string @for)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.For, @for);
        });

        var bitLabel = component.Find(".bit-lbl");

        if (@for.HasNoValue())
        {
            Assert.IsFalse(bitLabel.HasAttribute("for"));
        }
        else
        {
            Assert.IsTrue(bitLabel.HasAttribute("for"));
            Assert.AreEqual(@for, bitLabel.GetAttribute("for"));
        }
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitLabelShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var bitLabel = component.Find(".bit-lbl");

        if (dir.HasValue)
        {
            Assert.IsTrue(bitLabel.HasAttribute("dir"));
            Assert.AreEqual(dir.Value.ToString().ToLower(), bitLabel.GetAttribute("dir"));
        }
        else
        {
            Assert.IsFalse(bitLabel.HasAttribute("dir"));
        }
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden),
        DataRow(null)
    ]
    public void BitLabelShouldRespectVisibility(BitVisibility? visibility)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            if (visibility.HasValue)
            {
                parameters.Add(p => p.Visibility, visibility.Value);
            }
        });

        var bitLabel = component.Find(".bit-lbl");

        switch (visibility)
        {
            case BitVisibility.Visible or null:
                Assert.IsFalse(bitLabel.HasAttribute("style"));
                break;
            case BitVisibility.Hidden:
                Assert.AreEqual("hidden", bitLabel.GetStyle().GetVisibility());
                break;
            case BitVisibility.Collapsed:
                Assert.AreEqual("none", bitLabel.GetStyle().GetDisplay());
                break;
        }
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        var bitLabel = component.Find(".bit-lbl");

        if (childContent.HasNoValue())
        {
            Assert.AreEqual(string.Empty, bitLabel.TextContent);
        }
        else
        {
            Assert.AreEqual(childContent, bitLabel.InnerHtml);
        }
    }

    [DataTestMethod,
        DataRow("data-val-bit", "ok"),
        DataRow("data-val-test", "bit")
    ]
    public void BitLabelShouldRespectHtmlAttributes(string name, string value)
    {
        var component = RenderComponent<BitLabel>(ComponentParameter.CreateParameter(name, value));

        var bitLabel = component.Find(".bit-lbl");

        Assert.IsTrue(bitLabel.HasAttribute(name));
        Assert.AreEqual(value, bitLabel.GetAttribute(name));
    }
}
