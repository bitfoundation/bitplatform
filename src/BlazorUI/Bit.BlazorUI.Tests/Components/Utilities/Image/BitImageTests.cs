using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;
using System;
using System.Text;
using System.Collections.Generic;

namespace Bit.BlazorUI.Tests.Components.Utilities.Image;

[TestClass]
public class BitImageTests : BunitTestContext
{
    [DataTestMethod]
    public void BitImageShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div class=""bit-img bit-img-fde{cssClass}"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod]
    public void BitImageShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde bit-dis"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitImageShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
    }

    [DataTestMethod]
    public void BitImageShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");

        var style = "padding: 1rem;";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("margin: 14px;", "padding: 1rem;"),
        DataRow("background-color: #fff;", "border: 2px solid red"),
    ]
    public void BitImageShouldRespectStyles(string rootStyle, string imageStyle)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Styles, new() { Root = rootStyle, Image = imageStyle });
        });

        component.MarkupMatches(@$"<div style=""{rootStyle}"" class=""bit-img bit-img-fde"" id:ignore><img style=""{imageStyle}"" class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod, Ignore]
    public void BitImageShouldRespectStylesChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");

        var rootStyle = "padding: 1rem;";
        var imageStyle = "margin: 1rem;";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Styles, new() { Root = rootStyle, Image = imageStyle });
        });

        component.MarkupMatches(@$"<div style=""{rootStyle}"" class=""bit-img bit-img-fde"" id:ignore><img style=""{imageStyle}"" class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitImageShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div class=""bit-img bit-img-fde{cssClass}"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod]
    public void BitImageShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");

        var cssClass = "test-class";

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div class=""bit-img bit-img-fde {cssClass}"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("test-root-class", "test-image-class")
    ]
    public void BitImageShouldRespectClasses(string rootClass, string imageClass)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Root = rootClass, Image = imageClass });
        });

        component.MarkupMatches(@$"<div class=""{rootClass} bit-img bit-img-fde"" id:ignore><img class=""{imageClass} bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod, Ignore]
    public void BitImageShouldRespectClassesChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");

        var rootClass = "test-root-class";
        var imageClass = "test-image-class";
        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Root = rootClass, Image = imageClass });
        });

        component.MarkupMatches(@$"<div class=""{rootClass} bit-img bit-img-fde"" id:ignore><img class=""{imageClass} bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitImageShouldRespectId(string id)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-img bit-img-fde""><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitImageShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-img bit-img-fde{cssClass}"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
    }

    [DataTestMethod]
    public void BitImageShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div dir=""ltr"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitImageShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var styleAttribute = visibility switch
        {
            BitVisibility.Hidden => @"style=""visibility: hidden;""",
            BitVisibility.Collapsed => @"style=""display: none;""",
            _ => null
        };

        component.MarkupMatches(@$"<div {styleAttribute} class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod]
    public void BitImageShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<div style=""display: none;"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod]
    public void BitImageShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitImageHtmlAttributesTest>();

        component.MarkupMatches(@"<div data-val-test=""bit"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" src=""images/bit-logo-blue.png"" /></div>");
    }

    [DataTestMethod,
        DataRow("BitBlazor"),
        DataRow("Bit Platform"),
        DataRow(null)
    ]
    public void BitImageShouldRespectAlt(string alt)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Alt, alt);
        });

        if (alt.HasValue())
        {
            component.MarkupMatches(@$"<div class=""bit-img bit-img-fde"" id:ignore><img alt=""{alt}"" class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
    }

    [DataTestMethod,
        DataRow(BitImageCoverStyle.Landscape),
        DataRow(BitImageCoverStyle.Portrait),
        DataRow(null)
    ]
    public void BitImageShouldRespectCoverStyle(BitImageCoverStyle? coverStyle)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.CoverStyle, coverStyle);
        });

        if (coverStyle is BitImageCoverStyle.Landscape)
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-lan bit-img-img bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-por bit-img-img bit-img-vis"" /></div>");
        }
    }

    [DataTestMethod,
        DataRow("85.69"),
        DataRow("69px"),
        DataRow("69rem"),
        DataRow(null)
    ]
    public void BitImageShouldRespectHeight(string height)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Height, height);
        });

        if (height.HasValue())
        {
            string style;
            if (double.TryParse(height, out double result))
            {
                style = $"height:{FormattableString.Invariant($"{result}px")}";
            }
            else
            {
                style = $"height:{height}";
            }

            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-ihh bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
    }

    [DataTestMethod]
    public void BitImageShouldRespectHeightChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Height, "85px");
        });

        component.MarkupMatches(@"<div style=""height:85px"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-ihh bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("85.69"),
        DataRow("69px"),
        DataRow("69rem"),
        DataRow(null)
    ]
    public void BitImageShouldRespectWidth(string width)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Width, width);
        });

        if (width.HasValue())
        {
            string style;
            if (double.TryParse(width, out double result))
            {
                style = $"width:{FormattableString.Invariant($"{result}px")}";
            }
            else
            {
                style = $"width:{width}";
            }

            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-ihw bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
    }

    [DataTestMethod]
    public void BitImageShouldRespectWidthChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Width, "85px");
        });

        component.MarkupMatches(@"<div style=""width:85px"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-ihw bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("85", "69"),
        DataRow("85rem", "69rem"),
        DataRow("85rem", "69px"),
        DataRow("85", "69rem"),
    ]
    public void BitImageShouldRespectHeightAndWidth(string height, string width)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Width, width);
            parameters.Add(p => p.Height, height);
        });

        StringBuilder style = new();
        if (double.TryParse(width, out double w))
        {
            style.Append($"width:{FormattableString.Invariant($"{w}px")};");
        }
        else
        {
            style.Append($"width:{width};");
        }

        if (double.TryParse(height, out double h))
        {
            style.Append($"height:{FormattableString.Invariant($"{h}px")};");
        }
        else
        {
            style.Append($"height:{height};");
        }

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("dir", "ltr"),
         DataRow("foo", "bar")
    ]
    public void BitImageShouldRespectImageAttributes(string attrKey, string attrValue)
    {
        var imageHtmlAttributes = new Dictionary<string, object> { { attrKey, attrValue } };
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.ImageAttributes, imageHtmlAttributes);
        });

        string attr = @$"{attrKey}=""{attrValue}""";

        component.MarkupMatches(@$"<div class=""bit-img bit-img-fde"" id:ignore><img {attr} class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow(BitImageFit.None),
        DataRow(BitImageFit.Center),
        DataRow(BitImageFit.Contain),
        DataRow(BitImageFit.Cover),
        DataRow(BitImageFit.CenterCover),
        DataRow(BitImageFit.CenterContain),
        DataRow(null)
    ]
    public void BitImageShouldRespectImageFit(BitImageFit? imageFit)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.ImageFit, imageFit);
        });

        var cssClass = imageFit switch
        {
            BitImageFit.None => " bit-img-non",
            BitImageFit.Center => " bit-img-ctr",
            BitImageFit.Contain => " bit-img-cnt",
            BitImageFit.Cover => " bit-img-cvr",
            BitImageFit.CenterCover => " bit-img-ccv",
            BitImageFit.CenterContain => " bit-img-cct",
            _ => null
        };

        component.MarkupMatches(@$"<div class=""bit-img bit-img-fde"" id:ignore><img class=""{cssClass} bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("85px", "69px", BitImageFit.Center),
        DataRow("85px", "69px", null),
        DataRow("85px", null, BitImageFit.Center),
        DataRow(null, "69px", BitImageFit.Center),
        DataRow(null, null, BitImageFit.Center),
        DataRow(null, "69px", null),
        DataRow(null, null, null),
    ]
    public void BitImageShouldRespectHeightWidthAndImageFit(string height, string width, BitImageFit? imageFit)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Width, width);
            parameters.Add(p => p.Height, height);
            parameters.Add(p => p.ImageFit, imageFit);
        });

        StringBuilder style = new();
        if (width.HasValue())
        {
            style.Append($"width:{width};");
        }

        if (height.HasValue())
        {
            style.Append($"height:{height};");
        }

        StringBuilder cssClass = new();
        if (imageFit.HasValue)
        {
            cssClass.Append("bit-img-ctr");
        }

        if (imageFit.HasValue is false && (width.HasValue() ^ height.HasValue()))
        {
            if (width.HasValue())
            {
                cssClass.Append(" bit-img-ihw");
            }
            else
            {
                cssClass.Append(" bit-img-ihh");
            }
        }

        if (style.Length > 0)
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img bit-img-fde"" id:ignore><img class=""{cssClass} bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img bit-img-fde"" id:ignore><img class=""{cssClass} bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
    }

    [DataTestMethod,
        DataRow("lazy"),
        DataRow(null)
    ]
    public void BitImageShouldRespectLoading(string loading)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Loading, loading);
        });

        if (loading.HasValue())
        {
            component.MarkupMatches(@$"<div class=""bit-img bit-img-fde"" id:ignore><img loading=""{loading}"" class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectMaximizeFrame(bool maximizeFrame)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.MaximizeFrame, maximizeFrame);
        });

        var cssClass = maximizeFrame ? " bit-img-max" : null;

        component.MarkupMatches(@$"<div class=""bit-img bit-img-fde{cssClass}"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectOnClick(bool isEnabled)
    {
        int clickedValue = 0;
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var image = component.Find(".bit-img > img");
        image.Click();

        var expected = isEnabled ? 1 : 0;
        Assert.AreEqual(expected, clickedValue);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectShouldFadeIn(bool shouldFadeIn)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.ShouldFadeIn, shouldFadeIn);
        });

        var cssClass = shouldFadeIn ? " bit-img-fde" : null;

        component.MarkupMatches(@$"<div class=""bit-img{cssClass}"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectShouldStartVisible(bool shouldStartVisible)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.ShouldStartVisible, shouldStartVisible);
        });

        var cssClass = shouldStartVisible ? " bit-img-vis" : null;

        component.MarkupMatches(@$"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por {cssClass}"" /></div>");
    }

    [DataTestMethod,
        DataRow("bit-logo-blue.png"),
        DataRow("https://blazorui.bitplatform.dev/bit-logo-blue.png"),
        DataRow(null)
    ]
    public void BitImageShouldRespectSrc(string src)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, src);
        });

        if (src.HasValue())
        {
            component.MarkupMatches(@$"<div class=""bit-img bit-img-fde"" id:ignore><img src=""{src}"" class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
    }

    [DataTestMethod,
        DataRow("Bit title"),
        DataRow(null)
    ]
    public void BitImageShouldRespectTitle(string title)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Title, title);
        });

        if (title.HasValue())
        {
            component.MarkupMatches(@$"<div class=""bit-img bit-img-fde"" id:ignore><img title=""{title}"" class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-por bit-img-vis"" /></div>");
        }
    }
}
