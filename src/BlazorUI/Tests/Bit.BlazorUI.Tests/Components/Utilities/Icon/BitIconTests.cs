using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Icon;

[TestClass]
public class BitIconTests : BunitTestContext
{
    private const string CLASS = "bit-ico bit-ico-pri bit-ico-md bit-ico-txt";

    // An icon with nothing to name it is decorative, so it is hidden from assistive technology and
    // carries no role. Every markup expectation that does not set a name therefore carries this.
    private const string HIDDEN = @"aria-hidden=""true""";

    [TestMethod]
    public void BitIconShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i {HIDDEN} class=""{CLASS}"" id:ignore />");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-dis"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<i class=""{CLASS}{icoClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectIconNameChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IconName, "AzureIcon");
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-icon bit-icon--AzureIcon"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectIcon()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Fa("solid house"));
        });

        component.MarkupMatches(@$"<i class=""{CLASS} fa-solid fa-house"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldPreferIconOverIconName()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Bi("house"));
            parameters.Add(p => p.IconName, "AzureIcon");
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bi bi-house"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldFallBackToIconNameWhenIconNamesNothing()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Icon, new BitIconInfo());
            parameters.Add(p => p.IconName, "AzureIcon");
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-icon bit-icon--AzureIcon"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRenderTheLigatureOfALigatureBasedIconSet()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Ms("home"));
        });

        component.MarkupMatches(@$"<i class=""{CLASS} material-symbols-outlined"" {HIDDEN} translate=""no"" id:ignore>home</i>");
    }

    [TestMethod]
    public void BitIconShouldNotMarkAClassBasedIconAsNotToTranslate()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, "Accept");
        });

        Assert.IsFalse(component.Find("i").HasAttribute("translate"));
    }

    [TestMethod]
    public void BitIconShouldKeepASplattedTranslate()
    {
        var component = RenderComponent<BitIconTranslateTest>();

        // A ligature the app has decided is worth translating stays translatable.
        Assert.AreEqual("yes", component.Find("i").GetAttribute("translate"));
    }

    [TestMethod]
    public void BitIconShouldRenderChildContent()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.AddChildContent("<svg viewBox=\"0 0 1 1\"></svg>");
        });

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore><svg viewBox=""0 0 1 1""></svg></i>");
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<i style=""{style}"" class=""{CLASS}"" {HIDDEN} id:ignore />");
        }
        else
        {
            component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");
        }
    }

    [TestMethod]
    public void BitIconShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        component.MarkupMatches(@$"<i style=""padding: 1rem;"" class=""{CLASS}"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        component.MarkupMatches(@$"<i class=""{CLASS} test-class"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<i id=""{expectedId}"" class=""{CLASS}"" {HIDDEN} />");
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" dir=""{dir.Value.ToString().ToLower()}"" {HIDDEN} id:ignore />");
        }
        else
        {
            component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");
        }
    }

    [TestMethod]
    public void BitIconShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@$"<i dir=""ltr"" class=""{CLASS}"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
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
                component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@$"<i style=""visibility: hidden;"" class=""{CLASS}"" {HIDDEN} id:ignore />");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@$"<i style=""display: none;"" class=""{CLASS}"" {HIDDEN} id:ignore />");
                break;
        }
    }

    [TestMethod]
    public void BitIconShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@$"<i style=""display: none;"" class=""{CLASS}"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
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
            // A named icon is a picture worth announcing: it takes the img role and stops being hidden.
            component.MarkupMatches(@$"<i aria-label=""{ariaLabel}"" class=""{CLASS}"" role=""img"" id:ignore />");
        }
        else
        {
            component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");
        }
    }

    [TestMethod]
    public void BitIconShouldBecomeNamedWhenAriaLabelIsSetAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Saved");
        });

        component.MarkupMatches(@$"<i aria-label=""Saved"" role=""img"" class=""{CLASS}"" id:ignore />");
    }

    [TestMethod,
        DataRow("Delete this row"),
        DataRow(null)
    ]
    public void BitIconShouldRespectTitle(string title)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Title, title);
        });

        if (title.HasValue())
        {
            // A title is read out as well as shown, so it names the icon exactly as an aria-label does.
            component.MarkupMatches(@$"<i title=""{title}"" role=""img"" class=""{CLASS}"" id:ignore />");
        }
        else
        {
            component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");
        }
    }

    [TestMethod]
    public void BitIconShouldStayHiddenBehindAnAriaLabelledByOfItsOwn()
    {
        var component = RenderComponent<BitIconLabelledByTest>();

        component.MarkupMatches(@$"<i aria-labelledby=""some-id"" role=""img"" class=""{CLASS}"" id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldKeepASplattedAriaHidden()
    {
        var component = RenderComponent<BitIconAriaHiddenTest>();

        // An icon named by the app, but hidden by the app as well: neither decision is second-guessed.
        component.MarkupMatches(@$"<i aria-hidden=""false"" aria-label=""Attention"" role=""img"" class=""{CLASS}"" id:ignore />");
    }

    [TestMethod,
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

        component.MarkupMatches(@$"<i class=""bit-ico bit-ico-pri bit-ico-txt {sizeClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectSizeChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
        });

        component.MarkupMatches(@$"<i class=""bit-ico bit-ico-pri bit-ico-txt bit-ico-lg"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow("2rem"),
        DataRow("inherit"),
        DataRow(null)
    ]
    public void BitIconShouldRespectFontSize(string fontSize)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.FontSize, fontSize);
        });

        if (fontSize.HasValue())
        {
            component.MarkupMatches(@$"<i style=""font-size:{fontSize}"" class=""{CLASS}"" {HIDDEN} id:ignore />");
        }
        else
        {
            component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");
        }
    }

    [TestMethod]
    public void BitIconShouldRespectFontSizeChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.FontSize, "3rem");
        });

        component.MarkupMatches(@$"<i style=""font-size:3rem"" class=""{CLASS}"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
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
        DataRow(BitColor.TertiaryBorder),
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
            BitColor.Primary => "bit-ico-pri",
            BitColor.Secondary => "bit-ico-sec",
            BitColor.Tertiary => "bit-ico-ter",
            BitColor.Info => "bit-ico-inf",
            BitColor.Success => "bit-ico-suc",
            BitColor.Warning => "bit-ico-wrn",
            BitColor.SevereWarning => "bit-ico-swr",
            BitColor.Error => "bit-ico-err",
            BitColor.PrimaryBackground => "bit-ico-pbg",
            BitColor.SecondaryBackground => "bit-ico-sbg",
            BitColor.TertiaryBackground => "bit-ico-tbg",
            BitColor.PrimaryForeground => "bit-ico-pfg",
            BitColor.SecondaryForeground => "bit-ico-sfg",
            BitColor.TertiaryForeground => "bit-ico-tfg",
            BitColor.PrimaryBorder => "bit-ico-pbr",
            BitColor.SecondaryBorder => "bit-ico-sbr",
            BitColor.TertiaryBorder => "bit-ico-tbr",
            _ => "bit-ico-pri"
        };

        component.MarkupMatches(@$"<i class=""bit-ico bit-ico-md bit-ico-txt {colorClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectColorChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
        });

        component.MarkupMatches(@$"<i class=""bit-ico bit-ico-md bit-ico-txt bit-ico-err"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow(BitVariant.Fill),
        DataRow(BitVariant.Outline),
        DataRow(BitVariant.Text),
        DataRow(null)
    ]
    public void BitIconShouldRespectVariant(BitVariant? variant)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Variant, variant);
        });

        var variantClass = variant switch
        {
            BitVariant.Fill => "bit-ico-fil",
            BitVariant.Outline => "bit-ico-out",
            BitVariant.Text => "bit-ico-txt",
            _ => "bit-ico-txt"
        };

        component.MarkupMatches(@$"<i class=""bit-ico bit-ico-pri bit-ico-md {variantClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectVariantChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Variant, BitVariant.Fill);
        });

        component.MarkupMatches(@$"<i class=""bit-ico bit-ico-pri bit-ico-md bit-ico-fil"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow(BitIconRotate.Rotate90, "bit-ico-rt90"),
        DataRow(BitIconRotate.Rotate180, "bit-ico-rt180"),
        DataRow(BitIconRotate.Rotate270, "bit-ico-rt270")
    ]
    public void BitIconShouldRespectRotate(BitIconRotate rotate, string expectedClass)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Rotate, rotate);
        });

        component.MarkupMatches(@$"<i class=""{CLASS} {expectedClass} bit-ico-trn"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRenderNoTransformWithoutRotateOrFlip()
    {
        var component = RenderComponent<BitIcon>();

        Assert.IsFalse(component.Find("i").ClassList.Contains("bit-ico-trn"));
    }

    [TestMethod,
        DataRow(BitIconFlip.Horizontal, "bit-ico-flh"),
        DataRow(BitIconFlip.Vertical, "bit-ico-flv"),
        DataRow(BitIconFlip.Both, "bit-ico-flb")
    ]
    public void BitIconShouldRespectFlip(BitIconFlip flip, string expectedClass)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Flip, flip);
        });

        component.MarkupMatches(@$"<i class=""{CLASS} {expectedClass} bit-ico-trn"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldComposeRotateAndFlip()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Rotate, BitIconRotate.Rotate90);
            parameters.Add(p => p.Flip, BitIconFlip.Horizontal);
        });

        // One transform class for both, so neither rule can overwrite the other's transform.
        component.MarkupMatches(@$"<i class=""{CLASS} bit-ico-rt90 bit-ico-flh bit-ico-trn"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow(45),
        DataRow(200),
        DataRow(-30),
        DataRow(0)
    ]
    public void BitIconShouldRespectRotateAngle(int angle)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.RotateAngle, angle);
        });

        component.MarkupMatches(@$"<i style=""--bit-ico-rotate:{angle}deg"" class=""{CLASS} bit-ico-trn"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldWriteRotateAngleWhereItWinsOverRotate()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Rotate, BitIconRotate.Rotate90);
            parameters.Add(p => p.RotateAngle, 45);
        });

        // The quarter-turn class stays on the element - the angle wins because an inline style beats
        // the custom property that class sets, not because the class was taken away.
        component.MarkupMatches(@$"<i style=""--bit-ico-rotate:45deg"" class=""{CLASS} bit-ico-rt90 bit-ico-trn"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldComposeRotateAngleAndFlip()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.RotateAngle, 45);
            parameters.Add(p => p.Flip, BitIconFlip.Horizontal);
        });

        component.MarkupMatches(@$"<i style=""--bit-ico-rotate:45deg"" class=""{CLASS} bit-ico-flh bit-ico-trn"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectRotateAngleChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.RotateAngle, 15);
        });

        component.MarkupMatches(@$"<i style=""--bit-ico-rotate:15deg"" class=""{CLASS} bit-ico-trn"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldWriteTheRotateAngleWithAnInvariantDecimalSeparator()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fa-IR");

        try
        {
            var component = RenderComponent<BitIcon>(parameters =>
            {
                parameters.Add(p => p.RotateAngle, 45);
            });

            Assert.AreEqual("--bit-ico-rotate:45deg", component.Find("i").GetAttribute("style"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitIconShouldRespectFlipRtl(bool flipRtl)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.FlipRtl, flipRtl);
        });

        var cssClass = flipRtl ? " bit-ico-frt bit-ico-trn" : null;

        component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitIconShouldRespectFixedWidth(bool fixedWidth)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.FixedWidth, fixedWidth);
        });

        var cssClass = fixedWidth ? " bit-ico-fxw" : null;

        component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitIconShouldRespectCircular(bool circular)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Circular, circular);
        });

        var cssClass = circular ? " bit-ico-cir" : null;

        component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectCircularChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Circular, true);
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-ico-cir"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitIconShouldRespectInline(bool inline)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Inline, inline);
        });

        var cssClass = inline ? " bit-ico-inl" : null;

        component.MarkupMatches(@$"<i class=""{CLASS}{cssClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow(BitIconAnimation.Spin, "bit-ico-spn"),
        DataRow(BitIconAnimation.SpinReverse, "bit-ico-spr"),
        DataRow(BitIconAnimation.Pulse, "bit-ico-pls"),
        DataRow(BitIconAnimation.Beat, "bit-ico-bet"),
        DataRow(BitIconAnimation.Fade, "bit-ico-fad"),
        DataRow(BitIconAnimation.Shake, "bit-ico-shk"),
        DataRow(BitIconAnimation.Bounce, "bit-ico-bnc"),
        DataRow(BitIconAnimation.BeatFade, "bit-ico-btf")
    ]
    public void BitIconShouldRespectAnimation(BitIconAnimation animation, string expectedClass)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Animation, animation);
        });

        component.MarkupMatches(@$"<i class=""{CLASS} {expectedClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectAnimationChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>();

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Animation, BitIconAnimation.Spin);
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-ico-spn"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitIconShouldRespectForceAnimation(bool forceAnimation)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.ForceAnimation, forceAnimation);
            parameters.Add(p => p.Animation, BitIconAnimation.Spin);
        });

        var cssClass = forceAnimation ? " bit-fam" : null;

        component.MarkupMatches(@$"<i class=""{CLASS} bit-ico-spn{cssClass}"" {HIDDEN} id:ignore />");
    }

    [TestMethod,
        DataRow("2s"),
        DataRow("500ms")
    ]
    public void BitIconShouldRespectAnimationDuration(string duration)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Animation, BitIconAnimation.Spin);
            parameters.Add(p => p.AnimationDuration, duration);
        });

        component.MarkupMatches(@$"<i style=""--bit-ico-anm-dur:{duration}"" class=""{CLASS} bit-ico-spn bit-ico-anm"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRenderNoDurationClassWithoutADuration()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Animation, BitIconAnimation.Spin);
        });

        // The rule that class carries resolves to nothing without a duration behind it, which would
        // stop the animation rather than leave it at its default speed.
        Assert.IsFalse(component.Find("i").ClassList.Contains("bit-ico-anm"));
    }

    [TestMethod]
    public void BitIconShouldRespectAnimationDurationChangingAfterRender()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Animation, BitIconAnimation.Beat);
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-ico-bet"" {HIDDEN} id:ignore />");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AnimationDuration, "3s");
        });

        component.MarkupMatches(@$"<i style=""--bit-ico-anm-dur:3s"" class=""{CLASS} bit-ico-bet bit-ico-anm"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldComposeEveryStyleItWrites()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.FontSize, "2rem");
            parameters.Add(p => p.RotateAngle, 45);
            parameters.Add(p => p.Animation, BitIconAnimation.Spin);
            parameters.Add(p => p.AnimationDuration, "2s");
        });

        var style = component.Find("i").GetAttribute("style");

        StringAssert.Contains(style, "font-size:2rem");
        StringAssert.Contains(style, "--bit-ico-rotate:45deg");
        StringAssert.Contains(style, "--bit-ico-anm-dur:2s");
    }

    [TestMethod]
    public void BitIconShouldRenderNoTabIndexByDefault()
    {
        var component = RenderComponent<BitIcon>();

        Assert.IsFalse(component.Find("i").HasAttribute("tabindex"));
    }

    [TestMethod]
    public void BitIconShouldRespectTabIndex()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
        });

        component.MarkupMatches(@$"<i tabindex=""3"" class=""{CLASS}"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconWithOnClickShouldBecomeAButton()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
        });

        component.MarkupMatches(@$"<i role=""button"" tabindex=""0"" class=""{CLASS} bit-ico-int"" id:ignore />");
    }

    [TestMethod]
    public void BitIconWithOnClickShouldFallBackToTheIconNameAsItsAccessibleName()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, "Delete");
            parameters.Add(p => p.OnClick, () => { });
        });

        // A button role with nothing to read out is a control a screen reader stops at and announces as
        // "button", so an unnamed interactive icon falls back to the name of the glyph it draws.
        component.MarkupMatches(@$"<i role=""button"" tabindex=""0"" aria-label=""Delete"" class=""{CLASS} bit-icon bit-icon--Delete bit-ico-int"" id:ignore />");
    }

    [TestMethod]
    public void BitIconWithOnClickShouldPreferItsOwnNameOverTheIconName()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, "Delete");
            parameters.Add(p => p.AriaLabel, "Delete this row");
            parameters.Add(p => p.OnClick, () => { });
        });

        component.MarkupMatches(@$"<i role=""button"" tabindex=""0"" aria-label=""Delete this row"" class=""{CLASS} bit-icon bit-icon--Delete bit-ico-int"" id:ignore />");
    }

    [TestMethod]
    public void BitIconWithoutOnClickShouldNotFallBackToTheIconName()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, "Delete");
        });

        // A decorative icon is hidden rather than named: the label beside it already says the same thing.
        component.MarkupMatches(@$"<i class=""{CLASS} bit-icon bit-icon--Delete"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconWithOnClickShouldRespectTabIndex()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
            parameters.Add(p => p.TabIndex, "2");
        });

        component.MarkupMatches(@$"<i role=""button"" tabindex=""2"" class=""{CLASS} bit-ico-int"" id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectOnClick()
    {
        var clicked = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        component.Find("i").Click();

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitIconShouldNotRespectOnClickWhenDisabled()
    {
        var clicked = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@$"<i role=""button"" tabindex=""-1"" aria-disabled=""true"" class=""{CLASS} bit-ico-int bit-dis"" id:ignore />");

        component.Find("i").Click();

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public void BitIconShouldActivateOnEnterKeyDown()
    {
        var clicked = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        component.Find("i").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitIconShouldActivateOnSpaceKeyUp()
    {
        var clicked = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        component.Find("i").KeyDown(new KeyboardEventArgs { Key = " ", Code = "Space" });

        // The press alone does nothing: a reader who pressed Space by mistake can move off the icon
        // before releasing it, exactly as they can on a native button.
        Assert.AreEqual(0, clicked);

        component.Find("i").KeyUp(new KeyboardEventArgs { Key = " ", Code = "Space" });

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitIconShouldNotActivateOnASpaceItNeverSawPressed()
    {
        var clicked = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        // The focus landed on the icon while Space was already held down somewhere else, so the release
        // that arrives here is the end of someone else's keystroke.
        component.Find("i").KeyUp(new KeyboardEventArgs { Key = " ", Code = "Space" });

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public void BitIconShouldActivateOncePerSpacePress()
    {
        var clicked = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        component.Find("i").KeyDown(new KeyboardEventArgs { Key = " ", Code = "Space" });
        component.Find("i").KeyUp(new KeyboardEventArgs { Key = " ", Code = "Space" });
        component.Find("i").KeyUp(new KeyboardEventArgs { Key = " ", Code = "Space" });

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitIconShouldPassTheModifiersOfTheKeystrokeOnAsAClickWithNoPointerBehindIt()
    {
        MouseEventArgs? args = null;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, (MouseEventArgs e) => args = e);
        });

        component.Find("i").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true, ShiftKey = true });

        Assert.IsNotNull(args);
        Assert.AreEqual(0, args.Detail);
        Assert.IsTrue(args.CtrlKey);
        Assert.IsTrue(args.ShiftKey);
        Assert.IsFalse(args.AltKey);
        Assert.IsFalse(args.MetaKey);
    }

    [TestMethod]
    public void BitIconShouldRegisterTheSpaceKeyItStopsFromScrollingThePage()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.registerPreventKeys");
    }

    [TestMethod]
    public void BitIconShouldRegisterNoPreventedKeysWithoutAHandler()
    {
        var component = RenderComponent<BitIcon>();

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.registerPreventKeys"].Count);
    }

    [TestMethod]
    public void BitIconShouldRegisterThePreventedKeysOnlyOnce()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
        });

        // The listener stays on the element and reads the key list on every event, so re-registering it
        // on every render would be a round trip per render for nothing.
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.registerPreventKeys"].Count);
    }

    [TestMethod]
    public void BitIconShouldEmptyThePreventedKeysWhenItLosesItsHandler()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.OnClick, default(EventCallback<MouseEventArgs>));
        });

        var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.registerPreventKeys"];

        Assert.AreEqual(2, invocations.Count);
        Assert.AreEqual(0, ((string[])invocations[1].Arguments[1]!).Length);
    }

    [TestMethod]
    public void BitIconShouldResolveTheIconOncePerRender()
    {
        var calls = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, "house");
            parameters.Add(p => p.IconResolver, name => { calls++; return BitIconInfo.Fa($"solid {name}"); });
        });

        // The markup and the class attribute are two readers of one answer, not two questions.
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void BitIconShouldIgnoreOtherKeys()
    {
        var clicked = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        component.Find("i").KeyDown(new KeyboardEventArgs { Key = "a" });
        component.Find("i").KeyUp(new KeyboardEventArgs { Key = "a" });

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public void BitIconShouldResolveTheIconNameThroughAResolver()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, "house");
            parameters.Add(p => p.IconResolver, name => BitIconInfo.Fa($"solid {name}"));
        });

        component.MarkupMatches(@$"<i class=""{CLASS} fa-solid fa-house"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldResolveALigatureSetThroughAResolver()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, "home");
            parameters.Add(p => p.IconResolver, name => BitIconInfo.Ms(name));
        });

        component.MarkupMatches(@$"<i class=""{CLASS} material-symbols-outlined"" {HIDDEN} translate=""no"" id:ignore>home</i>");
    }

    [TestMethod]
    public void BitIconShouldLetTheIconWinOverTheResolver()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Bi("github"));
            parameters.Add(p => p.IconName, "house");
            parameters.Add(p => p.IconResolver, name => BitIconInfo.Fa($"solid {name}"));
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bi bi-github"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldFallBackToTheBuiltInSetWhenTheResolverAnswersWithNothing()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, "Accept");
            parameters.Add(p => p.IconResolver, name => null);
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-icon bit-icon--Accept"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldFallBackToTheBuiltInSetWhenTheResolverAnswersWithAnEmptyIcon()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconName, "Accept");
            parameters.Add(p => p.IconResolver, name => new BitIconInfo());
        });

        component.MarkupMatches(@$"<i class=""{CLASS} bit-icon bit-icon--Accept"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldNotCallTheResolverWithoutAnIconName()
    {
        var calls = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.IconResolver, name => { calls++; return null; });
        });

        component.MarkupMatches(@$"<i class=""{CLASS}"" {HIDDEN} id:ignore />");

        Assert.AreEqual(0, calls);
    }

    [TestMethod,
        DataRow("2s"),
        DataRow("250ms")
    ]
    public void BitIconShouldRespectAnimationDelay(string delay)
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Animation, BitIconAnimation.Fade);
            parameters.Add(p => p.AnimationDelay, delay);
        });

        component.MarkupMatches(@$"<i style=""--bit-ico-anm-dly:{delay}"" class=""{CLASS} bit-ico-fad bit-ico-dly"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRenderNoDelayClassWithoutADelay()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.Animation, BitIconAnimation.Fade);
        });

        Assert.IsFalse(component.Find("i").ClassList.Contains("bit-ico-dly"));
    }

    [TestMethod]
    public void BitIconShouldNotActivateFromTheKeyboardWhenDisabled()
    {
        var clicked = 0;

        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
            parameters.Add(p => p.IsEnabled, false);
        });

        component.Find("i").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        component.Find("i").KeyUp(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public void BitIconShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitIconHtmlAttributesTest>();

        component.MarkupMatches(@$"<i data-val-test=""bit"" {HIDDEN} class=""{CLASS}"" id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldRespectCascadingParameters()
    {
        var component = RenderComponent<BitIconParamsTest>();

        component.MarkupMatches(@$"<i style=""--bit-ico-rotate:90deg;--bit-ico-anm-dur:2s"" class=""bit-ico bit-ico-err bit-ico-lg bit-ico-out bit-ico-fxw bit-ico-cir bit-ico-inl bit-ico-trn bit-ico-spn bit-ico-anm"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public void BitIconShouldLetItsOwnParametersWinOverTheCascadedOnes()
    {
        var component = RenderComponent<BitIconParamsOverrideTest>();

        component.MarkupMatches(@$"<i style=""--bit-ico-rotate:90deg;--bit-ico-anm-dur:2s"" class=""bit-ico bit-ico-suc bit-ico-lg bit-ico-out bit-ico-fxw bit-ico-cir bit-ico-inl bit-ico-trn bit-ico-spn bit-ico-anm"" {HIDDEN} id:ignore />");
    }

    [TestMethod]
    public async Task BitIconShouldRespectFocusAsync()
    {
        var component = RenderComponent<BitIcon>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => { });
        });

        await component.InvokeAsync(() => component.Instance.FocusAsync());

        Context.JSInterop.VerifyFocusAsyncInvoke();
    }
}
