using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ErrorEventArgs = Microsoft.AspNetCore.Components.Web.ErrorEventArgs;

namespace Bit.BlazorUI.Tests.Components.Notifications.Persona;

[TestClass]
public class BitPersonaTests : BunitTestContext
{
    [TestMethod,
         DataRow(true),
         DataRow(false)
    ]
    public void BitPersonaTest(bool isEnabled)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var persona = component.Find(".bit-prs");

        if (isEnabled)
        {
            Assert.IsFalse(persona.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(persona.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitPersonaShouldAddCorrectDetailsText()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "PrimaryText");
            parameters.Add(p => p.SecondaryText, "SecondaryText");
            parameters.Add(p => p.TertiaryText, "TertiaryText");
            parameters.Add(p => p.OptionalText, "OptionalText");
        });

        Assert.AreEqual("PrimaryText", component.Find(".bit-prs-ptx").TextContent.Trim());
        Assert.AreEqual("SecondaryText", component.Find(".bit-prs-stx").TextContent.Trim());
        Assert.AreEqual("TertiaryText", component.Find(".bit-prs-ttx").TextContent.Trim());
        Assert.AreEqual("OptionalText", component.Find(".bit-prs-otx").TextContent.Trim());
    }

    [TestMethod]
    public void BitPersonaShouldNotRenderEmptyDetailsRows()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "PrimaryText");
        });

        // An empty row would still take a share of the space the details column spreads between its rows.
        Assert.IsNotEmpty(component.FindAll(".bit-prs-ptx"));
        Assert.IsEmpty(component.FindAll(".bit-prs-stx"));
        Assert.IsEmpty(component.FindAll(".bit-prs-ttx"));
        Assert.IsEmpty(component.FindAll(".bit-prs-otx"));
    }

    [TestMethod]
    public void BitPersonaShouldRenderDetailsRowsForTemplatesWithoutTexts()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.SecondaryTextTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span>role</span>")));
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-stx"));
        Assert.AreEqual("role", component.Find(".bit-prs-stx").TextContent.Trim());
    }

    [TestMethod,
        DataRow(BitPersonaSize.Size8),
        DataRow(BitPersonaSize.Size24),
        DataRow(BitPersonaSize.Size32),
        DataRow(BitPersonaSize.Size40),
        DataRow(BitPersonaSize.Size48),
        DataRow(BitPersonaSize.Size56),
        DataRow(BitPersonaSize.Size72),
        DataRow(BitPersonaSize.Size100),
        DataRow(BitPersonaSize.Size120)
    ]
    public void BitPersonaSizeClassNameTest(BitPersonaSize size)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var persona = component.Find(".bit-prs");
        var personaSizeClass = $"bit-prs-{size.ToString().ToLower().Replace("size", "s")}";

        Assert.IsTrue(persona.ClassList.Contains(personaSizeClass));
    }

    [TestMethod,
        DataRow("Image url"),
        DataRow(null)
    ]
    public void BitPersonaImageTest(string imageUrl)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, imageUrl);
        });

        if (imageUrl.HasValue())
        {
            var personaImage = component.Find(".bit-prs-img");
            var imageSrc = personaImage.GetAttribute("src");

            Assert.AreEqual(imageUrl, imageSrc);
        }
    }

    [TestMethod,
        DataRow("Presence Title", BitPersonaPresence.Blocked),
        DataRow("Presence Title", BitPersonaPresence.Away),
        DataRow("Presence Title", BitPersonaPresence.Offline),
        DataRow("Presence Title", BitPersonaPresence.Online),
        DataRow("Presence Title", BitPersonaPresence.Dnd),
        DataRow("Presence Title", BitPersonaPresence.Busy),
        DataRow("Presence Title", BitPersonaPresence.OutOfOffice),
        DataRow("Presence Title", BitPersonaPresence.Unknown)
    ]
    public void BitPersonaPresenceTitleTest(string presenceTitle, BitPersonaPresence presenceStatus)
    {
        var component = RenderComponent<BitPersona>(
            parameters =>
            {
                parameters.Add(p => p.PresenceTitle, presenceTitle);
                parameters.Add(p => p.Presence, presenceStatus);
            });

        var presenceEl = component.Find(".bit-prs-pre");

        Assert.AreEqual(presenceTitle, presenceEl.GetAttribute("title"));
        Assert.AreEqual(presenceTitle, presenceEl.GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(BitPersonaPresence.Offline, "bit-prs-off"),
        DataRow(BitPersonaPresence.Online, "bit-prs-onl"),
        DataRow(BitPersonaPresence.Away, "bit-prs-awy"),
        DataRow(BitPersonaPresence.Dnd, "bit-prs-dnd"),
        DataRow(BitPersonaPresence.Blocked, "bit-prs-blk"),
        DataRow(BitPersonaPresence.Busy, "bit-prs-bsy"),
        DataRow(BitPersonaPresence.OutOfOffice, "bit-prs-oof"),
        DataRow(BitPersonaPresence.Unknown, "bit-prs-unk")
    ]
    public void BitPersonaPresenceShouldApplyItsOwnClass(BitPersonaPresence presence, string expectedClass)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, presence);
        });

        Assert.IsTrue(component.Find(".bit-prs-pre").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitPersonaShouldNotRenderPresenceWhenPresenceIsNone()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.None);
        });

        Assert.IsEmpty(component.FindAll(".bit-prs-pre"));
    }

    [TestMethod,
        DataRow(BitPersonaPresence.Offline, "Offline"),
        DataRow(BitPersonaPresence.Online, "Online"),
        DataRow(BitPersonaPresence.Away, "Away"),
        DataRow(BitPersonaPresence.Dnd, "Do not disturb"),
        DataRow(BitPersonaPresence.Blocked, "Blocked"),
        DataRow(BitPersonaPresence.Busy, "Busy"),
        DataRow(BitPersonaPresence.OutOfOffice, "Out of office"),
        DataRow(BitPersonaPresence.Unknown, "Presence unknown")
    ]
    public void BitPersonaPresenceShouldCarryADefaultAccessibleName(BitPersonaPresence presence, string expectedLabel)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, presence);
        });

        var presenceEl = component.Find(".bit-prs-pre");

        Assert.AreEqual(expectedLabel, presenceEl.GetAttribute("aria-label"));
        // The default name is for screen readers only - nothing was asked to be shown as a tooltip.
        Assert.IsNull(presenceEl.GetAttribute("title"));
    }

    [TestMethod]
    public void BitPersonaPresenceTitlesShouldTakePrecedenceOverPresenceTitle()
    {
        var titles = new Dictionary<BitPersonaPresence, string>
        {
            { BitPersonaPresence.Dnd, "Do not disturb (localized)" }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Dnd);
            parameters.Add(p => p.PresenceTitle, "fallback");
            parameters.Add(p => p.PresenceTitles, titles);
        });

        var presenceEl = component.Find(".bit-prs-pre");

        Assert.AreEqual("Do not disturb (localized)", presenceEl.GetAttribute("title"));
        Assert.AreEqual("Do not disturb (localized)", presenceEl.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaPresenceTitlesShouldFallBackToPresenceTitleForUnmappedStatuses()
    {
        var titles = new Dictionary<BitPersonaPresence, string>
        {
            { BitPersonaPresence.Dnd, "Do not disturb (localized)" }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.PresenceTitle, "fallback");
            parameters.Add(p => p.PresenceTitles, titles);
        });

        Assert.AreEqual("fallback", component.Find(".bit-prs-pre").GetAttribute("title"));
    }

    [TestMethod]
    public void BitPersonaShouldRenderActionIconCssClassesFromBitIconInfo()
    {
        var actionIcon = new BitIconInfo("camera", "fa", "fa-");

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.OnActionClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
            parameters.Add(p => p.ActionIcon, actionIcon);
            parameters.Add(p => p.Size, BitPersonaSize.Size120);
        });

        var iconEl = component.Find(".bit-prs-aic");

        Assert.IsTrue(iconEl.ClassList.Contains("fa"));
        Assert.IsTrue(iconEl.ClassList.Contains("fa-camera"));
    }

    [TestMethod]
    public void BitPersonaActionIconShouldTakePrecedenceOverActionIconName()
    {
        var actionIcon = BitIconInfo.Bi("pencil-fill");

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.OnActionClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
            parameters.Add(p => p.ActionIcon, actionIcon);
            parameters.Add(p => p.ActionIconName, "Edit");
            parameters.Add(p => p.Size, BitPersonaSize.Size120);
        });

        var iconEl = component.Find(".bit-prs-aic");

        Assert.IsTrue(iconEl.ClassList.Contains("bi"));
        Assert.IsTrue(iconEl.ClassList.Contains("bi-pencil-fill"));
        Assert.IsFalse(iconEl.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public void BitPersonaActionButtonShouldBeNamedAndItsIconHidden()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.OnActionClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
            parameters.Add(p => p.ActionButtonTitle, "Change photo");
        });

        var button = component.Find(".bit-prs-abt");

        Assert.AreEqual("button", button.GetAttribute("type"));
        Assert.AreEqual("Change photo", button.GetAttribute("title"));
        Assert.AreEqual("Change photo", button.GetAttribute("aria-label"));
        Assert.AreEqual("true", component.Find(".bit-prs-aic").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitPersonaActionButtonShouldBeDisabledWhenPersonaIsDisabled()
    {
        var clicked = false;

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnActionClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true));
        });

        var button = component.Find(".bit-prs-abt");

        Assert.IsTrue(button.HasAttribute("disabled"));

        button.Click();

        Assert.IsFalse(clicked);
    }

    [TestMethod]
    public void BitPersonaActionTemplateShouldReplaceTheActionButton()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.OnActionClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
            parameters.Add(p => p.ActionTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class='custom-action'>go</span>")));
        });

        Assert.IsEmpty(component.FindAll(".bit-prs-abt"));
        Assert.IsNotEmpty(component.FindAll(".custom-action"));
    }

    [TestMethod]
    public void BitPersonaActionButtonShouldTakeThePlaceOfThePresence()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.OnActionClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-abt"));
        Assert.IsEmpty(component.FindAll(".bit-prs-pre"));
    }

    [TestMethod,
        DataRow(BitPersonaPresence.Online, "check-circle-fill"),
        DataRow(BitPersonaPresence.Offline, "wifi-off"),
        DataRow(BitPersonaPresence.Away, "clock-fill"),
        DataRow(BitPersonaPresence.Dnd, "dash-circle-fill"),
        DataRow(BitPersonaPresence.Busy, "exclamation-circle-fill")
    ]
    public void BitPersonaShouldRenderPresenceIconCssClassesFromPresenceIcons(BitPersonaPresence presence, string iconName)
    {
        var iconsInfo = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") },
            { BitPersonaPresence.Offline, BitIconInfo.Bi("wifi-off") },
            { BitPersonaPresence.Away, BitIconInfo.Bi("clock-fill") },
            { BitPersonaPresence.Dnd, BitIconInfo.Bi("dash-circle-fill") },
            { BitPersonaPresence.Busy, BitIconInfo.Bi("exclamation-circle-fill") }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, presence);
            parameters.Add(p => p.PresenceIcons, iconsInfo);
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        Assert.IsTrue(iconEl.ClassList.Contains("bi"));
        Assert.IsTrue(iconEl.ClassList.Contains($"bi-{iconName}"));
        Assert.AreEqual("true", iconEl.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitPersonaPresenceIconsShouldTakePrecedenceOverPresenceIconNames()
    {
        var iconsInfo = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") }
        };

        var icons = new Dictionary<BitPersonaPresence, string>
        {
            { BitPersonaPresence.Online, "SkypeCheck" }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.PresenceIcons, iconsInfo);
            parameters.Add(p => p.PresenceIconNames, icons);
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        Assert.IsTrue(iconEl.ClassList.Contains("bi"));
        Assert.IsTrue(iconEl.ClassList.Contains("bi-check-circle-fill"));
        Assert.IsFalse(iconEl.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public void BitPersonaShouldFallbackToPresenceIconNamesWhenPresenceIconsDoesNotContainPresence()
    {
        var icons = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            // Only Online is defined in PresenceIcons; Offline is intentionally missing.
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") }
        };

        var iconNames = new Dictionary<BitPersonaPresence, string>
        {
            // Offline is only defined in PresenceIconNames to verify the fallback behavior.
            { BitPersonaPresence.Offline, "SkypeMinus" }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Offline);
            parameters.Add(p => p.PresenceIcons, icons);
            parameters.Add(p => p.PresenceIconNames, iconNames);
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        // When PresenceIcons has no mapping for the current Presence, the component
        // should fall back to PresenceIconNames, which uses the BitIcon-based rendering.
        Assert.IsTrue(iconEl.ClassList.Contains("bit-icon"));
        Assert.IsFalse(iconEl.ClassList.Contains("bi"));
    }

    [TestMethod]
    public void BitPersonaShouldRenderNoPresenceIconWhenNoMappingExists()
    {
        var icons = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            // Define a presence different from the one used in the test to keep it unmapped.
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") }
        };

        var iconNames = new Dictionary<BitPersonaPresence, string>();

        var component = RenderComponent<BitPersona>(parameters =>
        {
            // Use a presence that is not present in either dictionary.
            parameters.Add(p => p.Presence, BitPersonaPresence.Busy);
            parameters.Add(p => p.PresenceIcons, icons);
            parameters.Add(p => p.PresenceIconNames, iconNames);
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconElements = component.FindAll(".bit-prs-pre i");

        // When neither PresenceIcons nor PresenceIconNames contains the current Presence,
        // no presence icon should be rendered.
        Assert.IsEmpty(iconElements);
    }

    [TestMethod]
    public void BitPersonaPresenceIconNameShouldRenderTheGlyphForTheCurrentStatus()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.OutOfOffice);
            parameters.Add(p => p.PresenceIconName, "Airplane");
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        Assert.IsTrue(iconEl.ClassList.Contains("bit-icon"));
        Assert.IsTrue(iconEl.ClassList.Contains("bit-icon--Airplane"));
        Assert.AreEqual("true", iconEl.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitPersonaPresenceIconShouldTakePrecedenceOverPresenceIconName()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.PresenceIcon, BitIconInfo.Bi("check-circle-fill"));
            parameters.Add(p => p.PresenceIconName, "SkypeCheck");
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        Assert.IsTrue(iconEl.ClassList.Contains("bi-check-circle-fill"));
        Assert.IsFalse(iconEl.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public void BitPersonaPresenceIconsShouldTakePrecedenceOverTheSingularPresenceIcon()
    {
        var icons = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.PresenceIcons, icons);
            parameters.Add(p => p.PresenceIcon, BitIconInfo.Fa("solid plane"));
            parameters.Add(p => p.PresenceIconName, "Airplane");
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        Assert.IsTrue(iconEl.ClassList.Contains("bi-check-circle-fill"));
    }

    [TestMethod]
    public void BitPersonaShouldFallBackToTheSingularPresenceIconForAnUnmappedStatus()
    {
        var icons = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            // Only Online is mapped, so the status under test has to fall through to the singular pair.
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Busy);
            parameters.Add(p => p.PresenceIcons, icons);
            parameters.Add(p => p.PresenceIconName, "Airplane");
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        Assert.IsTrue(iconEl.ClassList.Contains("bit-icon--Airplane"));
    }

    [TestMethod]
    public void BitPersonaSingularPresenceIconShouldNotBeRenderedOnTheSmallestSizes()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.PresenceIconName, "SkypeCheck");
            parameters.Add(p => p.Size, BitPersonaSize.Size32);
        });

        Assert.IsEmpty(component.FindAll(".bit-prs-pre i"));
    }

    [TestMethod,
        DataRow(BitPersonaSize.Size8),
        DataRow(BitPersonaSize.Size24),
        DataRow(BitPersonaSize.Size32)
    ]
    public void BitPersonaShouldNotRenderPresenceIconOnTheSmallestSizes(BitPersonaSize size)
    {
        var iconNames = new Dictionary<BitPersonaPresence, string>
        {
            { BitPersonaPresence.Online, "SkypeCheck" }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.PresenceIconNames, iconNames);
            parameters.Add(p => p.Size, size);
        });

        // The dot itself is there, but it has no room for a glyph inside it.
        Assert.IsNotEmpty(component.FindAll(".bit-prs-pre"));
        Assert.IsEmpty(component.FindAll(".bit-prs-pre i"));
    }

    [TestMethod]
    public void BitPersonaSize8ShouldRenderThePresenceInsteadOfACoin()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, BitPersonaSize.Size8);
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.ImageUrl, "some-image.png");
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-cin > .bit-prs-pre"));
        Assert.IsEmpty(component.FindAll(".bit-prs-imc"));
        Assert.IsEmpty(component.FindAll(".bit-prs-img"));
    }

    [TestMethod]
    public void BitPersonaSize8ShouldKeepItsDetailsEvenWhenTheyAreHidden()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, BitPersonaSize.Size8);
            parameters.Add(p => p.HidePersonaDetails, true);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-det"));
    }

    [TestMethod]
    public void BitPersonaSize8ShouldNotRenderAnEmptyCoinContainer()
    {
        // At the smallest size the coin container holds nothing but the presence dot, and an empty one
        // would still take its share of the row gap and indent the texts beside it.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, BitPersonaSize.Size8);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
        });

        Assert.IsEmpty(component.FindAll(".bit-prs-cin"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Size, BitPersonaSize.Size8);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-cin"));
    }

    [TestMethod]
    public void BitPersonaShouldAlwaysRenderTheCoinContainerAboveTheSmallestSize()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, BitPersonaSize.Size24);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-cin"));
    }

    [TestMethod]
    public void BitPersonaSquaredShouldApplySquaredClass()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Squared, true);
        });

        var persona = component.Find(".bit-prs");

        Assert.IsTrue(persona.ClassList.Contains("bit-prs-sqr"));
        Assert.IsFalse(persona.ClassList.Contains("bit-prs-crl"));
    }

    [TestMethod]
    public void BitPersonaDefaultShapeShouldBeCircular()
    {
        var component = RenderComponent<BitPersona>();

        var persona = component.Find(".bit-prs");

        Assert.IsFalse(persona.ClassList.Contains("bit-prs-sqr"));
    }

    [TestMethod]
    public void BitPersonaFullWidthShouldApplyItsClass()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.FullWidth, true);
        });

        Assert.IsTrue(component.Find(".bit-prs").ClassList.Contains("bit-prs-fwi"));
    }

    [TestMethod]
    public void BitPersonaReversedShouldApplyItsClass()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Reversed, true);
        });

        Assert.IsTrue(component.Find(".bit-prs").ClassList.Contains("bit-prs-rvs"));
    }

    [TestMethod,
        DataRow(null, "bit-prs-arg"),
        DataRow(BitPersonaActiveAppearance.Ring, "bit-prs-arg"),
        DataRow(BitPersonaActiveAppearance.Shadow, "bit-prs-ash"),
        DataRow(BitPersonaActiveAppearance.RingShadow, "bit-prs-ars")
    ]
    public void BitPersonaActiveShouldApplyTheAppearanceClass(BitPersonaActiveAppearance? appearance, string expectedClass)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Active, true);
            parameters.Add(p => p.ActiveAppearance, appearance);
        });

        Assert.IsTrue(component.Find(".bit-prs").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitPersonaShouldNotApplyAnActiveClassWhenItIsNotActive()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ActiveAppearance, BitPersonaActiveAppearance.Shadow);
        });

        var classList = component.Find(".bit-prs").ClassList;

        Assert.IsFalse(classList.Contains("bit-prs-arg"));
        Assert.IsFalse(classList.Contains("bit-prs-ash"));
        Assert.IsFalse(classList.Contains("bit-prs-ars"));
    }

    [TestMethod]
    public void BitPersonaActiveShouldBeIgnoredOnSize8WhichHasNoCoinToRing()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Active, true);
            parameters.Add(p => p.Size, BitPersonaSize.Size8);
        });

        Assert.IsFalse(component.Find(".bit-prs").ClassList.Contains("bit-prs-arg"));
    }

    [TestMethod,
        DataRow(null, "bit-prs-fil"),
        DataRow(BitVariant.Fill, "bit-prs-fil"),
        DataRow(BitVariant.Outline, "bit-prs-otl"),
        DataRow(BitVariant.Text, "bit-prs-txt")
    ]
    public void BitPersonaCoinVariantShouldApplyItsClass(BitVariant? variant, string expectedClass)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinVariant, variant);
        });

        Assert.IsTrue(component.Find(".bit-prs-imc").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-prs-pri"),
        DataRow(BitColor.Secondary, "bit-prs-sec"),
        DataRow(BitColor.Tertiary, "bit-prs-ter"),
        DataRow(BitColor.Info, "bit-prs-inf"),
        DataRow(BitColor.Success, "bit-prs-suc"),
        DataRow(BitColor.Warning, "bit-prs-wrn"),
        DataRow(BitColor.SevereWarning, "bit-prs-swr"),
        DataRow(BitColor.Error, "bit-prs-err"),
        DataRow(BitColor.PrimaryBackground, "bit-prs-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-prs-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-prs-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-prs-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-prs-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-prs-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-prs-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-prs-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-prs-tbr")
    ]
    public void BitPersonaCoinColorShouldApplyItsClass(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinColor, color);
        });

        Assert.IsTrue(component.Find(".bit-prs").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitPersonaAutoCoinColorShouldApplyAColorClassFromKnownPalette()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.AutoCoinColor, true);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
        });

        var persona = component.Find(".bit-prs");

        Assert.IsTrue(_autoCoinColorClasses.Any(c => persona.ClassList.Contains(c)));
    }

    [TestMethod]
    public void BitPersonaAutoCoinColorShouldBeDeterministicForSameName()
    {
        var class1 = GetAutoCoinColorClass(p => p.Add(x => x.PrimaryText, "Saleh Khafan"));
        var class2 = GetAutoCoinColorClass(p => p.Add(x => x.PrimaryText, "Saleh Khafan"));

        Assert.AreEqual(class1, class2);
    }

    [TestMethod]
    public void BitPersonaCoinColorShouldTakePrecedenceOverAutoCoinColor()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.AutoCoinColor, true);
            parameters.Add(p => p.CoinColor, BitColor.Warning);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
        });

        var persona = component.Find(".bit-prs");

        Assert.IsTrue(persona.ClassList.Contains("bit-prs-wrn"));
    }

    [TestMethod]
    public void BitPersonaCoinColorSeedShouldKeepTheColorWhileTheNameChanges()
    {
        var class1 = GetAutoCoinColorClass(p =>
        {
            p.Add(x => x.CoinColorSeed, "u-1024");
            p.Add(x => x.PrimaryText, "Xafan Salina");
        });

        var class2 = GetAutoCoinColorClass(p =>
        {
            p.Add(x => x.CoinColorSeed, "u-1024");
            p.Add(x => x.PrimaryText, "X. Salina");
        });

        Assert.AreEqual(class1, class2);
    }

    [TestMethod]
    public void BitPersonaCoinColorSeedShouldTakePrecedenceOverTheNameAndTheInitials()
    {
        var seeded = GetAutoCoinColorClass(p =>
        {
            p.Add(x => x.CoinColorSeed, "u-1024");
            p.Add(x => x.ImageInitials, "ZZ");
            p.Add(x => x.PrimaryText, "Xafan Salina");
        });

        var unseeded = GetAutoCoinColorClass(p =>
        {
            p.Add(x => x.ImageInitials, "ZZ");
            p.Add(x => x.PrimaryText, "Xafan Salina");
        });

        var seedOnly = GetAutoCoinColorClass(p => p.Add(x => x.CoinColorSeed, "u-1024"));

        Assert.AreEqual(seedOnly, seeded);
        Assert.AreNotEqual(unseeded, seeded);
    }

    [TestMethod]
    public void BitPersonaAutoCoinColorShouldFallBackToInfoWithoutAnythingToHash()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.AutoCoinColor, true);
        });

        Assert.IsTrue(component.Find(".bit-prs").ClassList.Contains("bit-prs-inf"));
    }

    [TestMethod,
        DataRow("Saleh Khafan", "SK"),
        DataRow("Ted Alan Randall", "TR"),
        DataRow("Cher", "C"),
        DataRow("Ted Alan Bob Randall", "T"),
        DataRow("Elvia Atkins (Contoso)", "EA"),
        DataRow("Dr. Ted Randall", "DR"),
        DataRow("  Saleh   Khafan  ", "SK"),
        DataRow("Saleh\tKhafan", "SK"),
        // The coin uppercases the initials in CSS, so what the DOM holds is the source casing.
        DataRow("carlos.slattery@contoso.com", "cs"),
        DataRow("carlos@contoso.com", "c"),
        DataRow("Mary-Jane Watson", "MW")
    ]
    public void BitPersonaShouldDeriveInitialsFromPrimaryText(string primaryText, string expectedInitials)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, primaryText);
        });

        Assert.AreEqual(expectedInitials, component.Find(".bit-prs-ini").TextContent.Trim());
    }

    [TestMethod,
        DataRow("+1 (555) 016 7788"),
        DataRow("1234567890"),
        DataRow("...")
    ]
    public void BitPersonaShouldShowTheFallbackIconWhenTheNameYieldsNoInitials(string primaryText)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, primaryText);
        });

        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));

        var icon = component.Find(".bit-prs-cic");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Contact"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitPersonaShouldShowTheFallbackIconWithoutAnyName()
    {
        var component = RenderComponent<BitPersona>();

        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));
        Assert.IsNotEmpty(component.FindAll(".bit-prs-cic"));
    }

    [TestMethod]
    public void BitPersonaTwoInitialsShouldNotBeShrunkToFitTheCoin()
    {
        // Two is what the coin is sized for, and what deriving initials from a name can ever produce.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
        });

        var initials = component.Find(".bit-prs-ini");

        Assert.IsFalse(initials.ClassList.Contains("bit-prs-in3"));
        Assert.IsFalse(initials.ClassList.Contains("bit-prs-in4"));
    }

    [TestMethod,
        DataRow("SKH", "bit-prs-in3"),
        DataRow("SKHN", "bit-prs-in4"),
        DataRow("SKHNM", "bit-prs-in4")
    ]
    public void BitPersonaLongInitialsShouldBeShrunkToFitTheCoin(string imageInitials, string expectedClass)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageInitials, imageInitials);
        });

        Assert.IsTrue(component.Find(".bit-prs-ini").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitPersonaInitialsShouldBeMeasuredInTextElementsRatherThanChars()
    {
        // Two emoji are four chars but take the room of two letters, so they are not shrunk as four would be.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageInitials, "\U0001F600\U0001F601");
        });

        var initials = component.Find(".bit-prs-ini");

        Assert.IsFalse(initials.ClassList.Contains("bit-prs-in3"));
        Assert.IsFalse(initials.ClassList.Contains("bit-prs-in4"));
    }

    [TestMethod]
    public void BitPersonaImageInitialsShouldBeUsedVerbatim()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
            parameters.Add(p => p.ImageInitials, "S.K!");
        });

        Assert.AreEqual("S.K!", component.Find(".bit-prs-ini").TextContent.Trim());
    }

    [TestMethod]
    public void BitPersonaInitialsShouldBeReversedInRtl()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
        });

        Assert.AreEqual("KS", component.Find(".bit-prs-ini").TextContent.Trim());
    }

    [TestMethod]
    public void BitPersonaInitialsShouldNotBeReversedForARightToLeftName()
    {
        // The bidi algorithm already lays a right-to-left pair out from the right, so writing it back to
        // front the way a Latin pair needs would undo the very order the reversal is there to produce.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.PrimaryText, "صالح یوسف نژاد");
        });

        Assert.AreEqual("صن", component.Find(".bit-prs-ini").TextContent.Trim());
    }

    [TestMethod]
    public void BitPersonaInitialsShouldNotBeReversedForARightToLeftNameOfTwoWords()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.PrimaryText, "משה כהן");
        });

        Assert.AreEqual("מכ", component.Find(".bit-prs-ini").TextContent.Trim());
    }

    [TestMethod]
    public void BitPersonaInitialsShouldNotSplitASurrogatePair()
    {
        // A single char index into this name would cut the emoji in half and leave a lone surrogate behind.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "\U0001F600 Smith");
        });

        Assert.AreEqual("\U0001F600S", component.Find(".bit-prs-ini").TextContent.Trim());
    }

    [TestMethod]
    public void BitPersonaCoinIconNameShouldReplaceTheInitials()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Design Team");
            parameters.Add(p => p.CoinIconName, "Group");
        });

        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));
        Assert.IsTrue(component.Find(".bit-prs-cic").ClassList.Contains("bit-icon--Group"));
    }

    [TestMethod]
    public void BitPersonaCoinIconShouldTakePrecedenceOverCoinIconName()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinIcon, BitIconInfo.Bi("people-fill"));
            parameters.Add(p => p.CoinIconName, "Group");
        });

        var icon = component.Find(".bit-prs-cic");

        Assert.IsTrue(icon.ClassList.Contains("bi-people-fill"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public void BitPersonaUnknownShouldTakePrecedenceOverTheImageAndTheInitials()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Unknown, true);
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
            parameters.Add(p => p.ImageUrl, "some-image.png");
        });

        Assert.IsEmpty(component.FindAll(".bit-prs-img"));
        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));
        Assert.IsTrue(component.Find(".bit-prs-cic").ClassList.Contains("bit-icon--Help"));
    }

    [TestMethod]
    public void BitPersonaUnknownIconNameShouldReplaceTheQuestionMark()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Unknown, true);
            parameters.Add(p => p.UnknownIconName, "StatusErrorFull");
        });

        Assert.IsTrue(component.Find(".bit-prs-cic").ClassList.Contains("bit-icon--StatusErrorFull"));
    }

    [TestMethod]
    public void BitPersonaUnknownIconShouldTakePrecedenceOverUnknownIconName()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Unknown, true);
            parameters.Add(p => p.UnknownIcon, BitIconInfo.Bi("question-circle"));
            parameters.Add(p => p.UnknownIconName, "StatusErrorFull");
        });

        var icon = component.Find(".bit-prs-cic");

        Assert.IsTrue(icon.ClassList.Contains("bi-question-circle"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon"));
    }

    [TestMethod,
        DataRow(BitImageLoading.Lazy),
        DataRow(BitImageLoading.Eager)
    ]
    public void BitPersonaImageLoadingAttributeShouldBeSet(BitImageLoading loading)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.ImageLoading, loading);
        });

        var img = component.Find(".bit-prs-img");

        Assert.AreEqual(loading.ToString().ToLower(), img.GetAttribute("loading"));
    }

    [TestMethod]
    public void BitPersonaImageSrcSetAttributeShouldBeSet()
    {
        var srcSet = "img-1x.png 1x, img-2x.png 2x";

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.ImageSrcSet, srcSet);
        });

        var img = component.Find(".bit-prs-img");

        Assert.AreEqual(srcSet, img.GetAttribute("srcset"));
    }

    [TestMethod]
    public void BitPersonaImageSrcSetAloneShouldStillBeAPicture()
    {
        // A set of candidates is as much a picture as a single url is - the img element needs only one of
        // the two to have something to fetch, so a coin given only candidates must not fall back to initials.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
            parameters.Add(p => p.ImageSrcSet, "img-1x.png 1x, img-2x.png 2x");
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-img"));
        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));
        Assert.IsTrue(component.Find(".bit-prs").ClassList.Contains("bit-prs-him"));
    }

    [TestMethod]
    public void BitPersonaImageSrcSetAloneShouldStillFallBackToTheInitialsOnError()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
            parameters.Add(p => p.ImageSrcSet, "img-1x.png 1x, img-2x.png 2x");
        });

        component.Find(".bit-prs-img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.IsEmpty(component.FindAll(".bit-prs-img"));
        Assert.AreEqual("SK", component.Find(".bit-prs-ini").TextContent);
    }

    [TestMethod]
    public void BitPersonaImageSizesAttributeShouldBeSet()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.ImageSizes, "(max-width: 600px) 48px, 72px");
        });

        Assert.AreEqual("(max-width: 600px) 48px, 72px", component.Find(".bit-prs-img").GetAttribute("sizes"));
    }

    [TestMethod]
    public void BitPersonaImageAttributesShouldReachTheImageElement()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.ImageAttributes, new Dictionary<string, object>
            {
                { "draggable", "false" },
                { "decoding", "async" }
            });
        });

        var img = component.Find(".bit-prs-img");

        Assert.AreEqual("false", img.GetAttribute("draggable"));
        Assert.AreEqual("async", img.GetAttribute("decoding"));
    }

    [TestMethod]
    public void BitPersonaImageShouldBeDecorativeByDefaultAndDescribableOnDemand()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.ImageUrl, "some-image.png");
        });

        // The name is already announced from the details, so the picture adds nothing to repeat.
        Assert.AreEqual("", component.Find(".bit-prs-img").GetAttribute("alt"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.ImageAlt, "Xafan smiling at the camera");
        });

        Assert.AreEqual("Xafan smiling at the camera", component.Find(".bit-prs-img").GetAttribute("alt"));
    }

    [TestMethod,
        DataRow(BitPersonaSize.Size24, "24"),
        DataRow(BitPersonaSize.Size48, "48"),
        DataRow(BitPersonaSize.Size120, "120")
    ]
    public void BitPersonaImageDimensionsShouldBeBarePixelCounts(BitPersonaSize size, string expected)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, size);
            parameters.Add(p => p.ImageUrl, "some-image.png");
        });

        var img = component.Find(".bit-prs-img");

        // The HTML width and height attributes are pixel counts, not CSS lengths.
        Assert.AreEqual(expected, img.GetAttribute("width"));
        Assert.AreEqual(expected, img.GetAttribute("height"));
    }

    [TestMethod]
    public void BitPersonaCoinSizeShouldScaleTheCoinAndTheInitials()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinSize, 64);
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
        });

        var style = component.Find(".bit-prs-imc").GetAttribute("style");

        Assert.IsTrue(style!.Contains("width:64px"));
        Assert.IsTrue(style.Contains("height:64px"));
        Assert.IsTrue(style.Contains("font-size:25.6px"));
    }

    [TestMethod]
    public void BitPersonaCoinSizeShouldScaleThePresenceDot()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinSize, 64);
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
        });

        var style = component.Find(".bit-prs-pre").GetAttribute("style");

        Assert.IsTrue(style!.Contains("width:16px"));
        Assert.IsTrue(style.Contains("height:16px"));
    }

    [TestMethod]
    public void BitPersonaCoinSizeShouldBeIgnoredOnSize8()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinSize, 64);
            parameters.Add(p => p.Size, BitPersonaSize.Size8);
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
        });

        Assert.IsEmpty(component.FindAll(".bit-prs-imc"));
    }

    [TestMethod]
    public void BitPersonaImageShouldNeverBeHeldBackWaitingForItsLoadEvent()
    {
        // A statically rendered page has no handler attached to fire the load event at all, and a prerendered
        // one can have the picture in the cache before it does - so a coin that hides its picture until the
        // event arrives is a coin that can stay empty for good.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
        });

        var img = component.Find(".bit-prs-img");

        Assert.IsFalse(img.GetAttribute("style")?.Contains("opacity:0") ?? false);

        img.TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsFalse(component.Find(".bit-prs-img").GetAttribute("style")?.Contains("opacity:0") ?? false);
    }

    [TestMethod]
    public void BitPersonaImageStylesShouldReachTheImageUntouched()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.Styles, new BitPersonaClassStyles { Image = "filter:grayscale(1);" });
        });

        Assert.AreEqual("filter:grayscale(1);", component.Find(".bit-prs-img").GetAttribute("style"));
    }

    [TestMethod]
    public void BitPersonaShouldShowTheInitialsAgainWhenImageUrlChanges()
    {
        // The new picture has its own load to wait for, so a coin told to show its initials until then has to
        // put them back rather than keep standing on the verdict the old picture gave.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
            parameters.Add(p => p.ShowInitialsUntilImageLoads, true);
            parameters.Add(p => p.ImageUrl, "image-1.png");
        });

        component.Find(".bit-prs-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
            parameters.Add(p => p.ShowInitialsUntilImageLoads, true);
            parameters.Add(p => p.ImageUrl, "image-2.png");
        });

        Assert.AreEqual("SK", component.Find(".bit-prs-ini").TextContent);
    }

    [TestMethod]
    public void BitPersonaShouldStartAFreshLoadWhenImageSrcSetChanges()
    {
        // The browser refetches when the candidate list changes, so a coin that had already given its
        // verdict on the old candidates has to wait for the new ones rather than keep the old one.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
            parameters.Add(p => p.ImageUrl, "image-1.png");
            parameters.Add(p => p.ImageSrcSet, "image-1.png 1x");
        });

        component.Find(".bit-prs-img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.IsEmpty(component.FindAll(".bit-prs-img"));
        Assert.IsNotEmpty(component.FindAll(".bit-prs-ini"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
            parameters.Add(p => p.ImageUrl, "image-1.png");
            parameters.Add(p => p.ImageSrcSet, "image-2.png 1x, image-2@2x.png 2x");
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-img"));
        Assert.AreEqual("image-2.png 1x, image-2@2x.png 2x", component.Find(".bit-prs-img").GetAttribute("srcset"));
    }

    [TestMethod]
    public void BitPersonaOnImageLoadShouldBeInvoked()
    {
        var loaded = false;

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.OnImageLoad, EventCallback.Factory.Create<ProgressEventArgs>(this, _ => loaded = true));
        });

        component.Find(".bit-prs-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsTrue(loaded);
    }

    [TestMethod]
    public void BitPersonaOnImageErrorShouldBeInvoked()
    {
        var errored = false;

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "invalid-url.png");
            parameters.Add(p => p.OnImageError, EventCallback.Factory.Create<ErrorEventArgs>(this, _ => errored = true));
        });

        component.Find(".bit-prs-img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.IsTrue(errored);
    }

    [TestMethod]
    public void BitPersonaOnImageErrorShouldFallbackToInitials()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.ImageUrl, "invalid.png");
        });

        Assert.IsNotNull(component.Find(".bit-prs-img"));
        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));

        component.Find(".bit-prs-img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.IsEmpty(component.FindAll(".bit-prs-img"));
        Assert.IsNotEmpty(component.FindAll(".bit-prs-ini"));
    }

    [TestMethod]
    public void BitPersonaOnImageErrorShouldFallbackToTheIconWhenThereAreNoInitials()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "invalid.png");
        });

        component.Find(".bit-prs-img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));
        Assert.IsNotEmpty(component.FindAll(".bit-prs-cic"));
    }

    [TestMethod]
    public void BitPersonaShowInitialsUntilImageLoadsShouldShowInitialsWhileLoading()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.ShowInitialsUntilImageLoads, true);
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-ini"));

        component.Find(".bit-prs-img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));
    }

    [TestMethod]
    public void BitPersonaShowInitialsUntilImageLoadsShouldBeIgnoredForACoinTemplate()
    {
        // A coin template paints itself and reports no load, so the initials behind it would never go away.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.ShowInitialsUntilImageLoads, true);
            parameters.Add(p => p.CoinTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class='custom-coin'>coin</span>")));
        });

        Assert.IsNotEmpty(component.FindAll(".custom-coin"));
        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));
    }

    [TestMethod]
    public void BitPersonaCoinTemplateShouldReplaceTheCoinContents()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.CoinTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class='custom-coin'>coin</span>")));
        });

        Assert.IsNotEmpty(component.FindAll(".custom-coin"));
        Assert.IsEmpty(component.FindAll(".bit-prs-img"));
        Assert.IsEmpty(component.FindAll(".bit-prs-ini"));
        // The default coin box is not drawn around a template that brings its own.
        Assert.IsEmpty(component.FindAll(".bit-prs-imc"));
    }

    [TestMethod]
    public void BitPersonaCoinElementClassShouldBeOnTheCoinWhateverFillsIt()
    {
        // The interaction rules - the pointer, the focus ring, the overlay it reveals - hang off this class,
        // so a coin filled by a template has to carry it just as the coin the component draws does.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-cne.bit-prs-imc"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.CoinTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class='custom-coin'>coin</span>")));
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-cne"));
        Assert.IsEmpty(component.FindAll(".bit-prs-imc"));
    }

    [TestMethod]
    public void BitPersonaClickableCoinTemplateShouldStillBeAButtonWithAnOverlay()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class='custom-coin'>coin</span>")));
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        var coin = component.Find(".bit-prs-cne");

        Assert.AreEqual("BUTTON", coin.TagName);
        Assert.AreEqual("button", coin.GetAttribute("type"));
        Assert.IsNotEmpty(component.FindAll(".bit-prs-cne > .bit-prs-imo"));
    }

    [TestMethod]
    public void BitPersonaOnImageClickShouldTurnTheCoinIntoAButton()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageOverlayText, "Change photo");
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        var coin = component.Find(".bit-prs-imc");

        Assert.IsTrue(component.Find(".bit-prs").ClassList.Contains("bit-prs-iac"));
        // A real button rather than a div dressed as one: the role, the focus, the keyboard and the disabled
        // state all come from the browser instead of being emulated.
        Assert.AreEqual("BUTTON", coin.TagName);
        Assert.AreEqual("button", coin.GetAttribute("type"));
        Assert.IsNull(coin.GetAttribute("role"));
        Assert.IsNull(coin.GetAttribute("tabindex"));
        Assert.AreEqual("Change photo", coin.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaCoinShouldStayAPlainElementWithoutAClickHandler()
    {
        var component = RenderComponent<BitPersona>();

        var coin = component.Find(".bit-prs-imc");

        Assert.AreEqual("DIV", coin.TagName);
        Assert.IsNull(coin.GetAttribute("role"));
        Assert.IsNull(coin.GetAttribute("tabindex"));
        Assert.IsNull(coin.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaOnImageClickShouldBeInvoked()
    {
        var clicked = 0;

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++));
        });

        component.Find(".bit-prs-imc").Click();

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitPersonaDisabledCoinShouldBeOutOfTheTabOrderAndInert()
    {
        var clicked = 0;

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++));
        });

        var coin = component.Find(".bit-prs-imc");

        // A disabled button is out of the tab order, unclickable and unannounced as an action, all of it
        // from the one attribute rather than from a tabindex and an aria-disabled kept in step by hand.
        Assert.IsTrue(coin.HasAttribute("disabled"));
        Assert.IsNull(coin.GetAttribute("tabindex"));

        coin.Click();

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public void BitPersonaDisabledCoinShouldDropTheDisabledAttributeWhenItIsEnabledAgain()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.IsTrue(component.Find(".bit-prs-imc").HasAttribute("disabled"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.IsFalse(component.Find(".bit-prs-imc").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitPersonaPlainCoinShouldNotBeMarkedDisabled()
    {
        // Nothing that is not a control has a disabled state to announce.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
        });

        var coin = component.Find(".bit-prs-imc");

        Assert.AreEqual("DIV", coin.TagName);
        Assert.IsFalse(coin.HasAttribute("disabled"));
        Assert.IsNull(coin.GetAttribute("aria-disabled"));
        Assert.IsNull(coin.GetAttribute("role"));
    }

    [TestMethod]
    public void BitPersonaImageOverlayShouldOnlyBeRenderedForAClickableCoin()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
        });

        Assert.IsEmpty(component.FindAll(".bit-prs-imo"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.AreEqual("Edit image", component.Find(".bit-prs-imo").TextContent.Trim());
    }

    [TestMethod]
    public void BitPersonaImageOverlayTemplateShouldReplaceTheOverlayText()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
            parameters.Add(p => p.ImageOverlayTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class='custom-overlay'>upload</span>")));
        });

        Assert.IsNotEmpty(component.FindAll(".custom-overlay"));
    }

    [TestMethod]
    public void BitPersonaImageOverlayShouldBeRenderedForAClickableCoinWithoutAPicture()
    {
        // The overlay belongs to the click, not to the picture: a coin carrying initials is every bit as
        // clickable as one carrying a photo and owes the pointer the same affordance.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Saleh Khafan");
            parameters.Add(p => p.ImageOverlayText, "Add photo");
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.AreEqual("Add photo", component.Find(".bit-prs-imo").TextContent.Trim());
        Assert.IsNotEmpty(component.FindAll(".bit-prs-ini"));
    }

    [TestMethod]
    public void BitPersonaImageOverlayShouldBeRenderedForAClickableUnknownCoin()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Unknown, true);
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.IsNotEmpty(component.FindAll(".bit-prs-imo"));
        Assert.IsNotEmpty(component.FindAll(".bit-prs-cic"));
    }

    [TestMethod]
    public void BitPersonaImageOverlayShouldNotCarryTheCoinVariantClass()
    {
        // The variant classes repaint what they are put on, and an outlined or bare coin would leave the
        // overlay transparent - which over a photo is an overlay that cannot be read at all.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, "some-image.png");
            parameters.Add(p => p.CoinVariant, BitVariant.Outline);
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        var overlay = component.Find(".bit-prs-imo");

        Assert.IsFalse(overlay.ClassList.Contains("bit-prs-otl"));
        Assert.IsFalse(overlay.ClassList.Contains("bit-prs-fil"));
        Assert.IsFalse(overlay.ClassList.Contains("bit-prs-txt"));
        Assert.IsTrue(component.Find(".bit-prs-imc").ClassList.Contains("bit-prs-otl"));
    }

    [TestMethod]
    public void BitPersonaHidePersonaDetailsShouldLeaveTheCoinAloneAndNameIt()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.HidePersonaDetails, true);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
        });

        var persona = component.Find(".bit-prs");

        Assert.IsEmpty(component.FindAll(".bit-prs-det"));
        Assert.AreEqual("img", persona.GetAttribute("role"));
        Assert.AreEqual("Xafan Salina", persona.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaCoinOnlyLabelShouldCarryThePresenceToo()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.HidePersonaDetails, true);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.Presence, BitPersonaPresence.Away);
        });

        // role="img" on the root makes the dot inside it presentational, so its name has to be folded in.
        Assert.AreEqual("Xafan Salina, Away", component.Find(".bit-prs").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaCoinOnlyShouldNotClaimTheImageRoleAroundAnInteractiveCoin()
    {
        // role="img" makes everything inside it presentational, which would put the button out of reach.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.HidePersonaDetails, true);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.ImageOverlayText, "Change photo");
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        var persona = component.Find(".bit-prs");

        Assert.IsNull(persona.GetAttribute("role"));
        Assert.IsNull(persona.GetAttribute("aria-label"));

        // The coin names itself instead, and says who it belongs to since nothing else does.
        Assert.AreEqual("Xafan Salina, Change photo", component.Find(".bit-prs-imc").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaCoinOnlyShouldNotClaimTheImageRoleAroundAnActionButton()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.HidePersonaDetails, true);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.OnActionClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.IsNull(component.Find(".bit-prs").GetAttribute("role"));
    }

    [TestMethod]
    public void BitPersonaClickableCoinWithVisibleDetailsShouldOnlyBeNamedAfterItsAction()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.ImageOverlayText, "Change photo");
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        // The name is already in the details next to it, so repeating it would only be noise.
        Assert.AreEqual("Change photo", component.Find(".bit-prs-imc").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaClickableCoinShouldFallBackToTheNameWithoutAnOverlayText()
    {
        // A template of one's own in the overlay is free to leave the text empty, and a button with nothing
        // to announce is one no assistive technology can tell apart from the next.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.ImageOverlayText, string.Empty);
            parameters.Add(p => p.ImageOverlayTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<i class='custom-overlay'></i>")));
            parameters.Add(p => p.OnImageClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        Assert.AreEqual("Xafan Salina", component.Find(".bit-prs-imc").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaPresenceStylesShouldBeAppliedWithoutACustomCoinSize()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.Styles, new BitPersonaClassStyles { Presence = "outline: 1px solid red" });
        });

        Assert.AreEqual("outline: 1px solid red", component.Find(".bit-prs-pre").GetAttribute("style"));
    }

    [TestMethod]
    public void BitPersonaPresenceStylesShouldSurviveACustomCoinSize()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinSize, 64);
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.Styles, new BitPersonaClassStyles { Presence = "outline: 1px solid red" });
        });

        var style = component.Find(".bit-prs-pre").GetAttribute("style");

        Assert.IsTrue(style!.Contains("width:16px"));
        Assert.IsTrue(style.Contains("outline: 1px solid red"));
    }

    [TestMethod]
    public void BitPersonaSquaredPresenceShouldBeNudgedOutWithACustomCoinSize()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinSize, 60);
            parameters.Add(p => p.Squared, true);
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
        });

        var style = component.Find(".bit-prs-pre").GetAttribute("style");

        // Retuned as the knob the stylesheet reads rather than as a side of its own, so the nudge follows the
        // dot to whichever of the four corners the writing direction and Reversed between them put it in.
        Assert.IsTrue(style!.Contains("--bit-prs-presence-inset:-5px"));
        Assert.IsFalse(style.Contains("inset-inline-end:"));
    }

    [TestMethod]
    public void BitPersonaSquaredPresenceNudgeShouldFollowAReversedPersona()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.CoinSize, 60);
            parameters.Add(p => p.Squared, true);
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
        });

        var style = component.Find(".bit-prs-pre").GetAttribute("style");

        // Reversed moves the dot to the other corner in the stylesheet; a nudge written as one named side
        // would have stayed behind on the corner the dot no longer sits in.
        Assert.IsTrue(style!.Contains("--bit-prs-presence-inset:-5px"));
        Assert.IsTrue(component.Find(".bit-prs").ClassList.Contains("bit-prs-rvs"));
    }

    [TestMethod]
    public void BitPersonaWithVisibleDetailsShouldNotClaimTheImageRole()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
        });

        var persona = component.Find(".bit-prs");

        Assert.IsNull(persona.GetAttribute("role"));
        Assert.IsNull(persona.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaCoinOnlyShouldNotClaimTheImageRoleWithNothingToAnnounce()
    {
        // An image role with no accessible name makes everything inside the persona presentational and then
        // has nothing of its own to put in their place, which is worse than claiming no role at all.
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.HidePersonaDetails, true);
            parameters.Add(p => p.ImageUrl, "some-image.png");
        });

        var persona = component.Find(".bit-prs");

        Assert.IsNull(persona.GetAttribute("role"));
        Assert.IsNull(persona.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaCoinOnlyShouldClaimTheImageRoleForAPresenceAlone()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.HidePersonaDetails, true);
            parameters.Add(p => p.Presence, BitPersonaPresence.Away);
            parameters.Add(p => p.ImageUrl, "some-image.png");
        });

        var persona = component.Find(".bit-prs");

        Assert.AreEqual("img", persona.GetAttribute("role"));
        Assert.AreEqual("Away", persona.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaAriaLabelShouldTakePrecedenceOverTheDerivedName()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.HidePersonaDetails, true);
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.AriaLabel, "The author of this message");
        });

        Assert.AreEqual("The author of this message", component.Find(".bit-prs").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaClassesAndStylesShouldReachTheirParts()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "Xafan Salina");
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.Classes, new BitPersonaClassStyles
            {
                Root = "custom-root",
                CoinContainer = "custom-coin-container",
                ImageContainer = "custom-image-container",
                Initials = "custom-initials",
                Presence = "custom-presence",
                DetailsContainer = "custom-details",
                PrimaryTextContainer = "custom-primary"
            });
            parameters.Add(p => p.Styles, new BitPersonaClassStyles
            {
                Root = "color: red",
                PrimaryTextContainer = "font-weight: bold"
            });
        });

        Assert.IsTrue(component.Find(".bit-prs").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-prs-cin").ClassList.Contains("custom-coin-container"));
        Assert.IsTrue(component.Find(".bit-prs-imc").ClassList.Contains("custom-image-container"));
        Assert.IsTrue(component.Find(".bit-prs-ini").ClassList.Contains("custom-initials"));
        Assert.IsTrue(component.Find(".bit-prs-pre").ClassList.Contains("custom-presence"));
        Assert.IsTrue(component.Find(".bit-prs-det").ClassList.Contains("custom-details"));
        Assert.IsTrue(component.Find(".bit-prs-ptx").ClassList.Contains("custom-primary"));

        Assert.IsTrue(component.Find(".bit-prs").GetAttribute("style")!.Contains("color: red"));
        Assert.IsTrue(component.Find(".bit-prs-ptx").GetAttribute("style")!.Contains("font-weight: bold"));
    }

    [TestMethod]
    public void BitPersonaSize8ShouldApplyBothPresenceAndPresentationCustomizations()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, BitPersonaSize.Size8);
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.Classes, new BitPersonaClassStyles { Presence = "custom-presence", Presentation = "custom-presentation" });
            parameters.Add(p => p.Styles, new BitPersonaClassStyles { Presence = "opacity: 0.5", Presentation = "outline: 1px solid red" });
        });

        var dot = component.Find(".bit-prs-pre");

        Assert.IsTrue(dot.ClassList.Contains("custom-presence"));
        Assert.IsTrue(dot.ClassList.Contains("custom-presentation"));

        var style = dot.GetAttribute("style");

        Assert.IsTrue(style!.Contains("opacity: 0.5"));
        Assert.IsTrue(style.Contains("outline: 1px solid red"));
    }

    [TestMethod]
    public void BitPersonaTextTemplatesShouldReplaceTheirTexts()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, BitPersonaSize.Size120);
            parameters.Add(p => p.PrimaryTextTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span>name</span>")));
            parameters.Add(p => p.SecondaryTextTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span>role</span>")));
            parameters.Add(p => p.TertiaryTextTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span>status</span>")));
            parameters.Add(p => p.OptionalTextTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span>note</span>")));
        });

        Assert.AreEqual("name", component.Find(".bit-prs-ptx").TextContent.Trim());
        Assert.AreEqual("role", component.Find(".bit-prs-stx").TextContent.Trim());
        Assert.AreEqual("status", component.Find(".bit-prs-ttx").TextContent.Trim());
        Assert.AreEqual("note", component.Find(".bit-prs-otx").TextContent.Trim());
    }

    [TestMethod]
    public void BitPersonaDetailTextsShouldBeTheirOwnTooltips()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, "A rather long name that will be cut off");
        });

        Assert.AreEqual("A rather long name that will be cut off", component.Find(".bit-prs-ptx").GetAttribute("title"));
    }

    [TestMethod]
    public void BitPersonaShouldApplyTheRtlDirection()
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var persona = component.Find(".bit-prs");

        Assert.AreEqual("rtl", persona.GetAttribute("dir"));
        Assert.IsTrue(persona.ClassList.Contains("bit-rtl"));
    }



    private static readonly string[] _autoCoinColorClasses =
        ["bit-prs-pri", "bit-prs-sec", "bit-prs-ter", "bit-prs-suc", "bit-prs-wrn", "bit-prs-err", "bit-prs-inf"];

    private string GetAutoCoinColorClass(System.Action<Bunit.ComponentParameterCollectionBuilder<BitPersona>> configure)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.AutoCoinColor, true);
            configure(parameters);
        });

        var classList = component.Find(".bit-prs").ClassList;

        var colorClass = _autoCoinColorClasses.FirstOrDefault(classList.Contains);

        Assert.IsNotNull(colorClass, $"No auto coin color class found on the persona: {string.Join(' ', classList)}");

        return colorClass;
    }
}
