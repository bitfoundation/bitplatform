using System.Globalization;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Text;

[TestClass]
public class BitTextTests : BunitTestContext
{
    private static readonly Dictionary<BitTextVariant, string> VariantMapping = new()
    {
        { BitTextVariant.Body1, "p" },
        { BitTextVariant.Body2, "p" },
        { BitTextVariant.Button, "span" },
        { BitTextVariant.Caption1, "span" },
        { BitTextVariant.Caption2, "span" },
        { BitTextVariant.H1, "h1" },
        { BitTextVariant.H2, "h2" },
        { BitTextVariant.H3, "h3" },
        { BitTextVariant.H4, "h4" },
        { BitTextVariant.H5, "h5" },
        { BitTextVariant.H6, "h6" },
        { BitTextVariant.Inherit, "p" },
        { BitTextVariant.Overline, "span" },
        { BitTextVariant.Subtitle1, "h6" },
        { BitTextVariant.Subtitle2, "h6" }
    };

    [DataTestMethod]
    public void BitTextShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore></h6>");
    }

    [DataTestMethod,
         DataRow(BitTextVariant.H1),
         DataRow(BitTextVariant.H2),
         DataRow(BitTextVariant.H3),
         DataRow(BitTextVariant.H4),
         DataRow(BitTextVariant.H5),
         DataRow(BitTextVariant.H6),
         DataRow(BitTextVariant.Subtitle1),
         DataRow(BitTextVariant.Subtitle2),
         DataRow(BitTextVariant.Body1),
         DataRow(BitTextVariant.Body2),
         DataRow(BitTextVariant.Button),
         DataRow(BitTextVariant.Caption1),
         DataRow(BitTextVariant.Caption2),
         DataRow(BitTextVariant.Overline),
         DataRow(BitTextVariant.Inherit),
    ]
    public void BitTextShouldRespectVariant(BitTextVariant variant)
    {
        var component = RenderComponent<BitText>(parameters =>
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
    public void BitTextShouldRespectElement(string element)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var defaultVariant = BitTextVariant.Subtitle1;
        var el = element is null ? VariantMapping[defaultVariant] : element;

        component.MarkupMatches(@$"<{el} class=""bit-tpg bit-tpg-subtitle1"" id:ignore></{el}>");

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

        var cssClass = noWrap ? " bit-tpg-nowrap" : null;

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectNoWrapChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

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
    public void BitTextShouldRespectGutter(bool gutter)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Gutter, gutter);
        });

        var cssClass = gutter ? " bit-tpg-gutter" : null;

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectGutterChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

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
    public void BitTextShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

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
    public void BitTextShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitText>(parameters =>
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
    public void BitTextShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

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
    public void BitTextShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

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
    public void BitTextShouldRespectId(string id)
    {
        var component = RenderComponent<BitText>(parameters =>
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
    public void BitTextShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitText>(parameters =>
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
    public void BitTextShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

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
    public void BitTextShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitText>(parameters =>
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
    public void BitTextShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

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
    public void BitTextShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitText>(parameters =>
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
    public void BitTextShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches(@$"<h6 class=""bit-tpg bit-tpg-subtitle1"" id:ignore>{childContent}</h6>");
    }

    [DataTestMethod]
    public void BitTextShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitTextHtmlAttributesTest>();

        component.MarkupMatches(@"<h6 data-val-test=""bit"" class=""bit-tpg bit-tpg-subtitle1"" id:ignore>I'm a text</h6>");
    }
}
