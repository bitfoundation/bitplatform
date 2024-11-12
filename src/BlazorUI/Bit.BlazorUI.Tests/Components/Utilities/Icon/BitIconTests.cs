using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Icon;

[TestClass]
public class BitIconTests : BunitTestContext
{
    private const string CLASS = "bit-ico bit-ico-pri bit-ico-md bit-ico-txt";

    [DataTestMethod]
    public void BitIconShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i role=""img"" class=""{CLASS}"" id:ignore />");
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

        component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" role=""img"" id:ignore />");
    }

    [DataTestMethod]
    public void BitIconShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-dis"" role=""img"" id:ignore />");
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

        component.MarkupMatches(@$"<i class=""{CLASS}{icoClass}"" role=""img"" id:ignore />");
    }

    [DataTestMethod]
    public void BitIconShouldRespectIconNameChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IconName, "AzureIcon");
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-icon bit-icon--AzureIcon"" role=""img""  id:ignore />");
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
            component.MarkupMatches(@$"<i style=""{style}"" class=""{CLASS}"" role=""img"" id:ignore />");
        }
        else
        {
            component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");
        }
    }

    [DataTestMethod]
    public void BitIconShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        component.MarkupMatches(@$"<i style=""padding: 1rem;"" class=""{CLASS}"" role=""img"" id:ignore />");
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

        component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" role=""img"" id:ignore />");
    }

    [DataTestMethod]
    public void BitIconShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        component.MarkupMatches(@$"<i class=""{CLASS} test-class"" role=""img""  id:ignore />");
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

        component.MarkupMatches(@$"<i id=""{expectedId}"" class=""{CLASS}"" role=""img"" />");
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
            component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" dir=""{dir.Value.ToString().ToLower()}"" role=""img"" id:ignore />");
        }
        else
        {
            component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");
        }
    }

    [DataTestMethod]
    public void BitIconShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@$"<i dir=""ltr"" class=""{CLASS}"" role=""img"" id:ignore />");
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
                component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@$"<i style=""visibility: hidden;"" class=""{CLASS}"" role=""img"" id:ignore />");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@$"<i style=""display: none;"" class=""{CLASS}"" role=""img"" id:ignore />");
                break;
        }
    }

    [DataTestMethod]
    public void BitIconShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@$"<i style=""display: none;"" class=""{CLASS}"" role=""img"" id:ignore />");
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
            component.MarkupMatches(@$"<i aria-label=""{ariaLabel}"" class=""{CLASS}"" role=""img"" id:ignore />");
        }
        else
        {
            component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");
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
            BitSize.Small => "bit-ico-sm",
            BitSize.Medium => "bit-ico-md",
            BitSize.Large => "bit-ico-lg",
            _ => "bit-ico-md"
        };

        component.MarkupMatches(@$"<i class=""bit-ico bit-ico-pri bit-ico-txt {sizeClass}"" role=""img"" id:ignore />");
    }

    [DataTestMethod]
    public void BitIconShouldRespectSizeChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
        });

        component.MarkupMatches(@"<i class=""bit-ico bit-ico-pri bit-ico-txt bit-ico-lg"" role=""img"" id:ignore />");
    }

    [DataTestMethod,
        DataRow(BitColor.Primary),
        DataRow(BitColor.Secondary),
        DataRow(BitColor.Tertiary),
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

        var colorClass = color switch
        {
            BitColor.Primary => " bit-ico-pri",
            BitColor.Secondary => " bit-ico-sec",
            BitColor.Tertiary => " bit-ico-ter",
            BitColor.Info => " bit-ico-inf",
            BitColor.Success => " bit-ico-suc",
            BitColor.Warning => " bit-ico-wrn",
            BitColor.SevereWarning => " bit-ico-swr",
            BitColor.Error => " bit-ico-err",
            _ => " bit-ico-pri"
        };

        component.MarkupMatches(@$"<i class=""bit-ico bit-ico-md bit-ico-txt {colorClass}"" role=""img"" id:ignore />");
    }

    [DataTestMethod]
    public void BitIconShouldRespectColorChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" role=""img"" id:ignore />");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
        });

        component.MarkupMatches(@"<i class=""bit-ico bit-ico-md bit-ico-txt bit-ico-err"" role=""img"" id:ignore />");
    }

    [DataTestMethod]
    public void BitIconShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitIconHtmlAttributesTest>();

        component.MarkupMatches(@$"<i data-val-test=""bit"" role=""img"" class=""{CLASS}"" id:ignore />");
    }
}
