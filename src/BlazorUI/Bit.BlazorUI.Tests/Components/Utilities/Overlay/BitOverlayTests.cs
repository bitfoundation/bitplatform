using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Overlay;

[TestClass]
public class BitOverlayTests : BunitTestContext
{
    [DataTestMethod]
    public void BitOverlayShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div class=""bit-ovl{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitOverlayShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div class=""bit-ovl bit-dis"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-ovl"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitOverlayShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");

        var style = "padding: 1rem;";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-ovl"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div class=""bit-ovl{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitOverlayShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");

        var cssClass = "test-class";

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div class=""bit-ovl {cssClass}"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectId(string id)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-ovl""></div>");
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-ovl{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitOverlayShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div dir=""ltr"" class=""bit-ovl"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitOverlayShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var styleAttribute = visibility switch
        {
            BitVisibility.Hidden => @"style=""visibility: hidden;""",
            BitVisibility.Collapsed => @"style=""display: none;""",
            _ => null
        };

        component.MarkupMatches(@$"<div {styleAttribute} class=""bit-ovl"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitOverlayShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@$"<div style=""display: none;"" class=""bit-ovl"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            if (childContent is not null)
            {
                parameters.AddChildContent(childContent);
            }
        });

        component.MarkupMatches(@$"<div class=""bit-ovl"" id:ignore>{childContent}</div>");
    }

    [DataTestMethod]
    public void BitOverlayShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitOverlayHtmlAttributesTest>();

        component.MarkupMatches(@$"<div data-val-test=""bit"" class=""bit-ovl"" id:ignore>I'm an overlay</div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectNoAutoClose(bool noAutoClose)
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, currentValue => isOpen = currentValue);
            parameters.Add(p => p.NoAutoClose, noAutoClose);
        });

        component.MarkupMatches(@"<div class=""bit-ovl bit-ovl-opn"" id:ignore></div>");

        Assert.IsTrue(isOpen);

        var element = component.Find(".bit-ovl");
        element.Click();

        var cssClass = noAutoClose ? " bit-ovl-opn" : null;

        component.MarkupMatches(@$"<div class=""bit-ovl{cssClass}"" id:ignore></div>");

        if (noAutoClose)
        {
            Assert.IsTrue(isOpen);
        }
        else
        {
            Assert.IsFalse(isOpen);
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectAutoToggleScroll(bool autoToggleScroll)
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.AutoToggleScroll, autoToggleScroll);
        });

        var element = component.Find(".bit-ovl");
        element.Click();

        if (autoToggleScroll)
        {
            //AutoToggleScroll is false by default so it should invoke "BitBlazorUI.Overlay.toggleScroll" once and then once again on closing component
            Context.JSInterop.VerifyInvoke("BitBlazorUI.Overlay.toggleScroll", 2);
        }
        else
        {
            Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Overlay.toggleScroll");
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectAbsolutePosition(bool absolutePosition)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, absolutePosition);
        });

        var cssClass = absolutePosition ? " bit-ovl-abs" : null;

        component.MarkupMatches(@$"<div class=""bit-ovl{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitOverlayShouldRespectAbsolutePositionChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, true);
        });

        component.MarkupMatches(@$"<div class=""bit-ovl bit-ovl-abs"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectIsOpen(bool IsOpen)
    {
        var isOpenBind = IsOpen;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpenBind, value => isOpenBind = value);
        });

        var cssClass = IsOpen ? " bit-ovl-opn" : null;

        component.MarkupMatches(@$"<div class=""bit-ovl{cssClass}"" id:ignore></div>");

        Assert.AreEqual(IsOpen, isOpenBind);

        var element = component.Find(".bit-ovl");
        element.Click();

        component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");

        Assert.IsFalse(isOpenBind);
    }

    [DataTestMethod]
    public void BitOverlayShouldRespectIsOpenChangingAfterRender()
    {
        var isOpen = false;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        component.MarkupMatches(@"<div class=""bit-ovl"" id:ignore></div>");

        isOpen = true;
        component.SetParametersAndRender(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        component.MarkupMatches(@$"<div class=""bit-ovl bit-ovl-opn"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectOnClick(bool isEnabled)
    {
        int clickedValue = 0;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var rootDiv = component.Find(".bit-ovl");
        rootDiv.Click();

        var expected = isEnabled ? 1 : 0;
        Assert.AreEqual(expected, clickedValue);
    }
}
