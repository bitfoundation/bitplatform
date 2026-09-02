using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Sticky;

[TestClass]
public class BitStickyTests : BunitTestContext
{
    [TestMethod]
    public void BitStickyShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top bit-dis"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var style = "padding: 1rem;";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var cssClass = "test-class";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top {cssClass}"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div dir=""ltr"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<div style=""display: none;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod,
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

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top"" id:ignore>{childContent}</div>");
    }

    [TestMethod]
    public void BitStickyShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitStickyHtmlAttributesTest>();

        component.MarkupMatches(@"<div data-val-test=""bit"" class=""bit-stk bit-stk-top"" id:ignore>I'm a sticky</div>");
    }

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectTopChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var top = "20px";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Top, top);
        });

        component.MarkupMatches(@$"<div style=""top: {top};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectBottomChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var bottom = "20px";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Bottom, "20px");
        });

        component.MarkupMatches(@$"<div style=""bottom: {bottom};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectLeftChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var left = "20px";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Left, left);
        });

        component.MarkupMatches(@$"<div style=""left: {left};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectRightChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var right = "20px";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Right, right);
        });

        component.MarkupMatches(@$"<div style=""right: {right};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div style=""top: {top};bottom: {bottom};left: {left};right: {right};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
       DataRow("20", "20px"),
       DataRow("1.5", "1.5px"),
       DataRow("0", "0px")
    ]
    public void BitStickyShouldReadBareNumberOffsetsAsPixels(string offset, string expected)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Top, offset);
        });

        component.MarkupMatches(@$"<div style=""top: {expected};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
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

    [TestMethod]
    public void BitStickyShouldRespectPositionChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Position, BitStickyPosition.Start);
        });

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-srt"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStickyShouldRespectPositionAlongsideOffsets()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Position, BitStickyPosition.Top);
            parameters.Add(p => p.Top, "10px");
        });

        component.MarkupMatches(@"<div style=""top: 10px;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
       DataRow(3),
       DataRow(0),
       DataRow(-1),
       DataRow(null)
    ]
    public void BitStickyShouldRespectZIndex(int? zIndex)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.ZIndex, zIndex);
        });

        if (zIndex.HasValue)
        {
            component.MarkupMatches(@$"<div style=""z-index: {zIndex};"" class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitStickyShouldRespectZIndexChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.ZIndex, 5);
        });

        component.MarkupMatches(@"<div style=""z-index: 5;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStickyShouldNotApplyStuckClassAndStyleWhileNotStuck()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
            parameters.Add(p => p.StuckStyle, "color: red");
        });

        Assert.IsFalse(component.Instance.IsStuck);

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod]
    public async Task BitStickyShouldApplyStuckClassAndStyleWhileStuck()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
            parameters.Add(p => p.StuckStyle, "color: red");
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange(true));

        Assert.IsTrue(component.Instance.IsStuck);

        component.MarkupMatches(@"<div style=""color: red;"" class=""bit-stk bit-stk-top bit-stk-stc my-stuck"" id:ignore></div>");

        await component.InvokeAsync(() => component.Instance._OnStuckChange(false));

        Assert.IsFalse(component.Instance.IsStuck);

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod]
    public async Task BitStickyShouldAppendStuckStyleAfterStyle()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Style, "color: blue");
            parameters.Add(p => p.StuckStyle, "color: red");
        });

        Assert.AreEqual("color: blue", component.Find("div").GetAttribute("style"));

        await component.InvokeAsync(() => component.Instance._OnStuckChange(true));

        // The stuck style has to land after the resting one, since the later declaration of the same
        // property is the one an inline style resolves to.
        Assert.AreEqual("color: blue;color: red", component.Find("div").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitStickyShouldRespectOnStuckChanged()
    {
        var stuckStates = new List<bool>();

        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.OnStuckChanged, (bool stuck) => stuckStates.Add(stuck));
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange(true));

        CollectionAssert.AreEqual(new List<bool> { true }, stuckStates);
        Assert.IsTrue(component.Instance.IsStuck);

        // A repeated report of the same state must not raise the callback again.
        await component.InvokeAsync(() => component.Instance._OnStuckChange(true));

        CollectionAssert.AreEqual(new List<bool> { true }, stuckStates);

        await component.InvokeAsync(() => component.Instance._OnStuckChange(false));

        CollectionAssert.AreEqual(new List<bool> { true, false }, stuckStates);
        Assert.IsFalse(component.Instance.IsStuck);
    }

    [TestMethod]
    public async Task BitStickyShouldIgnoreStuckReportsWhileDisabled()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.StuckClass, "my-stuck");
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange(true));

        Assert.IsFalse(component.Instance.IsStuck);

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top bit-dis"" id:ignore></div>");
    }

    [TestMethod]
    public async Task BitStickyShouldResetStuckStateWhenDisabled()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange(true));

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top bit-stk-stc my-stuck"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsFalse(component.Instance.IsStuck);

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top bit-dis"" id:ignore></div>");
    }

    [TestMethod]
    public async Task BitStickyShouldReportUnstuckWhenDetectionIsDetached()
    {
        var stuckStates = new List<bool>();

        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.OnStuckChanged, (bool stuck) => stuckStates.Add(stuck));
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange(true));

        CollectionAssert.AreEqual(new[] { true }, stuckStates);

        component.Render(parameters =>
        {
            parameters.Add(p => p.OnStuckChanged, (bool stuck) => stuckStates.Add(stuck));
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsFalse(component.Instance.IsStuck);

        CollectionAssert.AreEqual(new[] { true, false }, stuckStates);
    }
}
