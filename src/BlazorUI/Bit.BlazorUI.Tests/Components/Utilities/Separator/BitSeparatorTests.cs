using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Separator;

[TestClass]
public class BitSeparatorTests : BunitTestContext
{
    [DataTestMethod]
    public void BitSeparatorShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSeparatorShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div class=""bit-spr bit-spr-hrz bit-spr-ctr{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr bit-dis"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitSeparatorShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        var style = "padding: 1rem;";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitSeparatorShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div class=""bit-spr bit-spr-hrz bit-spr-ctr{cssClass}"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        var cssClass = "test-class";

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div class=""bit-spr bit-spr-hrz bit-spr-ctr {cssClass}"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitSeparatorShouldRespectId(string id)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-spr bit-spr-hrz bit-spr-ctr""></div>");
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitSeparatorShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-spr bit-spr-hrz bit-spr-ctr{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div dir=""ltr"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitSeparatorShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<div style=""visibility: hidden;"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<div style=""display: none;"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
                break;
        }
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<div style=""display: none;"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitSeparatorShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<div aria-label=""{ariaLabel}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [DataTestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitSeparatorShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            if (childContent is not null)
            {
                parameters.AddChildContent(childContent);
            }
        });

        if (childContent is not null)
        {
            component.MarkupMatches(@$"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div class=""bit-spr-cnt"" role=""separator"" aria-orientation=""horizontal"">
                                        {childContent}
                                      </div>
                                  </div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitSeparatorHtmlAttributesTest>();

        component.MarkupMatches(@"<div data-val-test=""bit"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div class=""bit-spr-cnt"" role=""separator"" aria-orientation=""horizontal"">
                                        I'm a separator
                                      </div>
                                  </div>");
    }

    [DataTestMethod,
        DataRow(BitSeparatorAlignContent.Start),
        DataRow(BitSeparatorAlignContent.Center),
        DataRow(BitSeparatorAlignContent.End)
    ]
    public void BitSeparatorShouldRespectAlignContent(BitSeparatorAlignContent alignContent)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.AlignContent, alignContent);
        });

        var cssClass = alignContent switch
        {
            BitSeparatorAlignContent.Start => "bit-spr-srt",
            BitSeparatorAlignContent.End => "bit-spr-end",
            _ => "bit-spr-ctr"
        };

        component.MarkupMatches(@$"<div class=""bit-spr bit-spr-hrz {cssClass}"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSeparatorShouldRespectVertical(bool vertical)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Vertical, vertical);
            parameters.AddChildContent("Bit Blazor UI");
        });

        var cssClass = vertical ? "bit-spr-vrt" : "bit-spr-hrz";
        var ariaOrientation = vertical ? "vertical" : "horizontal";

        component.MarkupMatches(@$"<div class=""bit-spr {cssClass} bit-spr-ctr"" id:ignore>
                                      <div class=""bit-spr-cnt"" role=""separator"" aria-orientation=""{ariaOrientation}"">
                                        Bit Blazor UI
                                      </div>
                                    </div>");
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectVerticalChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.AddChildContent("Bit Blazor UI");
        });

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div class=""bit-spr-cnt"" role=""separator"" aria-orientation=""horizontal"">
                                        Bit Blazor UI
                                      </div>
                                    </div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore>
                                      <div class=""bit-spr-cnt"" role=""separator"" aria-orientation=""vertical"">
                                        Bit Blazor UI
                                      </div>
                                    </div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSeparatorShouldRespectAutoSize(bool autoSize)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.AutoSize, autoSize);
        });

        if (autoSize)
        {
            component.MarkupMatches(@"<div style=""width:auto"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectAutoSizeChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.AutoSize, true);
        });

        component.MarkupMatches(@"<div style=""width:auto"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(BitColorKind.Primary),
        DataRow(BitColorKind.Secondary),
        DataRow(BitColorKind.Tertiary),
        DataRow(BitColorKind.Transparent)
    ]
    public void BitSeparatorShouldRespectBackground(BitColorKind? background)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Background, background);
        });

        var cssClass = background switch
        {
            BitColorKind.Primary => "bit-spr-pbg",
            BitColorKind.Secondary => "bit-spr-sbg",
            BitColorKind.Tertiary => "bit-spr-tbg",
            BitColorKind.Transparent => "bit-spr-rbg",
            _ => null
        };

        component.MarkupMatches(@$"<div class=""{cssClass} bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectBackgroundChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Background, BitColorKind.Secondary);
        });

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-sbg bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(BitColorKind.Primary),
        DataRow(BitColorKind.Secondary),
        DataRow(BitColorKind.Tertiary),
        DataRow(BitColorKind.Transparent)
    ]
    public void BitSeparatorShouldRespectBorder(BitColorKind? border)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Border, border);
        });

        var cssClass = border switch
        {
            BitColorKind.Primary => "bit-spr-pbr",
            BitColorKind.Secondary => "bit-spr-sbr",
            BitColorKind.Tertiary => "bit-spr-tbr",
            BitColorKind.Transparent => "bit-spr-rbr",
            _ => null
        };

        component.MarkupMatches(@$"<div class=""{cssClass} bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectBorderChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Border, BitColorKind.Secondary);
        });

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-sbr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSeparatorShouldRespectAutoSizeInVertical(bool autoSize)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.AutoSize, autoSize);
            parameters.Add(p => p.Vertical, true);
        });

        if (autoSize)
        {
            component.MarkupMatches(@"<div style=""height:auto"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore></div>");
        }
    }

    [DataTestMethod]
    public void BitSeparatorShouldRespectAutoSizeChangingAfterRenderInVertical()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        component.MarkupMatches(@"<div class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.AutoSize, true);
        });

        component.MarkupMatches(@"<div style=""height:auto"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore></div>");
    }
}
