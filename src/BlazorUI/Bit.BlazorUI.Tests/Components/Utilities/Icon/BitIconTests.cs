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

        component.MarkupMatches(@"<i role=""img"" class=""bit-ico"" id:ignore />");
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

        component.MarkupMatches(@$"<i class=""bit-ico{cssClass}"" role=""img"" id:ignore />");
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

        component.MarkupMatches(@$"<i class=""bit-ico{icoClass}"" role=""img"" id:ignore />");
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
            component.MarkupMatches(@$"<i style=""{style}"" role=""img"" class=""bit-ico"" id:ignore />");
        }
        else
        {
            component.MarkupMatches(@"<i role=""img"" class=""bit-ico"" id:ignore />");
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

        component.MarkupMatches(@$"<i class=""bit-ico{cssClass}"" role=""img"" id:ignore />");
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
            component.MarkupMatches(@$"<i class=""bit-ico{cssClass}"" dir=""{dir.Value.ToString().ToLower()}"" role=""img"" id:ignore />");
        }
        else
        {
            component.MarkupMatches(@"<i role=""img"" class=""bit-ico"" id:ignore />");
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
                component.MarkupMatches(@"<i role=""img"" class=""bit-ico"" id:ignore />");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<i style=""visibility: hidden;"" role=""img"" class=""bit-ico"" id:ignore />");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<i style=""display: none;"" role=""img"" class=""bit-ico"" id:ignore />");
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
            component.MarkupMatches(@$"<i aria-label=""{ariaLabel}"" role=""img"" class=""bit-ico"" id:ignore />");
        }
        else
        {
            component.MarkupMatches(@"<i role=""img"" class=""bit-ico"" id:ignore />");
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

        component.MarkupMatches(@$"<i class=""bit-ico{sizeClass}"" role=""img"" id:ignore />");
    }

    [DataTestMethod,
        DataRow(BitSeverity.Info),
        DataRow(BitSeverity.Success),
        DataRow(BitSeverity.Warning),
        DataRow(BitSeverity.SevereWarning),
        DataRow(BitSeverity.Error),
        DataRow(null)
    ]
    public void BitIconShouldRespectColor(BitSeverity? severity)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Severity, severity);
        });

        var sizeClass = severity switch
        {
            BitSeverity.Info => " bit-ico-inf",
            BitSeverity.Success => " bit-ico-suc",
            BitSeverity.Warning => " bit-ico-wrn",
            BitSeverity.SevereWarning => " bit-ico-swr",
            BitSeverity.Error => " bit-ico-err",
            _ => null
        };

        component.MarkupMatches(@$"<i class=""bit-ico{sizeClass}"" role=""img"" id:ignore />");
    }

    [DataTestMethod]
    public void BitIconShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitIconHtmlAttributesTest>();

        component.MarkupMatches(@"<i data-val-test=""bit"" role=""img"" class=""bit-ico"" id:ignore />");
    }
}
