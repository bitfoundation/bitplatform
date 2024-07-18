using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Element;

[TestClass]
public class BitElementTests : BunitTestContext
{
    [DataTestMethod]
    public void BitElementShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
    }

    [DataTestMethod,
         DataRow("div"),
         DataRow("button"),
         DataRow("input"),
         DataRow("a"),
         DataRow(null)
    ]
    public void BitElementShouldRespectElement(string element)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var el = element ?? "div";

        component.MarkupMatches(@$"<{el} class=""bit-elm"" id:ignore></{el}>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitElementShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div class=""bit-elm{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitElementShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div class=""bit-elm bit-dis"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitElementShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-elm"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitElementShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        component.MarkupMatches(@"<div style=""padding: 1rem;"" class=""bit-elm"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitElementShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div class=""bit-elm{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitElementShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        component.MarkupMatches(@"<div class=""bit-elm test-class"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitElementShouldRespectId(string id)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-elm""></div>");
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitElementShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-elm{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitElementShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div dir=""ltr"" class=""bit-elm"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitElementShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<div style=""visibility: hidden;"" class=""bit-elm"" id:ignore></div>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<div style=""display: none;"" class=""bit-elm"" id:ignore></div>");
                break;
        }
    }

    [DataTestMethod]
    public void BitElementShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<div style=""display: none;"" class=""bit-elm"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitElementShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<div aria-label=""{ariaLabel}"" class=""bit-elm"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
        }
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitElementShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches(@$"<div class=""bit-elm"" id:ignore>{childContent}</label>");
    }

    [DataTestMethod]
    public void BitElementShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitElementHtmlAttributesTest>();

        component.MarkupMatches(@"<div data-val-test=""bit"" class=""bit-elm"" id:ignore>I'm a element</div>");
    }
}
