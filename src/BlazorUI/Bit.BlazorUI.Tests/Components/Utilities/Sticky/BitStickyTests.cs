using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Sticky;

[TestClass]
public class BitStickyTests : BunitTestContext
{
    [DataTestMethod]
    public void BitStickyShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStickyShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStickyShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top bit-dis"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitStickyShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var style = "padding: 1rem;";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStickyShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var cssClass = "test-class";

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top {cssClass}"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectId(string id)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-stk bit-stk-top""></div>");
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitStickyShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-stk bit-stk-top{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitStickyShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div dir=""ltr"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitStickyShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<div style=""visibility: hidden;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<div style=""display: none;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
                break;
        }
    }

    [DataTestMethod]
    public void BitStickyShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<div style=""display: none;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<div aria-label=""{ariaLabel}"" class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top"" id:ignore>{childContent}</label>");
    }

    [DataTestMethod]
    public void BitStickyShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitStickyHtmlAttributesTest>();

        component.MarkupMatches(@"<div data-val-test=""bit"" class=""bit-stk bit-stk-top"" id:ignore>I'm a sticky</div>");
    }

    [DataTestMethod,
       DataRow(null),
       DataRow(""),
       DataRow("14px"),
       DataRow("1.5rem")
    ]
    public void BitStickyShouldRespectTop(string top)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Top, top);
        });

        if (top.HasValue())
        {
            component.MarkupMatches(@$"<div style=""top: {top};"" class=""bit-stk"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitStickyShouldRespectTopChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var top = "20px";

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Top, top);
        });

        component.MarkupMatches(@$"<div style=""top: {top};"" class=""bit-stk"" id:ignore></div>");
    }

    [DataTestMethod,
       DataRow(null),
       DataRow(""),
       DataRow("14px"),
       DataRow("1.5rem")
    ]
    public void BitStickyShouldRespectBottom(string bottom)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Bottom, bottom);
        });

        if (bottom.HasValue())
        {
            component.MarkupMatches(@$"<div style=""bottom: {bottom};"" class=""bit-stk"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitStickyShouldRespectBottomChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var bottom = "20px";

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Bottom, "20px");
        });

        component.MarkupMatches(@$"<div style=""bottom: {bottom};"" class=""bit-stk"" id:ignore></div>");
    }

    [DataTestMethod,
       DataRow(null),
       DataRow(""),
       DataRow("14px"),
       DataRow("1.5rem")
    ]
    public void BitStickyShouldRespectLeft(string left)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Left, left);
        });

        if (left.HasValue())
        {
            component.MarkupMatches(@$"<div style=""left: {left};"" class=""bit-stk"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitStickyShouldRespectLeftChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var left = "20px";

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Left, left);
        });

        component.MarkupMatches(@$"<div style=""left: {left};"" class=""bit-stk"" id:ignore></div>");
    }

    [DataTestMethod,
       DataRow(null),
       DataRow(""),
       DataRow("14px"),
       DataRow("1.5rem")
    ]
    public void BitStickyShouldRespectRight(string right)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Right, right);
        });

        if (right.HasValue())
        {
            component.MarkupMatches(@$"<div style=""right: {right};"" class=""bit-stk"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitStickyShouldRespectRightChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var right = "20px";

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Right, right);
        });

        component.MarkupMatches(@$"<div style=""right: {right};"" class=""bit-stk"" id:ignore></div>");
    }

    [DataTestMethod,
       DataRow("14px", "15px", "16px", "17px"),
       DataRow("1.5rem", "2.5rem", "3.5rem", "4.5rem")
    ]
    public void BitStickyShouldRespectTopBottomLeftRight(string top, string bottom, string left, string right)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Top, top);
            parameters.Add(p => p.Bottom, bottom);
            parameters.Add(p => p.Left, left);
            parameters.Add(p => p.Right, right);
        });

        if (right.HasValue())
        {
            component.MarkupMatches(@$"<div style=""top: {top};bottom: {bottom};left: {left};right: {right};"" class=""bit-stk"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [DataTestMethod,
       DataRow(null),
       DataRow(BitStickyPosition.Top),
       DataRow(BitStickyPosition.Bottom),
       DataRow(BitStickyPosition.TopAndBottom),
       DataRow(BitStickyPosition.Start),
       DataRow(BitStickyPosition.End),
       DataRow(BitStickyPosition.StartAndEnd)
    ]
    public void BitStickyShouldRespectPosition(BitStickyPosition? position)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Position, position);
        });

        var cssClass = position switch
        {
            BitStickyPosition.Top => " bit-stk-top",
            BitStickyPosition.Bottom => " bit-stk-btm",
            BitStickyPosition.TopAndBottom => " bit-stk-tab",
            BitStickyPosition.Start => " bit-stk-srt",
            BitStickyPosition.End => " bit-stk-end",
            BitStickyPosition.StartAndEnd => " bit-stk-sae",
            _ => " bit-stk-top"
        };

        component.MarkupMatches(@$"<div class=""bit-stk{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStickyShouldRespectPositionChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Position, BitStickyPosition.Start);
        });

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-srt"" id:ignore></div>");
    }
}
