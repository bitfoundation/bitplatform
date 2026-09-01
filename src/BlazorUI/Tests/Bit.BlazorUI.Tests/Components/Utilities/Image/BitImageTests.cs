using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ErrorEventArgs = Microsoft.AspNetCore.Components.Web.ErrorEventArgs;

namespace Bit.BlazorUI.Tests.Components.Utilities.Image;

[TestClass]
public class BitImageTests : BunitTestContext
{
    /// <summary>The img of a component with nothing set: hidden, portrait, and never without an alt.</summary>
    private const string DefaultImage = @"<img alt="""" class=""bit-img-img bit-img-por bit-img-hid"" />";



    [TestMethod]
    public void BitImageShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div class=""bit-img{cssClass}"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod]
    public void BitImageShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@$"<div class=""bit-img bit-dis"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }

    [TestMethod]
    public void BitImageShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        var style = "padding: 1rem;";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
        DataRow("margin: 14px;", "padding: 1rem;"),
        DataRow("background-color: #fff;", "border: 2px solid red"),
    ]
    public void BitImageShouldRespectStyles(string rootStyle, string imageStyle)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Styles, new() { Root = rootStyle, Image = imageStyle });
        });

        component.MarkupMatches(@$"<div style=""{rootStyle}"" class=""bit-img"" id:ignore><img alt="""" style=""{imageStyle}"" class=""bit-img-img bit-img-por bit-img-hid"" /></div>");
    }

    [TestMethod]
    public void BitImageShouldRespectStylesForEveryPart()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.PlaceholderSrc, "placeholder.png");
            parameters.Add(p => p.Styles, new()
            {
                Root = "margin:1rem",
                Placeholder = "opacity:0.5",
                Image = "padding:1rem",
                LoadingTemplate = "color:red",
                Content = "color:blue"
            });
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(builder => builder.AddContent(0, "loading")));
            parameters.AddChildContent("overlay");
        });

        var markup = component.Markup;

        StringAssert.Contains(markup, @"opacity:0.5");
        StringAssert.Contains(markup, @"padding:1rem");
        StringAssert.Contains(markup, @"color:red");
        StringAssert.Contains(markup, @"color:blue");
    }

    [TestMethod]
    public void BitImageShouldRespectStylesChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        var rootStyle = "padding: 1rem;";
        var imageStyle = "margin: 1rem;";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Styles, new() { Root = rootStyle, Image = imageStyle });
        });

        component.MarkupMatches(@$"<div style=""{rootStyle}"" class=""bit-img"" id:ignore><img alt="""" style=""{imageStyle}"" class=""bit-img-img bit-img-por bit-img-hid"" /></div>");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div class=""bit-img{cssClass}"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod]
    public void BitImageShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        var cssClass = "test-class";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div class=""bit-img {cssClass}"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
        DataRow("test-root-class", "test-image-class")
    ]
    public void BitImageShouldRespectClasses(string rootClass, string imageClass)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Root = rootClass, Image = imageClass });
        });

        component.MarkupMatches(@$"<div class=""{rootClass} bit-img"" id:ignore><img alt="""" class=""{imageClass} bit-img-img bit-img-por bit-img-hid"" /></div>");
    }

    [TestMethod]
    public void BitImageShouldRespectClassesChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        var rootClass = "test-root-class";
        var imageClass = "test-image-class";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Root = rootClass, Image = imageClass });
        });

        component.MarkupMatches(@$"<div class=""{rootClass} bit-img"" id:ignore><img alt="""" class=""{imageClass} bit-img-img bit-img-por bit-img-hid"" /></div>");
    }

    [TestMethod]
    public void BitImageShouldRespectClassesForEveryPart()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.PlaceholderSrc, "placeholder.png");
            parameters.Add(p => p.Classes, new()
            {
                Root = "root-class",
                Placeholder = "placeholder-class",
                Image = "image-class",
                LoadingTemplate = "loading-class",
                Content = "content-class"
            });
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(builder => builder.AddContent(0, "loading")));
            parameters.AddChildContent("overlay");
        });

        Assert.IsTrue(component.Find(".bit-img").ClassList.Contains("root-class"));
        Assert.IsTrue(component.Find(".bit-img-plc").ClassList.Contains("placeholder-class"));
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("image-class"));
        Assert.IsTrue(component.Find(".bit-img-tpl").ClassList.Contains("loading-class"));
        Assert.IsTrue(component.Find(".bit-img-ovl").ClassList.Contains("content-class"));
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-img"">{DefaultImage}</div>");
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-img{cssClass}"" id:ignore>{DefaultImage}</div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }

    [TestMethod]
    public void BitImageShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@$"<div dir=""ltr"" class=""bit-img"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div {styleAttribute} class=""bit-img"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod]
    public void BitImageShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@$"<div style=""display: none;"" class=""bit-img"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod]
    public void BitImageShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitImageHtmlAttributesTest>();

        var frames = component.FindAll(".bit-img");

        Assert.AreEqual("bit", frames[0].GetAttribute("data-val-test"));
        Assert.AreEqual("images/bit-logo-blue.png", component.FindAll(".bit-img-img")[0].GetAttribute("src"));
    }

    [TestMethod]
    public void BitImageShouldMergeImageAttributesWithItsOwnAttributes()
    {
        var component = RenderComponent<BitImageHtmlAttributesTest>();

        var image = component.FindAll(".bit-img-img")[1];

        // An attribute with a parameter of its own that is not set must survive the merge, since the
        // parameter is what would otherwise remove it.
        Assert.AreEqual("splatted.png 2x", image.GetAttribute("srcset"));

        // And one with no parameter at all reaches the img untouched.
        Assert.AreEqual("#map", image.GetAttribute("usemap"));

        // What the parameters do set wins over the dictionary.
        Assert.AreEqual("merged", image.GetAttribute("alt"));
        Assert.AreEqual("images/bit-logo-blue.png", image.GetAttribute("src"));

        // The two styles are joined rather than one replacing the other.
        StringAssert.Contains(image.GetAttribute("style"), "margin:1rem");
        StringAssert.Contains(image.GetAttribute("style"), "padding:1rem");
    }

    [TestMethod,
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

        // The attribute is always rendered: an image with no text of its own is decorative rather than
        // one whose name a screen reader has to guess at from the file name.
        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore><img alt=""{alt}"" class=""bit-img-img bit-img-por bit-img-hid"" /></div>");
    }

    [TestMethod,
        DataRow(BitImageCover.Landscape),
        DataRow(BitImageCover.Portrait),
        DataRow(null)
    ]
    public void BitImageShouldRespectCoverStyle(BitImageCover? coverStyle)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Cover, coverStyle);
        });

        if (coverStyle is BitImageCover.Landscape)
        {
            component.MarkupMatches(@"<div class=""bit-img"" id:ignore><img alt="""" class=""bit-img-lan bit-img-img bit-img-hid"" /></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }

    [TestMethod,
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
            if (double.TryParse(height, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                style = $"height:{FormattableString.Invariant($"{result}px")}";
            }
            else
            {
                style = $"height:{height}";
            }

            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img"" id:ignore><img alt="""" class=""bit-img-ihh bit-img-img bit-img-por bit-img-hid"" /></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }

    [TestMethod]
    public void BitImageShouldRespectHeightChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Height, "85px");
        });

        component.MarkupMatches(@"<div style=""height:85px"" class=""bit-img"" id:ignore><img alt="""" class=""bit-img-ihh bit-img-img bit-img-por bit-img-hid"" /></div>");
    }

    [TestMethod,
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
            if (double.TryParse(width, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                style = $"width:{FormattableString.Invariant($"{result}px")}";
            }
            else
            {
                style = $"width:{width}";
            }

            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img"" id:ignore><img alt="""" class=""bit-img-ihw bit-img-img bit-img-por bit-img-hid"" /></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }

    [TestMethod]
    public void BitImageShouldRespectWidthChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Width, "85px");
        });

        component.MarkupMatches(@"<div style=""width:85px"" class=""bit-img"" id:ignore><img alt="""" class=""bit-img-ihw bit-img-img bit-img-por bit-img-hid"" /></div>");
    }

    /// <summary>
    /// A bare number is a CSS length rather than a number shown to a user, so it is read with the
    /// invariant culture: parsed with a culture whose decimal separator is the comma, "85.69" would be
    /// a different length, and the CSS that came out of it would not be a length at all.
    /// </summary>
    [TestMethod,
        DataRow("de-DE"),
        DataRow("fa-IR"),
        DataRow("en-US")
    ]
    public void BitImageShouldParseBareNumbersInvariantly(string culture)
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            var component = RenderComponent<BitImage>(parameters =>
            {
                parameters.Add(p => p.Width, "85.5");
            });

            StringAssert.Contains(component.Find(".bit-img").GetAttribute("style"), "width:85.5px");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod,
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
        if (double.TryParse(width, NumberStyles.Float, CultureInfo.InvariantCulture, out double w))
        {
            style.Append($"width:{FormattableString.Invariant($"{w}px")};");
        }
        else
        {
            style.Append($"width:{width};");
        }

        if (double.TryParse(height, NumberStyles.Float, CultureInfo.InvariantCulture, out double h))
        {
            style.Append($"height:{FormattableString.Invariant($"{h}px")};");
        }
        else
        {
            style.Append($"height:{height};");
        }

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
        DataRow("16/9"),
        DataRow("1"),
        DataRow(null)
    ]
    public void BitImageShouldRespectAspectRatio(string aspectRatio)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.AspectRatio, aspectRatio);
        });

        if (aspectRatio.HasValue())
        {
            component.MarkupMatches(@$"<div style=""aspect-ratio:{aspectRatio}"" class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }

    [TestMethod]
    public void BitImageShouldRespectAspectRatioChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AspectRatio, "4/3");
        });

        component.MarkupMatches(@$"<div style=""aspect-ratio:4/3"" class=""bit-img"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore><img alt="""" {attr} class=""bit-img-img bit-img-por bit-img-hid"" /></div>");
    }

    [TestMethod,
        DataRow(BitImageFit.None),
        DataRow(BitImageFit.Center),
        DataRow(BitImageFit.Contain),
        DataRow(BitImageFit.Cover),
        DataRow(BitImageFit.CenterCover),
        DataRow(BitImageFit.CenterContain),
        DataRow(BitImageFit.Fill),
        DataRow(BitImageFit.ScaleDown),
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
            BitImageFit.Fill => " bit-img-fil",
            BitImageFit.ScaleDown => " bit-img-scd",
            _ => null
        };

        // A centered fit is placed by the frame, so it is the frame that carries a class of its own.
        var rootCssClass = imageFit is BitImageFit.Center or BitImageFit.CenterContain or BitImageFit.CenterCover
                            ? "bit-img bit-img-cen"
                            : "bit-img";

        component.MarkupMatches(@$"<div class=""{rootCssClass}"" id:ignore><img alt="""" class=""{cssClass} bit-img-img bit-img-por bit-img-hid"" /></div>");
    }

    [TestMethod,
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

        // The rows that pass a fit all pass the centered one, which is placed by the frame.
        var rootCssClass = imageFit.HasValue ? "bit-img bit-img-cen" : "bit-img";

        if (style.Length > 0)
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""{rootCssClass}"" id:ignore><img alt="""" class=""{cssClass} bit-img-img bit-img-por bit-img-hid"" /></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""{rootCssClass}"" id:ignore><img alt="""" class=""{cssClass} bit-img-img bit-img-por bit-img-hid"" /></div>");
        }
    }

    [TestMethod,
        DataRow("top"),
        DataRow("50% 25%"),
        DataRow(null)
    ]
    public void BitImageShouldRespectImagePosition(string imagePosition)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.ImagePosition, imagePosition);
        });

        if (imagePosition.HasValue())
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore><img alt="""" style=""object-position:{imagePosition}"" class=""bit-img-img bit-img-por bit-img-hid"" /></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }

    [TestMethod,
        DataRow(BitImageLoading.Lazy),
        DataRow(BitImageLoading.Eager),
        DataRow(null)
    ]
    public void BitImageShouldRespectLoading(BitImageLoading? loading)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Loading, loading);
        });

        if (loading.HasValue)
        {
            // A hidden lazy image keeps its box - a display:none element is never fetched at all.
            var lazyClass = loading is BitImageLoading.Lazy ? " bit-img-lzy" : null;

            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore><img alt="""" loading=""{loading.ToString()!.ToLowerInvariant()}"" class=""bit-img-img bit-img-por bit-img-hid{lazyClass}"" /></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }

    [TestMethod]
    public void BitImageShouldNotHideALazyImageOutOfTheLayout()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Loading, BitImageLoading.Lazy);
        });

        var image = component.Find(".bit-img-img");

        Assert.IsTrue(image.ClassList.Contains("bit-img-lzy"));

        // Once loaded there is nothing left to hide, so the class is gone with the hidden state.
        image.TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsFalse(component.Find(".bit-img-img").ClassList.Contains("bit-img-lzy"));
    }

    [TestMethod,
        DataRow(BitImageDecoding.Auto, "auto"),
        DataRow(BitImageDecoding.Sync, "sync"),
        DataRow(BitImageDecoding.Async, "async"),
        DataRow(null, null)
    ]
    public void BitImageShouldRespectDecoding(BitImageDecoding? decoding, string expected)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Decoding, decoding);
        });

        Assert.AreEqual(expected, component.Find(".bit-img-img").GetAttribute("decoding"));
    }

    [TestMethod,
        DataRow(BitImageFetchPriority.Auto, "auto"),
        DataRow(BitImageFetchPriority.High, "high"),
        DataRow(BitImageFetchPriority.Low, "low"),
        DataRow(null, null)
    ]
    public void BitImageShouldRespectFetchPriority(BitImageFetchPriority? fetchPriority, string expected)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.FetchPriority, fetchPriority);
        });

        Assert.AreEqual(expected, component.Find(".bit-img-img").GetAttribute("fetchpriority"));
    }

    [TestMethod,
        DataRow(BitImageCrossOrigin.Anonymous, "anonymous"),
        DataRow(BitImageCrossOrigin.UseCredentials, "use-credentials"),
        DataRow(null, null)
    ]
    public void BitImageShouldRespectCrossOrigin(BitImageCrossOrigin? crossOrigin, string expected)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.CrossOrigin, crossOrigin);
        });

        Assert.AreEqual(expected, component.Find(".bit-img-img").GetAttribute("crossorigin"));
    }

    [TestMethod,
        DataRow(BitImageReferrerPolicy.NoReferrer, "no-referrer"),
        DataRow(BitImageReferrerPolicy.NoReferrerWhenDowngrade, "no-referrer-when-downgrade"),
        DataRow(BitImageReferrerPolicy.Origin, "origin"),
        DataRow(BitImageReferrerPolicy.OriginWhenCrossOrigin, "origin-when-cross-origin"),
        DataRow(BitImageReferrerPolicy.SameOrigin, "same-origin"),
        DataRow(BitImageReferrerPolicy.StrictOrigin, "strict-origin"),
        DataRow(BitImageReferrerPolicy.StrictOriginWhenCrossOrigin, "strict-origin-when-cross-origin"),
        DataRow(BitImageReferrerPolicy.UnsafeUrl, "unsafe-url"),
        DataRow(null, null)
    ]
    public void BitImageShouldRespectReferrerPolicy(BitImageReferrerPolicy? referrerPolicy, string expected)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.ReferrerPolicy, referrerPolicy);
        });

        Assert.AreEqual(expected, component.Find(".bit-img-img").GetAttribute("referrerpolicy"));
    }

    [TestMethod,
        DataRow(true, "true"),
        DataRow(false, "false"),
        DataRow(null, null)
    ]
    public void BitImageShouldRespectDraggable(bool? draggable, string expected)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Draggable, draggable);
        });

        Assert.AreEqual(expected, component.Find(".bit-img-img").GetAttribute("draggable"));
    }

    [TestMethod,
        DataRow("photo-480.jpg 480w, photo-960.jpg 960w"),
        DataRow(null)
    ]
    public void BitImageShouldRespectSrcset(string srcset)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Srcset, srcset);
        });

        Assert.AreEqual(srcset, component.Find(".bit-img-img").GetAttribute("srcset"));
    }

    [TestMethod,
        DataRow("(max-width: 600px) 100vw, 50vw"),
        DataRow(null)
    ]
    public void BitImageShouldRespectSizes(string sizes)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Sizes, sizes);
        });

        Assert.AreEqual(sizes, component.Find(".bit-img-img").GetAttribute("sizes"));
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<div class=""bit-img{cssClass}"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectRounded(bool rounded)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Rounded, rounded);
        });

        var cssClass = rounded ? " bit-img-rnd" : null;

        component.MarkupMatches(@$"<div class=""bit-img{cssClass}"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectCircular(bool circular)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Circular, circular);
        });

        var cssClass = circular ? " bit-img-cir" : null;

        component.MarkupMatches(@$"<div class=""bit-img{cssClass}"" id:ignore>{DefaultImage}</div>");
    }

    /// <summary>The circle wins: a frame cannot be both, and the rounder of the two is the one asked for.</summary>
    [TestMethod]
    public void BitImageShouldPreferCircularOverRounded()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Rounded, true);
            parameters.Add(p => p.Circular, true);
        });

        var root = component.Find(".bit-img");

        Assert.IsTrue(root.ClassList.Contains("bit-img-cir"));
        Assert.IsFalse(root.ClassList.Contains("bit-img-rnd"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectShadow(bool shadow)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Shadow, shadow);
        });

        var cssClass = shadow ? " bit-img-shd" : null;

        component.MarkupMatches(@$"<div class=""bit-img{cssClass}"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectBordered(bool bordered)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Bordered, bordered);
        });

        var cssClass = bordered ? " bit-img-brd" : null;

        component.MarkupMatches(@$"<div class=""bit-img{cssClass}"" id:ignore>{DefaultImage}</div>");
    }

    [TestMethod,
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

    /// <summary>
    /// A clickable image is a button rather than a picture: reachable by the keyboard, announced as a
    /// button, and disabled visibly to assistive technologies when it is disabled.
    /// </summary>
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRenderClickableImageAsAButton(bool isEnabled)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => { });
        });

        var root = component.Find(".bit-img");
        var image = component.Find(".bit-img-img");

        Assert.AreEqual("button", image.GetAttribute("role"));
        Assert.AreEqual(isEnabled, root.ClassList.Contains("bit-img-clk"));
        Assert.AreEqual(isEnabled ? "0" : null, image.GetAttribute("tabindex"));
        Assert.AreEqual(isEnabled ? null : "true", image.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitImageShouldNotBeAButtonWithoutAClickHandler()
    {
        var component = RenderComponent<BitImage>();

        var image = component.Find(".bit-img-img");

        Assert.IsFalse(image.HasAttribute("role"));
        Assert.IsFalse(image.HasAttribute("tabindex"));
        Assert.IsFalse(component.Find(".bit-img").ClassList.Contains("bit-img-clk"));
    }

    [TestMethod,
        DataRow("test-tab-index"),
        DataRow(null)
    ]
    public void BitImageShouldRespectTabIndex(string tabIndex)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.TabIndex, tabIndex);
        });

        Assert.AreEqual(tabIndex, component.Find(".bit-img-img").GetAttribute("tabindex"));
    }

    /// <summary>An explicit tab index has the last word over the one a click handler brings with it.</summary>
    [TestMethod]
    public void BitImageShouldPreferTheExplicitTabIndexOverTheClickableOne()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.OnClick, () => { });
        });

        Assert.AreEqual("3", component.Find(".bit-img-img").GetAttribute("tabindex"));
    }

    [TestMethod,
        DataRow("Enter", true),
        DataRow(" ", true),
        DataRow("Spacebar", true),
        DataRow("a", false),
        DataRow("Tab", false)
    ]
    public void BitImageShouldRespectKeyboardActivation(string key, bool shouldActivate)
    {
        var clicked = 0;
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        var image = component.Find(".bit-img-img");

        // Enter activates on the way down and Space on the way up, exactly as a native button does.
        image.KeyDown(new KeyboardEventArgs { Key = key });
        image.KeyUp(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(shouldActivate ? 1 : 0, clicked);
    }

    [TestMethod]
    public void BitImageShouldNotActivateByKeyboardWhenDisabled()
    {
        var clicked = 0;
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        var image = component.Find(".bit-img-img");

        image.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        image.KeyUp(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual(0, clicked);
    }

    /// <summary>
    /// The fade belongs to the moment the image becomes visible rather than to the mounting of the
    /// component, so the class is on the image and only while it is shown.
    /// </summary>
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectFadeIn(bool fadeIn)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.FadeIn, fadeIn);
        });

        // Nothing to fade in while the image is still hidden.
        Assert.IsFalse(component.Find(".bit-img-img").ClassList.Contains("bit-img-fde"));

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(fadeIn, component.Find(".bit-img-img").ClassList.Contains("bit-img-fde"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectStartVisible(bool startVisible)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.StartVisible, startVisible);
        });

        var cssClass = startVisible ? " bit-img-vis" : " bit-img-hid";

        component.MarkupMatches(@$"<div class=""bit-img"" id:ignore><img alt="""" class=""bit-img-img bit-img-por{cssClass}"" /></div>");
    }

    /// <summary>An image that is visible from the start has nothing for a loading template to stand in for.</summary>
    [TestMethod]
    public void BitImageShouldNotRenderTheLoadingTemplateWhenStartVisible()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.StartVisible, true);
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(builder => builder.AddContent(0, "loading")));
        });

        Assert.AreEqual(0, component.FindAll(".bit-img-tpl").Count);
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore><img alt="""" src=""{src}"" class=""bit-img-img bit-img-por bit-img-hid"" /></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore><img alt="""" title=""{title}"" class=""bit-img-img bit-img-por bit-img-hid"" /></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-img"" id:ignore>{DefaultImage}</div>");
        }
    }



    // ---- the loading states ------------------------------------------------------------------

    [TestMethod]
    public void BitImageShouldStartInTheLoadingState()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
        });

        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
    }

    [TestMethod]
    public void BitImageShouldReachTheLoadedStateOnLoad()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
        });

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-vis"));
    }

    [TestMethod]
    public void BitImageShouldReachTheErrorStateOnError()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
        });

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.AreEqual(BitImageState.Error, component.Instance.LoadingState);
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-hid"));
    }

    [TestMethod]
    public void BitImageShouldRespectOnLoadAndOnError()
    {
        var loaded = 0;
        var errored = 0;

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.OnLoad, () => loaded++);
            parameters.Add(p => p.OnError, () => errored++);
        });

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());
        Assert.AreEqual(1, loaded);
        Assert.AreEqual(0, errored);

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());
        Assert.AreEqual(1, loaded);
        Assert.AreEqual(1, errored);
    }

    [TestMethod]
    public void BitImageShouldRespectOnLoadingStateChange()
    {
        List<BitImageState> states = [];

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.OnLoadingStateChange, s => states.Add(s));
        });

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());
        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());

        CollectionAssert.AreEqual(new[] { BitImageState.Loaded, BitImageState.Error }, states);
    }

    /// <summary>A new source is a new image, so whatever the previous one ended up as is no longer the answer.</summary>
    [TestMethod]
    public void BitImageShouldReturnToTheLoadingStateWhenTheSourceChanges()
    {
        List<BitImageState> states = [];

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "first.png");
            parameters.Add(p => p.OnLoadingStateChange, s => states.Add(s));
        });

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());
        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Src, "second.png");
        });

        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
        Assert.AreEqual("second.png", component.Find(".bit-img-img").GetAttribute("src"));
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-hid"));
        CollectionAssert.AreEqual(new[] { BitImageState.Loaded, BitImageState.Loading }, states);
    }

    [TestMethod]
    public void BitImageShouldRenderTheLoadingTemplateWhileLoading()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(builder => builder.AddContent(0, "loading...")));
            parameters.Add(p => p.ErrorTemplate, (RenderFragment)(builder => builder.AddContent(0, "error!")));
        });

        StringAssert.Contains(component.Markup, "loading...");

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(0, component.FindAll(".bit-img-tpl").Count);
    }

    [TestMethod]
    public void BitImageShouldRenderTheErrorTemplateOnError()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(builder => builder.AddContent(0, "loading...")));
            parameters.Add(p => p.ErrorTemplate, (RenderFragment)(builder => builder.AddContent(0, "error!")));
        });

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());

        StringAssert.Contains(component.Markup, "error!");
        Assert.IsFalse(component.Markup.Contains("loading..."));
    }



    // ---- the fallback and the placeholder -----------------------------------------------------

    [TestMethod]
    public void BitImageShouldSwapInTheFallbackSrcOnError()
    {
        List<BitImageState> states = [];

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "broken.png");
            parameters.Add(p => p.FallbackSrc, "fallback.png");
            parameters.Add(p => p.OnLoadingStateChange, s => states.Add(s));
        });

        Assert.AreEqual("broken.png", component.Find(".bit-img-img").GetAttribute("src"));

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());

        // The fallback is another image being fetched rather than the end of this one.
        Assert.AreEqual("fallback.png", component.Find(".bit-img-img").GetAttribute("src"));
        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
        Assert.AreEqual(0, states.Count);
    }

    [TestMethod]
    public void BitImageShouldTryTheFallbackSrcOnlyOnce()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "broken.png");
            parameters.Add(p => p.FallbackSrc, "fallback.png");
        });

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());
        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.AreEqual(BitImageState.Error, component.Instance.LoadingState);
        Assert.AreEqual("fallback.png", component.Find(".bit-img-img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitImageShouldUseTheFallbackSrcWhenThereIsNoSrc()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.FallbackSrc, "fallback.png");
        });

        Assert.AreEqual("fallback.png", component.Find(".bit-img-img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitImageShouldOfferTheFallbackAgainAfterTheSourceChanges()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "broken.png");
            parameters.Add(p => p.FallbackSrc, "fallback.png");
        });

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());
        Assert.AreEqual("fallback.png", component.Find(".bit-img-img").GetAttribute("src"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Src, "another.png");
        });

        Assert.AreEqual("another.png", component.Find(".bit-img-img").GetAttribute("src"));

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());
        Assert.AreEqual("fallback.png", component.Find(".bit-img-img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitImageShouldRespectPlaceholderSrc()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.PlaceholderSrc, "placeholder.png");
        });

        var placeholder = component.Find(".bit-img-plc");

        Assert.AreEqual("placeholder.png", placeholder.GetAttribute("src"));

        // The placeholder stands for the image, which carries the meaning, so it is not announced.
        Assert.AreEqual("true", placeholder.GetAttribute("aria-hidden"));
        Assert.AreEqual("", placeholder.GetAttribute("alt"));

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.AreEqual(0, component.FindAll(".bit-img-plc").Count);
    }

    [TestMethod]
    public void BitImageShouldNotRenderThePlaceholderWithoutASource()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
        });

        Assert.AreEqual(0, component.FindAll(".bit-img-plc").Count);
    }



    // ---- the content overlay and the public members --------------------------------------------

    [TestMethod]
    public void BitImageShouldRenderChildContentOverTheImage()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.AddChildContent("<span>caption</span>");
        });

        var overlay = component.Find(".bit-img-ovl");

        StringAssert.Contains(overlay.InnerHtml, "caption");
    }

    [TestMethod]
    public void BitImageShouldNotRenderAnOverlayWithoutChildContent()
    {
        var component = RenderComponent<BitImage>();

        Assert.AreEqual(0, component.FindAll(".bit-img-ovl").Count);
    }

    [TestMethod]
    public async Task BitImageShouldRespectReloadAsync()
    {
        List<BitImageState> states = [];

        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "broken.png");
            parameters.Add(p => p.FallbackSrc, "fallback.png");
            parameters.Add(p => p.OnLoadingStateChange, s => states.Add(s));
        });

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());
        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());
        Assert.AreEqual(BitImageState.Error, component.Instance.LoadingState);

        await component.InvokeAsync(() => component.Instance.ReloadAsync());

        // Back to the beginning: the original source, the loading state, and the fallback available again.
        Assert.AreEqual(BitImageState.Loading, component.Instance.LoadingState);
        Assert.AreEqual("broken.png", component.Find(".bit-img-img").GetAttribute("src"));
        CollectionAssert.AreEqual(new[] { BitImageState.Error, BitImageState.Loading }, states);

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());
        Assert.AreEqual("fallback.png", component.Find(".bit-img-img").GetAttribute("src"));
    }



    // ---- the alternative sources -----------------------------------------------------------------

    [TestMethod]
    public void BitImageShouldRenderSourcesInsideAPicture()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "photo.jpg");
            parameters.Add(p => p.Sources,
            [
                new BitImageSource { Srcset = "photo.avif", Type = "image/avif", Media = "(min-width: 600px)", Sizes = "50vw", Width = 800, Height = 600 },
                new BitImageSource { Srcset = "photo.webp", Type = "image/webp" }
            ]);
        });

        var picture = component.Find("picture.bit-img-pic");
        var sources = component.FindAll("picture.bit-img-pic > source");

        Assert.AreEqual(2, sources.Count);
        Assert.AreEqual("photo.avif", sources[0].GetAttribute("srcset"));
        Assert.AreEqual("image/avif", sources[0].GetAttribute("type"));
        Assert.AreEqual("(min-width: 600px)", sources[0].GetAttribute("media"));
        Assert.AreEqual("50vw", sources[0].GetAttribute("sizes"));
        Assert.AreEqual("800", sources[0].GetAttribute("width"));
        Assert.AreEqual("600", sources[0].GetAttribute("height"));
        Assert.AreEqual("photo.webp", sources[1].GetAttribute("srcset"));

        // The img has to be the LAST child of the picture: the sources are only offers, and it is the
        // img that the browser falls back to and that carries the alt.
        Assert.AreEqual("IMG", picture.LastElementChild!.TagName);
        Assert.AreEqual("photo.jpg", picture.LastElementChild.GetAttribute("src"));
    }

    [TestMethod]
    public void BitImageShouldSkipSourcesWithoutASrcset()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "photo.jpg");
            parameters.Add(p => p.Sources, [new BitImageSource { Srcset = "photo.avif" }, null!, new BitImageSource()]);
        });

        Assert.AreEqual(1, component.FindAll("picture.bit-img-pic > source").Count);
    }

    /// <summary>No source worth rendering means no picture at all, so nothing is added to the DOM for it.</summary>
    [TestMethod]
    public void BitImageShouldNotRenderAPictureWithoutSources()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "photo.jpg");
            parameters.Add(p => p.Sources, [new BitImageSource()]);
        });

        Assert.AreEqual(0, component.FindAll("picture").Count);
    }

    /// <summary>
    /// A collection is compared by reference, and a page that writes its sources inline builds a new
    /// one on every render - so changing them must not return a loaded image to the loading state, or
    /// it would be hidden again every time anything around it changed.
    /// </summary>
    [TestMethod]
    public void BitImageShouldNotReloadWhenTheSourcesAreRebuilt()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "photo.jpg");
            parameters.Add(p => p.Sources, [new BitImageSource { Srcset = "photo.avif" }]);
        });

        component.Find(".bit-img-img").TriggerEvent("onload", new ProgressEventArgs());
        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Sources, [new BitImageSource { Srcset = "photo.avif" }]);
        });

        Assert.AreEqual(BitImageState.Loaded, component.Instance.LoadingState);
        Assert.IsTrue(component.Find(".bit-img-img").ClassList.Contains("bit-img-vis"));
    }



    // ---- accessibility ---------------------------------------------------------------------------

    /// <summary>
    /// The label names the image rather than the frame: an aria-label on a div with no role of its own
    /// is ignored, so on the frame alone it would say nothing at all.
    /// </summary>
    [TestMethod,
        DataRow("A photograph of a bridge"),
        DataRow(null)
    ]
    public void BitImageShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        Assert.AreEqual(ariaLabel, component.Find(".bit-img-img").GetAttribute("aria-label"));
        Assert.AreEqual(ariaLabel, component.Find(".bit-img").GetAttribute("aria-label"));
    }

    /// <summary>The placeholder is cropped by the same frame the image is, so it is positioned the same way.</summary>
    [TestMethod]
    public void BitImageShouldPositionThePlaceholderLikeTheImage()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.PlaceholderSrc, "placeholder.png");
            parameters.Add(p => p.ImagePosition, "top");
        });

        StringAssert.Contains(component.Find(".bit-img-plc").GetAttribute("style"), "object-position:top");
    }

    /// <summary>An image that failed is as absent as one still loading, so the placeholder stays.</summary>
    [TestMethod]
    public void BitImageShouldKeepThePlaceholderOnError()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
            parameters.Add(p => p.PlaceholderSrc, "placeholder.png");
        });

        component.Find(".bit-img-img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.AreEqual(1, component.FindAll(".bit-img-plc").Count);
    }

    /// <summary>A class list in ImageAttributes is joined to the component's rather than replaced by it.</summary>
    [TestMethod]
    public void BitImageShouldMergeASplattedClassIntoItsOwn()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.ImageAttributes, new() { { "class", "splatted-class" } });
            parameters.Add(p => p.Classes, new() { Image = "parameter-class" });
        });

        var classList = component.Find(".bit-img-img").ClassList;

        Assert.IsTrue(classList.Contains("splatted-class"));
        Assert.IsTrue(classList.Contains("parameter-class"));
        Assert.IsTrue(classList.Contains("bit-img-img"));
    }



    /// <summary>
    /// The frame carries the size and the shape; the img is what a page reaches for to do something to
    /// the picture itself. Both references are captured.
    /// </summary>
    [TestMethod]
    public void BitImageShouldCaptureBothElementReferences()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Src, "image.png");
        });

        Assert.IsFalse(string.IsNullOrEmpty(component.Instance.RootElement.Id));
        Assert.IsFalse(string.IsNullOrEmpty(component.Instance.ImageElement.Id));
        Assert.AreNotEqual(component.Instance.RootElement.Id, component.Instance.ImageElement.Id);
    }

    /// <summary>Focusing before anything is rendered does nothing rather than fail on an element that is not there.</summary>
    [TestMethod]
    public async Task BitImageShouldNotThrowWhenFocusedWithoutARenderedElement()
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        await component.InvokeAsync(async () => await component.Instance.FocusAsync());
        await component.InvokeAsync(async () => await component.Instance.FocusAsync(true));
    }



    // ---- the cascading parameters ---------------------------------------------------------------

    [TestMethod]
    public void BitImageShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitImageCascadingParamsTest>();

        var frames = component.FindAll(".bit-img");
        var images = component.FindAll(".bit-img-img");

        // The first image takes everything from the cascading parameters.
        Assert.IsTrue(frames[0].ClassList.Contains("cascaded"));
        Assert.IsTrue(frames[0].ClassList.Contains("bit-img-rnd"));
        Assert.IsTrue(frames[0].ClassList.Contains("bit-img-brd"));
        StringAssert.Contains(frames[0].GetAttribute("style"), "width:10rem");
        StringAssert.Contains(frames[0].GetAttribute("style"), "aspect-ratio:16/9");
        Assert.IsTrue(images[0].ClassList.Contains("bit-img-cvr"));
        Assert.IsTrue(images[0].ClassList.Contains("bit-img-vis"));
        Assert.AreEqual("lazy", images[0].GetAttribute("loading"));
        Assert.AreEqual("async", images[0].GetAttribute("decoding"));

        // The second one sets its own width and fit, which the cascading parameters must not overwrite.
        StringAssert.Contains(frames[1].GetAttribute("style"), "width:20rem");
        Assert.IsTrue(images[1].ClassList.Contains("bit-img-cnt"));
        Assert.IsFalse(images[1].ClassList.Contains("bit-img-cvr"));

        // What it did not set is still filled in from them.
        Assert.IsTrue(frames[1].ClassList.Contains("bit-img-rnd"));
        Assert.AreEqual("lazy", images[1].GetAttribute("loading"));
    }
}
