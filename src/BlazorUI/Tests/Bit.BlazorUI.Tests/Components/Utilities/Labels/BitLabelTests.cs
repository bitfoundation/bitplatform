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

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
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

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [DataTestMethod]
    public void BitLabelShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-dis"" id:ignore></label>");
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

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [DataTestMethod]
    public void BitLabelShouldRespectRequiredChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Required, true);
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-req"" id:ignore></label>");
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

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<label style=""{style}"" class=""bit-lbl"" id:ignore></label>");
        }
        else
        {
            component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
        }
    }

    [DataTestMethod]
    public void BitLabelShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        component.MarkupMatches(@"<label style=""padding: 1rem;"" class=""bit-lbl"" id:ignore></label>");
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

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [DataTestMethod]
    public void BitLabelShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        component.MarkupMatches(@"<label class=""bit-lbl test-class"" id:ignore></label>");
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

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId;

        component.MarkupMatches(@$"<label id=""{expectedId}"" class=""bit-lbl""></label>");
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

        if (@for.HasValue())
        {
            component.MarkupMatches(@$"<label for=""{@for}"" class=""bit-lbl"" id:ignore></label>");
        }
        else
        {
            component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
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
            component.MarkupMatches(@$"<label dir=""{dir.Value.ToString().ToLower()}"" class=""{cssClass}"" id:ignore></label>");
        }
        else
        {
            component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
        }
    }

    [DataTestMethod]
    public void BitLabelShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<label dir=""ltr"" class=""bit-lbl"" id:ignore></label>");
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
                component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<label style=""visibility: hidden;"" class=""bit-lbl"" id:ignore></label>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<label style=""display: none;"" class=""bit-lbl"" id:ignore></label>");
                break;
        }
    }

    [DataTestMethod]
    public void BitLabelShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<label style=""display: none;"" class=""bit-lbl"" id:ignore></label>");
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

        component.MarkupMatches(@$"<label class=""bit-lbl"" id:ignore>{childContent}</label>");
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
            component.MarkupMatches(@$"<label aria-label=""{ariaLabel}"" class=""bit-lbl"" id:ignore></label>");
        }
        else
        {
            component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
        }
    }

    [DataTestMethod]
    public void BitLabelShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitLabelHtmlAttributesTest>();

        component.MarkupMatches(@"<label data-val-test=""bit"" class=""bit-lbl"" id:ignore>I'm a label</label>");
    }
}
