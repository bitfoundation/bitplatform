using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Icon;

[TestClass]
public class BitIconTests : BunitTestContext
{
    [DataTestMethod]
    public void BitIconShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@"<i id:regex="".+"" role=""img"" class=""bit-ico"" />");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitIconShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<i id:regex="".+"" role=""img"" class=""bit-ico{cssClass}"" />");
    }

    [DataTestMethod,
        DataRow("VisualStudioForWindows"),
        DataRow("AzureIcon"),
        DataRow(""),
        DataRow(null)
    ]
    public void BitIconShouldRespectIconName(string iconName)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var icoClass = iconName.HasValue() ? $" bit-icon bit-icon--{iconName}" : null;

        component.MarkupMatches(@$"<i id:regex="".+"" role=""img"" class=""bit-ico{icoClass}"" />");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitIconShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<i id:regex="".+"" role=""img"" style=""{style}"" class=""bit-ico"" />");
        }
        else
        {
            component.MarkupMatches(@"<i id:regex="".+"" role=""img"" class=""bit-ico"" />");
        }
    }

    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitIconShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<i id:regex="".+"" role=""img"" class=""bit-ico{cssClass}"" />");
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitIconShouldRespectId(string id)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<i id=""{expectedId}"" role=""img"" class=""bit-ico"" />");
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitIconShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<i id:regex="".+"" role=""img"" class=""bit-ico{cssClass}"" dir=""{dir.Value.ToString().ToLower()}"" />");
        }
        else
        {
            component.MarkupMatches(@"<i id:regex="".+"" role=""img"" class=""bit-ico"" />");
        }
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitIconShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<i id:regex="".+"" role=""img"" class=""bit-ico"" />");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<i id:regex="".+"" role=""img"" style=""visibility: hidden;"" class=""bit-ico"" />");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<i id:regex="".+"" role=""img"" style=""display: none;"" class=""bit-ico"" />");
                break;
        }
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitIconShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<i id:regex="".+"" role=""img"" aria-label=""{ariaLabel}"" class=""bit-ico"" />");
        }
        else
        {
            component.MarkupMatches(@"<i id:regex="".+"" role=""img"" class=""bit-ico"" />");
        }
    }

    [DataTestMethod,
        DataRow(BitSize.Small),
        DataRow(BitSize.Medium),
        DataRow(BitSize.Large),
        DataRow(null)
    ]
    public void BitIconShouldRespectSize(BitSize? size)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var sizeClass = size switch
        {
            BitSize.Small => " bit-ico-sm",
            BitSize.Medium => " bit-ico-md",
            BitSize.Large => " bit-ico-lg",
            _ => null
        };

        component.MarkupMatches(@$"<i id:regex="".+"" role=""img"" class=""bit-ico{sizeClass}"" />");
    }

    [DataTestMethod,
        DataRow(BitColor.Info),
        DataRow(BitColor.Success),
        DataRow(BitColor.Warning),
        DataRow(BitColor.SevereWarning),
        DataRow(BitColor.Error),
        DataRow(null)
    ]
    public void BitIconShouldRespectColor(BitColor? color)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var sizeClass = color switch
        {
            BitColor.Info => " bit-ico-inf",
            BitColor.Success => " bit-ico-suc",
            BitColor.Warning => " bit-ico-wrn",
            BitColor.SevereWarning => " bit-ico-swr",
            BitColor.Error => " bit-ico-err",
            _ => null
        };

        component.MarkupMatches(@$"<i id:regex="".+"" role=""img"" class=""bit-ico{sizeClass}"" />");
    }

    [DataTestMethod]
    public void BitIconShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitIconHtmlAttributesTest>();

        component.MarkupMatches(@"<i data-val-test=""bit"" id:regex="".+"" role=""img"" class=""bit-ico"" />");
    }
}
