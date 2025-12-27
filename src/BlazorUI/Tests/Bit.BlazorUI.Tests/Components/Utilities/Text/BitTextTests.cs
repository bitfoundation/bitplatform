using System.Globalization;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Text;

[TestClass]
public class BitTextTests : BunitTestContext
{
    private static readonly Dictionary<BitTypography, string> VariantMapping = new()
    {
        { BitTypography.Body1, "p" },
        { BitTypography.Body2, "p" },
        { BitTypography.Button, "span" },
        { BitTypography.Caption1, "span" },
        { BitTypography.Caption2, "span" },
        { BitTypography.H1, "h1" },
        { BitTypography.H2, "h2" },
        { BitTypography.H3, "h3" },
        { BitTypography.H4, "h4" },
        { BitTypography.H5, "h5" },
        { BitTypography.H6, "h6" },
        { BitTypography.Inherit, "p" },
        { BitTypography.Overline, "span" },
        { BitTypography.Subtitle1, "h6" },
        { BitTypography.Subtitle2, "h6" }
    };

    [DataTestMethod]
    public void BitTextShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [DataTestMethod,
         DataRow(BitTypography.H1),
         DataRow(BitTypography.H2),
         DataRow(BitTypography.H3),
         DataRow(BitTypography.H4),
         DataRow(BitTypography.H5),
         DataRow(BitTypography.H6),
         DataRow(BitTypography.Subtitle1),
         DataRow(BitTypography.Subtitle2),
         DataRow(BitTypography.Body1),
         DataRow(BitTypography.Body2),
         DataRow(BitTypography.Button),
         DataRow(BitTypography.Caption1),
         DataRow(BitTypography.Caption2),
         DataRow(BitTypography.Overline),
         DataRow(BitTypography.Inherit),
    ]
    public void BitTextShouldRespectVariant(BitTypography variant)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Typography, variant);
        });

        var el = VariantMapping[variant];
        var cssClass = $"bit-txt-{variant.ToString().ToLower(CultureInfo.InvariantCulture)}";

        component.MarkupMatches(@$"<{el} class=""bit-txt {cssClass}"" id:ignore></{el}>");
    }

    [DataTestMethod,
        DataRow("h1"),
        DataRow("div"),
        DataRow(null)
    ]
    public void BitTextShouldRespectElement(string element)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var defaultVariant = BitTypography.Subtitle1;
        var el = element is null ? VariantMapping[defaultVariant] : element;

        component.MarkupMatches(@$"<{el} class=""bit-txt bit-txt-subtitle1"" id:ignore></{el}>");

    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectNoWrap(bool noWrap)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.NoWrap, noWrap);
        });

        var cssClass = noWrap ? " bit-txt-nowrap" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectNoWrapChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.NoWrap, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-nowrap"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectGutter(bool gutter)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Gutter, gutter);
        });

        var cssClass = gutter ? " bit-txt-gutter" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectGutterChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Gutter, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-gutter"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-dis"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitTextShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<h6 style=""{style}"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
        else
        {
            component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
    }

    [DataTestMethod]
    public void BitTextShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        component.MarkupMatches(@$"<h6 style=""padding: 1rem;"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }


    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitTextShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 test-class"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitTextShouldRespectId(string id)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<h6 id=""{expectedId}"" class=""bit-txt bit-txt-subtitle1""></h6>");
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitTextShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<h6 dir=""{dir.Value.ToString().ToLower()}"" class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
        }
        else
        {
            component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
    }

    [DataTestMethod]
    public void BitTextShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<h6 dir=""ltr"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitTextShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<h6 style=""visibility: hidden;"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<h6 style=""display: none;"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
                break;
        }
    }

    [DataTestMethod]
    public void BitTextShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<h6 style=""display: none;"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitTextShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<h6 aria-label=""{ariaLabel}"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
        else
        {
            component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitTextShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore>{childContent}</h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitTextHtmlAttributesTest>();

        component.MarkupMatches(@"<h6 data-val-test=""bit"" class=""bit-txt bit-txt-subtitle1"" id:ignore>I'm a text</h6>");
    }
}
