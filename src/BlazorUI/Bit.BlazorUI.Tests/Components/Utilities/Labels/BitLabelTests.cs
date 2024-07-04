using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Labels;

[TestClass]
public class BitLabelTests : BunitTestContext
{
    [DataTestMethod]
    public void BitLabelShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label id:regex="".+"" class=""bit-lbl""></label>");
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

        var cssClass = isEnabled ? "bit-lbl" : "bit-lbl bit-dis";

        component.MarkupMatches(@$"<label id:regex="".+"" class=""{cssClass}""></label>");
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

        var cssClass = required ? "bit-lbl bit-lbl-req" : "bit-lbl";

        component.MarkupMatches(@$"<label id:regex="".+"" class=""{cssClass}""></label>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasNoValue())
        {
            component.MarkupMatches(@"<label id:regex="".+"" class=""bit-lbl""></label>");
        }
        else
        {
            component.MarkupMatches(@$"<label id:regex="".+"" class=""bit-lbl"" style=""{style}""></label>");
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

        var cssClass = @class.HasValue() ? $"bit-lbl {@class}" : "bit-lbl";

        component.MarkupMatches(@$"<label id:regex="".+"" class=""{cssClass}""></label>");
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

        if (id.HasNoValue())
        {
            component.MarkupMatches(@$"<label id=""{component.Instance.UniqueId}"" class=""bit-lbl""></label>");
        }
        else
        {
            component.MarkupMatches(@$"<label id=""{id}"" class=""bit-lbl""></label>");
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


        if (@for.HasNoValue())
        {
            component.MarkupMatches(@"<label id:regex="".+"" class=""bit-lbl""></label>");
        }
        else
        {
            component.MarkupMatches(@$"<label id:regex="".+"" class=""bit-lbl"" for=""{@for}""></label>");
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

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? "bit-lbl bit-rtl" : "bit-lbl";
            component.MarkupMatches(@$"<label id:regex="".+"" class=""{cssClass}"" dir=""{dir.Value.ToString().ToLower()}""></label>");
        }
        else
        {
            component.MarkupMatches(@"<label id:regex="".+"" class=""bit-lbl""></label>");
        }
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitLabelShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<label id:regex="".+"" class=""bit-lbl""></label>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<label id:regex="".+"" class=""bit-lbl"" style=""visibility: hidden;""></label>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<label id:regex="".+"" class=""bit-lbl"" style=""display: none;""></label>");
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

        component.MarkupMatches(@$"<label id:regex="".+"" class=""bit-lbl"">{childContent}</label>");
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<label id:regex="".+"" class=""bit-lbl"" aria-label=""{ariaLabel}""></label>");
        }
        else
        {
            component.MarkupMatches(@"<label id:regex="".+"" class=""bit-lbl""></label>");
        }
    }

    [DataTestMethod]
    public void BitLabelShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitLabelHtmlAttributesTest>();

        component.MarkupMatches(@"<label data-val-test=""bit"" id:regex="".+"" class=""bit-lbl"">I'm a label</label>");
    }
}
