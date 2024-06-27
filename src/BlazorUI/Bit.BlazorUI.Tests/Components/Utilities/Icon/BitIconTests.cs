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

        component.MarkupMatches(@"<i id:regex="".+"" role=""img"" class=""bit-icon bit-icon-- bit-ico"" />");
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

        var cssClass = isEnabled ? "bit-icon bit-icon-- bit-ico" : "bit-icon bit-icon-- bit-ico bit-dis";

        component.MarkupMatches(@$"<i class=""{cssClass}"" id:ignore role:ignore />");
    }

    [DataTestMethod,
        DataRow("VisualStudioForWindows"),
        DataRow("AzureIcon"),
        DataRow(""),
        DataRow(null)
    ]
    public void BitIconShouldRespectRequired(string iconName)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        component.MarkupMatches(@$"<i class=""bit-icon bit-icon--{iconName} bit-ico"" id:ignore role:ignore />");
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

        if (style.HasNoValue())
        {
            component.MarkupMatches(@"<i id:ignore class:ignore role:ignore />");
        }
        else
        {
            component.MarkupMatches(@$"<i style=""{style}"" id:ignore class:ignore role:ignore />");
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

        var cssClass = @class.HasValue() ? $"bit-icon bit-icon-- bit-ico {@class}" : "bit-icon bit-icon-- bit-ico";

        component.MarkupMatches(@$"<i class=""{cssClass}"" id:ignore role:ignore />");
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

        component.MarkupMatches(@$"<i id=""{expectedId}"" class:ignore role:ignore />");
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
            var cssClass = dir is BitDir.Rtl ? "bit-icon bit-icon-- bit-ico bit-rtl" : "bit-icon bit-icon-- bit-ico";
            component.MarkupMatches(@$"<i class=""{cssClass}"" dir=""{dir.Value.ToString().ToLower()}"" id:ignore role:ignore />");
        }
        else
        {
            component.MarkupMatches(@"<i id:ignore class:ignore role:ignore />");
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
                component.MarkupMatches(@"<i id:ignore class:ignore role:ignore />");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<i style=""visibility: hidden;"" id:ignore class:ignore role:ignore />");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<i style=""display: none;"" id:ignore class:ignore role:ignore />");
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
        }); ;

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<i aria-label=""{ariaLabel}"" id:ignore class:ignore role:ignore />");
        }
        else
        {
            component.MarkupMatches(@"<i id:ignore class:ignore role:ignore />");
        }
    }

    [DataTestMethod]
    public void BitIconShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitIconHtmlAttributesTest>();

        component.MarkupMatches(@"<i data-val-test=""bit"" id:ignore class:ignore role:ignore />");
    }
}
