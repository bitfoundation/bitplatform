using System.Globalization;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Typography;

[TestClass]
public class BitTypographyTests : BunitTestContext
{
    private static readonly Dictionary<BitTypographyVariant, string> VariantMapping = new()
    {
        { BitTypographyVariant.Body1, "p" },
        { BitTypographyVariant.Body2, "p" },
        { BitTypographyVariant.Button, "span" },
        { BitTypographyVariant.Caption1, "span" },
        { BitTypographyVariant.Caption2, "span" },
        { BitTypographyVariant.H1, "h1" },
        { BitTypographyVariant.H2, "h2" },
        { BitTypographyVariant.H3, "h3" },
        { BitTypographyVariant.H4, "h4" },
        { BitTypographyVariant.H5, "h5" },
        { BitTypographyVariant.H6, "h6" },
        { BitTypographyVariant.Inherit, "p" },
        { BitTypographyVariant.Overline, "span" },
        { BitTypographyVariant.Subtitle1, "h6" },
        { BitTypographyVariant.Subtitle2, "h6" }
    };

    [DataTestMethod]
    public void BitTypographyShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitTypography>();

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
    }

    [DataTestMethod,
         DataRow(BitTypographyVariant.H1),
         DataRow(BitTypographyVariant.H2),
         DataRow(BitTypographyVariant.H3),
         DataRow(BitTypographyVariant.H4),
         DataRow(BitTypographyVariant.H5),
         DataRow(BitTypographyVariant.H6),
         DataRow(BitTypographyVariant.Subtitle1),
         DataRow(BitTypographyVariant.Subtitle2),
         DataRow(BitTypographyVariant.Body1),
         DataRow(BitTypographyVariant.Body2),
         DataRow(BitTypographyVariant.Button),
         DataRow(BitTypographyVariant.Caption1),
         DataRow(BitTypographyVariant.Caption2),
         DataRow(BitTypographyVariant.Overline),
         DataRow(BitTypographyVariant.Inherit),
    ]
    public void BitTypographyShouldRespectVariant(BitTypographyVariant variant)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Variant, variant);
        });

        var el = VariantMapping[variant];
        var cssClass = $"bit-tpg-{variant.ToString().ToLower(CultureInfo.InvariantCulture)}";

        component.MarkupMatches(@$"<{el} class=""bit-tpg {cssClass}"" id:ignore></{el}>");
    }

    [DataTestMethod,
        DataRow("h1"),
        DataRow("div"),
        DataRow(null)
    ]
    public void BitTypographyShouldRespectElement(string element)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var defaultVariant = BitTypographyVariant.Subtitle1;
        var el = element is null ? VariantMapping[defaultVariant] : element;

        component.MarkupMatches(@$"<{el} class=""bit-tpg bit-tpg-subtitle1"" id:ignore></{el}>");

    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTypographyShouldRespectNoWrap(bool noWrap)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.NoWrap, noWrap);
        });

        var cssClass = noWrap ? " bit-tpg-nowrap" : null;

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTypographyShouldRespectNoWrapChangingAfterRender()
    {
        var component = RenderComponent<BitTypography>();

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.NoWrap, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1 bit-tpg-nowrap"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTypographyShouldRespectGutter(bool gutter)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Gutter, gutter);
        });

        var cssClass = gutter ? " bit-tpg-gutter" : null;

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTypographyShouldRespectGutterChangingAfterRender()
    {
        var component = RenderComponent<BitTypography>();

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Gutter, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1 bit-tpg-gutter"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTypographyShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTypographyShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitTypography>();

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1 bit-dis"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitTypographyShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<h6 style=""{style}"" class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
        }
        else
        {
            component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
        }
    }

    [DataTestMethod]
    public void BitTypographyShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitTypography>();

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        component.MarkupMatches(@$"<h6 style=""padding: 1rem;"" class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
    }


    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitTypographyShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTypographyShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitTypography>();

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1 test-class"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitTypographyShouldRespectId(string id)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<h6 id=""{expectedId}"" class=""bit-tpg bit-tpg-subtitle1""></h6>");
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitTypographyShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<h6 dir=""{dir.Value.ToString().ToLower()}"" class=""bit-tpg bit-tpg-subtitle1{cssClass}"" id:ignore></h6>");
        }
        else
        {
            component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
        }
    }

    [DataTestMethod]
    public void BitTypographyShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitTypography>();

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<h6 dir=""ltr"" class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitTypographyShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<h6 style=""visibility: hidden;"" class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<h6 style=""display: none;"" class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
                break;
        }
    }

    [DataTestMethod]
    public void BitTypographyShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitTypography>();

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<h6 style=""display: none;"" class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitTypographyShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<h6 aria-label=""{ariaLabel}"" class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
        }
        else
        {
            component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
        }
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitTypographyShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitTypography>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore>{childContent}</h6>");
    }

    [DataTestMethod]
    public void BitTypographyShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitTypographyHtmlAttributesTest>();

        component.MarkupMatches(@"<h6 data-val-test=""bit"" class=""bit-tpg bit-tpg-subtitle1"" id:ignore>I'm a typography</h6>");
    }
}
