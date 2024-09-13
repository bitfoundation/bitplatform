﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text;
using System;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Stack;

[TestClass]
public class BitStackTests : BunitTestContext
{
    private const string STYLE = "flex-direction:column;align-items:flex-start;justify-content:flex-start;";

    private static readonly Dictionary<BitAlignment, string> _AlignmentMap = new()
    {
        { BitAlignment.Start, "flex-start" },
        { BitAlignment.End, "flex-end" },
        { BitAlignment.Center, "center" },
        { BitAlignment.SpaceBetween, "space-between" },
        { BitAlignment.SpaceAround, "space-around" },
        { BitAlignment.SpaceEvenly, "space-evenly" },
        { BitAlignment.Baseline, "baseline" },
        { BitAlignment.Stretch, "stretch" },
    };

    [DataTestMethod]
    public void BitStackShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div class=""bit-stc{cssClass}"" style=""{STYLE}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div class=""bit-stc"" style=""{STYLE}"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@$"<div class=""bit-stc bit-dis"" style=""{STYLE}"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitStackShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        var style = "padding: 1rem;";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitStackShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div class=""bit-stc{cssClass}"" style=""{STYLE}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div class=""bit-stc"" style=""{STYLE}"" id:ignore></div>");

        var cssClass = "test-class";

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div class=""bit-stc {cssClass}"" style=""{STYLE}"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitStackShouldRespectId(string id)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-stc"" style=""{STYLE}""></div>");
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitStackShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-stc{cssClass}"" style=""{STYLE}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-stc"" style=""{STYLE}"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitStackShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@$"<div dir=""ltr"" class=""bit-stc"" style=""{STYLE}"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitStackShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = visibility switch
        {
            BitVisibility.Hidden => "visibility: hidden;",
            BitVisibility.Collapsed => "display: none;",
            _ => null
        };

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}display: none;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitStackShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            if (childContent is not null)
            {
                parameters.AddChildContent(childContent);
            }
        });

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore>{childContent}</div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitStackHtmlAttributesTest>();

        component.MarkupMatches(@$"<div data-val-test=""bit"" style=""{STYLE}"" class=""bit-stc"" id:ignore>I'm a stack</div>");
    }

    [DataTestMethod,
        DataRow("p"),
        DataRow("span"),
        DataRow(null)
    ]
    public void BitStackShouldRespectElement(string element)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var el = element.HasValue() ? element : "div";
        component.MarkupMatches(@$"<{el} class=""bit-stc"" style=""{STYLE}"" id:ignore></{el}>");
    }

    [DataTestMethod,
        DataRow("10px"),
        DataRow("1rem"),
        DataRow(null)
    ]
    public void BitStackShouldRespectGap(string gap)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Gap, gap);
        });

        var style = gap.HasValue() ? $"gap:{gap}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectGapChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        var gap = "1rem";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Gap, gap);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}gap:{gap};"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("3"),
        DataRow("initial"),
        DataRow("inherit"),
        DataRow(null)
    ]
    public void BitStackShouldRespectGrow(string grow)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Grow, grow);
        });

        var style = grow.HasValue() ? $"flex-grow:{grow}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectGrowChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        var grow = "2";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Grow, grow);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-grow:{grow};"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectGrows(bool grows)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Grows, grows);
        });

        var style = grows ? "flex-grow:1" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectGrowsChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Grows, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-grow:1;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectHorizontal(bool horizontal)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Horizontal, horizontal);
        });

        var fd = horizontal ? "row" : "column";

        component.MarkupMatches(@$"<div style=""flex-direction:{fd};align-items:flex-start;justify-content:flex-start;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectHorizontalChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Horizontal, true);
        });

        component.MarkupMatches(@$"<div style=""flex-direction:row;align-items:flex-start;justify-content:flex-start;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(BitAlignment.Start),
        DataRow(BitAlignment.End),
        DataRow(BitAlignment.Center),
        DataRow(BitAlignment.SpaceBetween),
        DataRow(BitAlignment.SpaceAround),
        DataRow(BitAlignment.SpaceEvenly),
        DataRow(BitAlignment.Baseline),
        DataRow(BitAlignment.Stretch)
    ]
    public void BitStackShouldRespectHorizontalAlign(BitAlignment horizontalAlign)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalAlign, horizontalAlign);
        });

        var ai = _AlignmentMap[horizontalAlign];

        component.MarkupMatches(@$"<div style=""flex-direction:column;align-items:{ai};justify-content:flex-start;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectHorizontalAlignChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.HorizontalAlign, BitAlignment.SpaceBetween);
        });

        component.MarkupMatches(@$"<div style=""flex-direction:column;align-items:space-between;justify-content:flex-start;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectReversed(bool reversed)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        var fd = reversed ? "column-reverse" : "column";

        component.MarkupMatches(@$"<div style=""flex-direction:{fd};align-items:flex-start;justify-content:flex-start;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectReversedChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Reversed, true);
        });

        component.MarkupMatches(@$"<div style=""flex-direction:column-reverse;align-items:flex-start;justify-content:flex-start;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(BitAlignment.Start),
        DataRow(BitAlignment.End),
        DataRow(BitAlignment.Center),
        DataRow(BitAlignment.SpaceBetween),
        DataRow(BitAlignment.SpaceAround),
        DataRow(BitAlignment.SpaceEvenly),
        DataRow(BitAlignment.Baseline),
        DataRow(BitAlignment.Stretch)
    ]
    public void BitStackShouldRespectBitAlignment(BitAlignment verticalAlign)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.VerticalAlign, verticalAlign);
        });

        var jc = _AlignmentMap[verticalAlign];

        component.MarkupMatches(@$"<div style=""flex-direction:column;align-items:flex-start;justify-content:{jc};"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectVerticalAlignChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.VerticalAlign, BitAlignment.SpaceBetween);
        });

        component.MarkupMatches(@$"<div style=""flex-direction:column;align-items:flex-start;justify-content:space-between;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectWrap(bool wrap)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Wrap, wrap);
        });

        var style = wrap ? "flex-wrap:wrap" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectWrapChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Wrap, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-wrap:wrap"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true, null),
        DataRow(true, "2"),
        DataRow(false, null),
        DataRow(false, "2")
    ]
    public void BitStackShouldRespectGrowsAndGrow(bool grows, string grow)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Grow, grow);
            parameters.Add(p => p.Grows, grows);
        });

        var style = (grow.HasValue() || grows) ? $"flex-grow:{(grow.HasValue() ? grow : "1")}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(false, false)
    ]
    public void BitStackShouldRespectHorizontalAndReversed(bool horizontal, bool reversed)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Horizontal, horizontal);
            parameters.Add(p => p.Reversed, reversed);
        });

        var fd = $"{(horizontal ? "row" : "column")}{(reversed ? "-reverse" : null)}";

        component.MarkupMatches(@$"<div style=""flex-direction:{fd};align-items:flex-start;justify-content:flex-start;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectHorizontalAndReversedAndHorizontalAlignAndVerticalAlign(bool horizontal)
    {
        var aligns = Enum.GetValues(typeof(BitAlignment));

        foreach (BitAlignment horizontalAlign in aligns)
        {
            foreach (BitAlignment verticalAlign in aligns)
            {
                var component = RenderComponent<BitStack>(parameters =>
                {
                    parameters.Add(p => p.Horizontal, horizontal);
                    parameters.Add(p => p.VerticalAlign, verticalAlign);
                    parameters.Add(p => p.HorizontalAlign, horizontalAlign);
                });

                var fd = horizontal ? "row" : "column";
                var ai = _AlignmentMap[horizontal ? verticalAlign : horizontalAlign];
                var jc = _AlignmentMap[horizontal ? horizontalAlign : verticalAlign];

                component.MarkupMatches(@$"<div style=""flex-direction:{fd};align-items:{ai};justify-content:{jc};"" class=""bit-stc"" id:ignore></div>");
            }
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectFull(bool full)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Full, full);
        });

        var style = full ? "width:100%;height:100%;" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectFullChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Full, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}width:100%;height:100%;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectFullWidth(bool fullWidth)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        var style = fullWidth ? "width:100%;" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectFullWidthChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.FullWidth, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}width:100%;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectFullHeight(bool fullHeight)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.FullHeight, fullHeight);
        });

        var style = fullHeight ? "height:100%;" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitStackShouldRespectFullHeightChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.FullHeight, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}height:100%;"" class=""bit-stc"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true, true, true),
        DataRow(true, true, false),
        DataRow(true, false, true),
        DataRow(true, false, false),
        DataRow(false, true, true),
        DataRow(false, true, false),
        DataRow(false, false, true),
        DataRow(false, false, false)
    ]
    public void BitStackShouldRespectFullAndFullWidthAndFullHeight(bool full, bool fullWidth, bool fullHeight)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Full, full);
            parameters.Add(p => p.FullWidth, fullWidth);
            parameters.Add(p => p.FullHeight, fullHeight);
        });

        StringBuilder style = new();

        if (full || fullWidth)
        {
            style.Append("width:100%;");
        }

        if (full || fullHeight)
        {
            style.Append("height:100%;");
        }

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }
}
