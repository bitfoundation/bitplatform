using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Separator;

[TestClass]
public class BitSeparatorTests : BunitTestContext
{
    [TestMethod]
    public void BitSeparatorShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr bit-dis"" id:ignore></div>");
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<div role=""separator"" style=""{style}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        var style = "padding: 1rem;";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div role=""separator"" style=""{style}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        var cssClass = "test-class";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr {cssClass}"" id:ignore></div>");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div role=""separator"" id=""{expectedId}"" class=""bit-spr bit-spr-hrz bit-spr-ctr""></div>");
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<div role=""separator"" dir=""{dir.Value.ToString().ToLower()}"" class=""bit-spr bit-spr-hrz bit-spr-ctr{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div role=""separator"" dir=""ltr"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
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
                component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<div role=""separator"" style=""visibility: hidden;"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<div role=""separator"" style=""display: none;"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
                break;
        }
    }

    [TestMethod]
    public void BitSeparatorShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<div role=""separator"" style=""display: none;"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<div role=""separator"" aria-label=""{ariaLabel}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [TestMethod,
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
            var contentId = $"{component.Instance.UniqueId}-cnt";

            component.MarkupMatches(@$"<div role=""separator"" aria-labelledby=""{contentId}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        {childContent}
                                      </div>
                                  </div>");
        }
        else
        {
            component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldNotRenderAriaLabelledbyWhenAriaLabelIsSet()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Bit Blazor UI");
            parameters.AddChildContent("Bit Blazor UI");
        });

        var contentId = $"{component.Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""separator"" aria-label=""Bit Blazor UI"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                  </div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitSeparatorHtmlAttributesTest>();

        var separator = component.FindComponent<BitSeparator>();
        var contentId = $"{separator.Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""separator"" aria-labelledby=""{contentId}"" data-val-test=""bit"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        I'm a separator
                                      </div>
                                  </div>");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div role=""separator"" class=""bit-spr bit-spr-hrz {cssClass}"" id:ignore></div>");
    }

    [TestMethod,
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

        var contentId = $"{component.Instance.UniqueId}-cnt";
        var cssClass = vertical ? "bit-spr-vrt" : "bit-spr-hrz";
        var ariaOrientation = vertical ? @" aria-orientation=""vertical""" : null;

        component.MarkupMatches(@$"<div role=""separator""{ariaOrientation} aria-labelledby=""{contentId}"" class=""bit-spr {cssClass} bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                    </div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectVerticalChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.AddChildContent("Bit Blazor UI");
        });

        var contentId = $"{component.Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""separator"" aria-labelledby=""{contentId}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                    </div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        component.MarkupMatches(@$"<div role=""separator"" aria-orientation=""vertical"" aria-labelledby=""{contentId}"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                    </div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSeparatorShouldRespectDecorative(bool decorative)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Decorative, decorative);
        });

        var role = decorative ? "none" : "separator";

        component.MarkupMatches(@$"<div role=""{role}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldNotRenderAriaAttributesWhenDecorative()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Decorative, true);
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.AriaLabel, "Bit Blazor UI");
            parameters.AddChildContent("Bit Blazor UI");
        });

        var contentId = $"{component.Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""none"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                  </div>");
    }

    [TestMethod,
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
            component.MarkupMatches(@"<div role=""separator"" style=""width:auto"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldRespectAutoSizeChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoSize, true);
        });

        component.MarkupMatches(@"<div role=""separator"" style=""width:auto"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
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
            component.MarkupMatches(@"<div role=""separator"" aria-orientation=""vertical"" style=""height:auto;align-self:auto"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div role=""separator"" aria-orientation=""vertical"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldRespectAutoSizeChangingAfterRenderInVertical()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        component.MarkupMatches(@"<div role=""separator"" aria-orientation=""vertical"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoSize, true);
        });

        component.MarkupMatches(@"<div role=""separator"" aria-orientation=""vertical"" style=""height:auto;align-self:auto"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
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
            BitColorKind.Primary => "bit-spr-bpg",
            BitColorKind.Secondary => "bit-spr-bsg",
            BitColorKind.Tertiary => "bit-spr-btg",
            BitColorKind.Transparent => "bit-spr-brg",
            _ => null
        };

        component.MarkupMatches(@$"<div role=""separator"" class=""{cssClass} bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectBackgroundChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Background, BitColorKind.Secondary);
        });

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-bsg bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
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
            BitColorKind.Primary => "bit-spr-bpr",
            BitColorKind.Secondary => "bit-spr-bsr",
            BitColorKind.Tertiary => "bit-spr-btr",
            BitColorKind.Transparent => "bit-spr-brr",
            _ => null
        };

        component.MarkupMatches(@$"<div role=""separator"" class=""{cssClass} bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectBorderChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Border, BitColorKind.Secondary);
        });

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-bsr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(null),
        DataRow(BitColor.Primary),
        DataRow(BitColor.Secondary),
        DataRow(BitColor.Tertiary),
        DataRow(BitColor.Info),
        DataRow(BitColor.Success),
        DataRow(BitColor.Warning),
        DataRow(BitColor.SevereWarning),
        DataRow(BitColor.Error),
        DataRow(BitColor.PrimaryBackground),
        DataRow(BitColor.SecondaryBackground),
        DataRow(BitColor.TertiaryBackground),
        DataRow(BitColor.PrimaryForeground),
        DataRow(BitColor.SecondaryForeground),
        DataRow(BitColor.TertiaryForeground),
        DataRow(BitColor.PrimaryBorder),
        DataRow(BitColor.SecondaryBorder),
        DataRow(BitColor.TertiaryBorder)
    ]
    public void BitSeparatorShouldRespectColor(BitColor? color)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var cssClass = color switch
        {
            BitColor.Primary => "bit-spr-pri",
            BitColor.Secondary => "bit-spr-sec",
            BitColor.Tertiary => "bit-spr-ter",
            BitColor.Info => "bit-spr-inf",
            BitColor.Success => "bit-spr-suc",
            BitColor.Warning => "bit-spr-wrn",
            BitColor.SevereWarning => "bit-spr-swr",
            BitColor.Error => "bit-spr-err",
            BitColor.PrimaryBackground => "bit-spr-pbg",
            BitColor.SecondaryBackground => "bit-spr-sbg",
            BitColor.TertiaryBackground => "bit-spr-tbg",
            BitColor.PrimaryForeground => "bit-spr-pfg",
            BitColor.SecondaryForeground => "bit-spr-sfg",
            BitColor.TertiaryForeground => "bit-spr-tfg",
            BitColor.PrimaryBorder => "bit-spr-pbr",
            BitColor.SecondaryBorder => "bit-spr-sbr",
            BitColor.TertiaryBorder => "bit-spr-tbr",
            _ => null
        };

        component.MarkupMatches(@$"<div role=""separator"" class=""{cssClass} bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectColorChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Success);
        });

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-suc bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(null),
        DataRow(BitSeparatorLineStyle.Solid),
        DataRow(BitSeparatorLineStyle.Dashed),
        DataRow(BitSeparatorLineStyle.Dotted),
        DataRow(BitSeparatorLineStyle.Double)
    ]
    public void BitSeparatorShouldRespectLineStyle(BitSeparatorLineStyle? lineStyle)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.LineStyle, lineStyle);
        });

        var cssClass = lineStyle switch
        {
            BitSeparatorLineStyle.Dashed => "bit-spr-dsh ",
            BitSeparatorLineStyle.Dotted => "bit-spr-dot ",
            BitSeparatorLineStyle.Double => "bit-spr-dbl ",
            _ => null
        };

        component.MarkupMatches(@$"<div role=""separator"" class=""{cssClass}bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectLineStyleChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.LineStyle, BitSeparatorLineStyle.Dashed);
        });

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-dsh bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("3px"),
        DataRow("0.5rem"),
        DataRow(null)
    ]
    public void BitSeparatorShouldRespectThickness(string thickness)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Thickness, thickness);
        });

        if (thickness.HasValue())
        {
            component.MarkupMatches(@$"<div role=""separator"" style=""--bit-spr-siz:{thickness}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldRespectThicknessChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Thickness, "3px");
        });

        component.MarkupMatches(@"<div role=""separator"" style=""--bit-spr-siz:3px"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("2rem"),
        DataRow("10%"),
        DataRow(null)
    ]
    public void BitSeparatorShouldRespectContentOffset(string contentOffset)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.ContentOffset, contentOffset);
        });

        if (contentOffset.HasValue())
        {
            component.MarkupMatches(@$"<div role=""separator"" style=""--bit-spr-ofs:{contentOffset}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldRespectContentOffsetChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.ContentOffset, "2rem");
        });

        component.MarkupMatches(@"<div role=""separator"" style=""--bit-spr-ofs:2rem"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectClassStyles()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitSeparatorClassStyles { Root = "custom-root", Content = "custom-content" });
            parameters.Add(p => p.Styles, new BitSeparatorClassStyles { Root = "margin: 1rem;", Content = "color: red;" });
            parameters.AddChildContent("Bit Blazor UI");
        });

        var contentId = $"{component.Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""separator"" aria-labelledby=""{contentId}"" style=""margin: 1rem;"" class=""custom-root bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" style=""color: red;"" class=""bit-spr-cnt custom-content"">
                                        Bit Blazor UI
                                      </div>
                                  </div>");
    }
    [TestMethod,
        DataRow(null),
        DataRow(BitSize.Small),
        DataRow(BitSize.Medium),
        DataRow(BitSize.Large)
    ]
    public void BitSeparatorShouldRespectSize(BitSize? size)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var cssClass = size switch
        {
            BitSize.Small => "bit-spr-sm ",
            BitSize.Medium => "bit-spr-md ",
            BitSize.Large => "bit-spr-lg ",
            _ => null
        };

        component.MarkupMatches(@$"<div role=""separator"" class=""{cssClass}bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectSizeChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
        });

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-lg bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRenderThicknessAlongsideSize()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
            parameters.Add(p => p.Thickness, "5px");
        });

        // The size class only sets the custom property the line reads, so the inline Thickness
        // always wins over it in the cascade.
        component.MarkupMatches(@"<div role=""separator"" style=""--bit-spr-siz:5px"" class=""bit-spr bit-spr-lg bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("2rem"),
        DataRow("25%"),
        DataRow(null)
    ]
    public void BitSeparatorShouldRespectInset(string inset)
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Inset, inset);
        });

        if (inset.HasValue())
        {
            component.MarkupMatches(@$"<div role=""separator"" style=""--bit-spr-ins:{inset}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldRespectInsetChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>();

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Inset, "2rem");
        });

        component.MarkupMatches(@"<div role=""separator"" style=""--bit-spr-ins:2rem"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRenderEveryLengthCustomProperty()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.AutoSize, true);
            parameters.Add(p => p.Thickness, "3px");
            parameters.Add(p => p.ContentOffset, "2rem");
            parameters.Add(p => p.Inset, "1rem");
            parameters.Add(p => p.AlignContent, BitSeparatorAlignContent.Start);
            parameters.AddChildContent("Bit Blazor UI");
        });

        var contentId = $"{component.Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""separator"" aria-labelledby=""{contentId}"" style=""width:auto;--bit-spr-siz:3px;--bit-spr-ofs:2rem;--bit-spr-ins:1rem"" class=""bit-spr bit-spr-hrz bit-spr-srt"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                  </div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRenderContentWrapperInVertical()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.AlignContent, BitSeparatorAlignContent.End);
            parameters.Add(p => p.ContentOffset, "25%");
            parameters.AddChildContent("<span>Bit</span>");
        });

        var contentId = $"{component.Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""separator"" aria-orientation=""vertical"" aria-labelledby=""{contentId}"" style=""--bit-spr-ofs:25%"" class=""bit-spr bit-spr-vrt bit-spr-end"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        <span>Bit</span>
                                      </div>
                                  </div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRespectDecorativeChangingAfterRender()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.AddChildContent("Bit Blazor UI");
        });

        var contentId = $"{component.Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""separator"" aria-orientation=""vertical"" aria-labelledby=""{contentId}"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                  </div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Decorative, true);
        });

        component.MarkupMatches(@$"<div role=""none"" class=""bit-spr bit-spr-vrt bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                  </div>");
    }

    [TestMethod]
    public void BitSeparatorShouldDropAriaLabelledbyWhenChildContentIsRemoved()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.AddChildContent("Bit Blazor UI");
        });

        var contentId = $"{component.Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""separator"" aria-labelledby=""{contentId}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                  </div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.ChildContent, (Microsoft.AspNetCore.Components.RenderFragment?)null);
        });

        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSeparatorShouldNameContentWrapperAfterTheId()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Id, "test-id");
            parameters.AddChildContent("Bit Blazor UI");
        });

        component.MarkupMatches(@"<div role=""separator"" aria-labelledby=""test-id-cnt"" id=""test-id"" class=""bit-spr bit-spr-hrz bit-spr-ctr"">
                                      <div id=""test-id-cnt"" class=""bit-spr-cnt"">
                                        Bit Blazor UI
                                      </div>
                                  </div>");
    }

    [TestMethod]
    public void BitSeparatorShouldRenderBothColorAndBorderClasses()
    {
        var component = RenderComponent<BitSeparator>(parameters =>
        {
            parameters.Add(p => p.Border, BitColorKind.Tertiary);
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Background, BitColorKind.Secondary);
        });

        // Both tiers render their class; the stylesheet declares the Color tier last, so it wins.
        component.MarkupMatches(@"<div role=""separator"" class=""bit-spr bit-spr-bsg bit-spr-btr bit-spr-err bit-spr-hrz bit-spr-ctr"" id:ignore></div>");
    }
}
